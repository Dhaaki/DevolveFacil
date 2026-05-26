using Asp.Versioning;
using System.Security.Claims;
using DevolveFacill.Api.DTOs;
using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Domain.Enums;
using DevolveFacill.Core.Events;
using DevolveFacill.Core.Ports;
using DevolveFacill.Core.UseCases;
using DevolveFacill.Infrastructure.Persistence;
using DevolveFacill.Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevolveFacill.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/returns")]
[Authorize(Roles = "Agent,QualityInspector,Finance,Supervisor")]
public class AdminReturnsController(
    ReturnRequestRepository returns,
    IPublishEndpoint publisher,
    AppDbContext db,
    IStorageService storage) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List(
        [FromQuery] ReturnStatus? status,
        [FromQuery] string? carrier,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var list = await returns.ListAdminAsync(status, carrier, from, to, page, pageSize, ct);
        return Ok(list.Select(r => new ReturnSummaryResponse(
            r.Id, r.RequestNumber, r.Status.ToString(), r.ResolutionType.ToString(),
            r.Customer.Name, r.CarrierCode, r.CreatedAt)));
    }

    [HttpGet("stats")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Stats(CancellationToken ct)
    {
        var raw = await returns.GetStatsAsync(ct);
        return Ok(new StatsResponse(raw));
    }

    [HttpGet("{returnId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(Guid returnId, CancellationToken ct)
    {
        var r = await returns.FindByIdAsync(returnId, ct);
        if (r is null) return NotFound();

        return Ok(new ReturnDetailResponse(
            r.Id, r.RequestNumber, r.Status.ToString(), r.ResolutionType.ToString(),
            r.ReasonCode, r.ReasonNotes, r.VoucherCode, r.ErpReturnOrderId, r.CreatedAt,
            new(r.Customer.Name, r.Customer.Email, r.Customer.Cpf),
            new(r.Order.ExternalOrderId, r.Order.Total, r.Order.Currency),
            r.Shipment is null ? null : new(
                r.Shipment.TrackingCode, r.Shipment.CarrierCode,
                r.Shipment.Status, r.Shipment.EstimatedDeliveryDate, r.Shipment.DeliveredAt),
            r.Items.Select(i => new ReturnItemResponse(
                i.Id, i.OrderItem.Sku, i.OrderItem.Name,
                i.QuantityReturned, i.OrderItem.UnitPrice, i.Condition?.ToString())).ToList(),
            r.Events.Select(e => new ReturnEventResponse(e.EventType, e.Actor, e.OccurredAt)).ToList()
        ));
    }

    [HttpPatch("{returnId:guid}/quality")]
    [Authorize(Roles = "QualityInspector,Supervisor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssessQuality(
        Guid returnId, [FromBody] QualityAssessmentDto dto, CancellationToken ct)
    {
        var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "admin";

        var persistence = new AdminReturnPersistenceAdapter(returns, publisher);
        var useCase = new AssessQuality(persistence, new MassTransitPublisherAdmin(publisher));

        var result = await useCase.ExecuteAsync(new AssessQualityInput(
            returnId, adminName, dto.Result, dto.Notes, dto.DamageImageKeys), ct);

        return Ok(new { result.Status });
    }

    // Directly creates the shipment + transitions PendingLabel → LabelGenerated without going through RabbitMQ.
    [HttpPost("{returnId:guid}/simulate-label")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SimulateLabel(Guid returnId, CancellationToken ct)
    {
        // Load ReturnRequest only (no navigation includes) to keep the change tracker minimal
        var r = await db.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == returnId, ct);

        if (r is null) return NotFound();
        if (r.Status != ReturnStatus.PendingLabel)
            return Ok(new { message = $"Cannot simulate label in status {r.Status}." });

        var alreadyHasShipment = await db.Shipments.AnyAsync(s => s.ReturnRequestId == returnId, ct);
        if (alreadyHasShipment)
            return BadRequest(new { error = "Shipment already exists." });

        var trackingCode = $"BR{Guid.NewGuid().ToString("N")[..12].ToUpper()}SB";
        var labelKey = $"labels/{r.Id}/label.pdf";

        using var labelStream = new MemoryStream(
            System.Text.Encoding.UTF8.GetBytes($"MOCK LABEL — {r.RequestNumber} — {trackingCode}"));
        await storage.UploadFileAsync(labelKey, labelStream, "application/pdf", ct);

        // Add Shipment directly to avoid EF navigation-property tracking conflicts
        db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            ReturnRequestId = r.Id,
            CarrierCode = "mock",
            TrackingCode = trackingCode,
            LabelStorageKey = labelKey,
            Status = "LabelGenerated",
            EstimatedDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            CarrierPayload = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        r.CarrierCode = "mock";
        var evt = ReturnRequestStateMachine.Apply(r, "LabelGenerated", "system:admin-simulate");
        db.ReturnEvents.Add(evt);
        await db.SaveChangesAsync(ct);

        return Ok(new { trackingCode });
    }

    // Directly transitions InTransit → Delivered without going through RabbitMQ.
    [HttpPost("{returnId:guid}/simulate-delivery")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SimulateDelivery(Guid returnId, CancellationToken ct)
    {
        var r = await db.ReturnRequests.FirstOrDefaultAsync(r => r.Id == returnId, ct);
        if (r is null) return NotFound();
        if (r.Status != ReturnStatus.InTransit)
            return Ok(new { message = $"Cannot simulate delivery in status {r.Status}." });

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.ReturnRequestId == returnId, ct);
        if (shipment is null)
            return BadRequest(new { error = "No shipment found — generate label first." });

        shipment.Status = "Delivered";
        shipment.DeliveredAt = DateTimeOffset.UtcNow;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        var evt = ReturnRequestStateMachine.Apply(r, "CarrierDelivered", "system:admin-simulate",
            new { DeliveredAt = DateTimeOffset.UtcNow });
        db.ReturnEvents.Add(evt);
        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Delivery simulated." });
    }

    [HttpPost("{returnId:guid}/cancel")]
    [Authorize(Roles = "Supervisor,Agent")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(Guid returnId, CancellationToken ct)
    {
        var r = await returns.FindByIdAsync(returnId, ct);
        if (r is null) return NotFound();

        var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "admin";
        var evt = ReturnRequestStateMachine.Apply(r, "Cancel", $"admin:{adminName}");
        returns.AddEvent(null, evt);
        await returns.SaveAsync(ct);

        return Ok(new { r.Status });
    }
}

internal class AdminReturnPersistenceAdapter(ReturnRequestRepository repo, IPublishEndpoint _) : IReturnRequestPersistence
{
    private readonly List<ReturnEvent> _events = [];
    private readonly List<QualityAssessment> _assessments = [];

    public Task<ReturnRequest?> FindByIdAsync(Guid id, CancellationToken ct) =>
        repo.FindByIdAsync(id, ct);

    public Task SaveAsync(CancellationToken ct)
    {
        foreach (var e in _events) repo.AddEvent(null, e);
        foreach (var a in _assessments) repo.AddQualityAssessment(a);
        return repo.SaveAsync(ct);
    }

    public void AddEvent(ReturnEvent evt) => _events.Add(evt);
    public void AddQualityAssessment(QualityAssessment assessment) => _assessments.Add(assessment);
}

internal class MassTransitPublisherAdmin(IPublishEndpoint bus) : IEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken ct) where T : class =>
        bus.Publish(message, ct);
}
