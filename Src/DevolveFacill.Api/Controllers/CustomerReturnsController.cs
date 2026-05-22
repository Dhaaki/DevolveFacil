using System.Security.Claims;
using DevolveFacill.Api.DTOs;
using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Domain.Entities;
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
[Route("api/customer/returns")]
[Authorize(Roles = "customer")]
public class CustomerReturnsController(
    CustomerRepository customers,
    OrderRepository orders,
    ReturnRequestRepository returns,
    IStorageService storage,
    IPublishEndpoint publisher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateReturn([FromBody] CreateReturnRequestDto dto, CancellationToken ct)
    {
        var customer = await GetCustomerAsync(ct);
        if (customer is null) return Unauthorized();

        var order = await orders.FindByExternalIdAsync(dto.OrderExternalId, ct);
        if (order is null) return NotFound(new { error = "Pedido não encontrado." });

        if (order.CustomerId != customer.Id)
            return Forbid();

        var writer = new ReturnWriterAdapter(returns);
        var createUseCase = new CreateReturnRequest(writer);

        var created = await createUseCase.ExecuteAsync(new CreateReturnInput(
            CustomerId: customer.Id,
            Platform: customer.Platform,
            OrderId: order.Id,
            ResolutionType: dto.ResolutionType,
            ReasonCode: dto.ReasonCode,
            ReasonNotes: dto.ReasonNotes,
            Items: dto.Items.Select(i => new ReturnItemInput(i.OrderItemId, i.QuantityReturned)).ToList()
        ), ct);

        var persistence = new ReturnPersistenceAdapter(returns, publisher);
        var submitUseCase = new SubmitReturnRequest(persistence, new MassTransitPublisher(publisher));
        var submitted = await submitUseCase.ExecuteAsync(created.Id, customer.Id.ToString(), ct);

        return CreatedAtAction(nameof(GetReturn), new { returnId = submitted.Id },
            new { id = submitted.Id, submitted.RequestNumber, submitted.Status });
    }

    [HttpGet]
    public async Task<IActionResult> ListReturns(CancellationToken ct)
    {
        var customer = await GetCustomerAsync(ct);
        if (customer is null) return Unauthorized();

        var list = await returns.FindByCustomerIdAsync(customer.Id, ct);
        return Ok(list.Select(r => new ReturnSummaryResponse(
            r.Id, r.RequestNumber, r.Status.ToString(), r.ResolutionType.ToString(),
            customer.Name, r.CarrierCode, r.CreatedAt)));
    }

    [HttpGet("{returnId:guid}")]
    public async Task<IActionResult> GetReturn(Guid returnId, CancellationToken ct)
    {
        var customer = await GetCustomerAsync(ct);
        if (customer is null) return Unauthorized();

        var r = await returns.FindByIdAsync(returnId, ct);
        if (r is null) return NotFound();
        if (r.CustomerId != customer.Id) return Forbid();

        return Ok(MapDetail(r));
    }

    [HttpGet("{returnId:guid}/label")]
    public async Task<IActionResult> GetLabel(Guid returnId, CancellationToken ct)
    {
        var customer = await GetCustomerAsync(ct);
        if (customer is null) return Unauthorized();

        var r = await returns.FindByIdAsync(returnId, ct);
        if (r is null || r.CustomerId != customer.Id) return NotFound();
        if (r.Shipment?.LabelStorageKey is null) return NotFound(new { error = "Etiqueta ainda não gerada." });

        var url = await storage.GetSignedUrlAsync(r.Shipment.LabelStorageKey, TimeSpan.FromMinutes(15), ct);
        return Redirect(url);
    }

    [HttpPost("{returnId:guid}/confirm-posted")]
    public async Task<IActionResult> ConfirmPosted(Guid returnId, CancellationToken ct)
    {
        var customer = await GetCustomerAsync(ct);
        if (customer is null) return Unauthorized();

        var r = await returns.FindByIdAsync(returnId, ct);
        if (r is null || r.CustomerId != customer.Id) return NotFound();

        var evt = ReturnRequestStateMachine.Apply(r, "CustomerPosted", $"customer:{customer.Id}");
        returns.AddEvent(r, evt);
        await returns.SaveAsync(ct);

        return Ok(new { r.Status });
    }

    private static ReturnDetailResponse MapDetail(ReturnRequest r) => new(
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
    );

    private async Task<Core.Domain.Entities.Customer?> GetCustomerAsync(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(id, out var guid)) return null;
        return await customers.FindByIdAsync(guid, ct);
    }
}

internal class ReturnWriterAdapter(ReturnRequestRepository repo) : IReturnRequestWriter
{
    public Task<string> NextRequestNumberAsync(CancellationToken ct) => repo.NextRequestNumberAsync(ct);
    public Task<ReturnRequest> CreateAsync(ReturnRequest r, CancellationToken ct) => repo.CreateAsync(r, ct);
    public Task<OrderItem?> FindItemByIdAsync(Guid itemId, CancellationToken ct) => repo.FindItemByIdAsync(itemId, ct);
}

internal class ReturnPersistenceAdapter(ReturnRequestRepository repo, IPublishEndpoint _) : IReturnRequestPersistence
{
    private readonly List<ReturnEvent> _events = [];

    public Task<ReturnRequest?> FindByIdAsync(Guid id, CancellationToken ct) => repo.FindByIdAsync(id, ct);
    public Task SaveAsync(CancellationToken ct)
    {
        foreach (var e in _events) repo.AddEvent(null!, e);
        return repo.SaveAsync(ct);
    }
    public void AddEvent(ReturnEvent evt) => _events.Add(evt);
    public void AddQualityAssessment(QualityAssessment assessment) => repo.AddQualityAssessment(assessment);
}

internal class MassTransitPublisher(IPublishEndpoint bus) : IEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken ct) where T : class =>
        bus.Publish(message, ct);
}
