using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Events;
using DevolveFacill.Core.Ports;
using DevolveFacill.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Workers;

public class LabelGeneratorConsumer(
    AppDbContext db,
    ICarrier carrier,
    IStorageService storage,
    ILogger<LabelGeneratorConsumer> logger) : IConsumer<ReturnSubmittedEvent>
{
    public async Task Consume(ConsumeContext<ReturnSubmittedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Generating label for return {RequestNumber}", msg.RequestNumber);

        var returnRequest = await db.ReturnRequests
            .Include(r => r.Customer)
            .Include(r => r.Order)
            .Include(r => r.Shipment)
            .FirstOrDefaultAsync(r => r.Id == msg.ReturnRequestId, context.CancellationToken);

        if (returnRequest is null)
        {
            logger.LogWarning("ReturnRequest {Id} not found — skipping label generation", msg.ReturnRequestId);
            return;
        }

        // Idempotency: skip if label already generated
        if (returnRequest.Shipment?.LabelStorageKey is not null)
        {
            logger.LogInformation("Label already exists for {RequestNumber} — skipping", msg.RequestNumber);
            return;
        }

        var receiverAddress = new Address(
            Name: "La Moda Devoluções",
            Street: "Rua das Devoluções",
            Number: "100",
            Complement: null,
            District: "Centro",
            City: "Joinville",
            State: "SC",
            PostalCode: "89200-000",
            Country: "BR"
        );

        var senderAddress = new Address(
            Name: returnRequest.Customer.Name,
            Street: "Endereço do Cliente",
            Number: "1",
            Complement: null,
            District: "Bairro",
            City: "Cidade",
            State: "SC",
            PostalCode: "89000-000",
            Country: "BR"
        );

        var labelResult = await carrier.GenerateReturnLabelAsync(new ReturnLabelRequest(
            ReturnRequestId: returnRequest.Id.ToString(),
            SenderAddress: senderAddress,
            ReceiverAddress: receiverAddress,
            PackageWeightKg: 0.5m,
            DeclaredValue: returnRequest.Order.Total,
            Currency: returnRequest.Order.Currency
        ), context.CancellationToken);

        var labelKey = $"labels/{returnRequest.Id}/label.pdf";
        using var labelStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"MOCK LABEL: {labelResult.TrackingCode}"));
        await storage.UploadFileAsync(labelKey, labelStream, "application/pdf", context.CancellationToken);

        if (returnRequest.Shipment is null)
        {
            returnRequest.Shipment = new Core.Domain.Entities.Shipment
            {
                Id = Guid.NewGuid(),
                ReturnRequestId = returnRequest.Id,
                CarrierCode = labelResult.CarrierCode,
                TrackingCode = labelResult.TrackingCode,
                LabelStorageKey = labelKey,
                Status = "LabelGenerated",
                EstimatedDeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(labelResult.EstimatedDeliveryDays)),
                CarrierPayload = "{}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }
        else
        {
            returnRequest.Shipment.TrackingCode = labelResult.TrackingCode;
            returnRequest.Shipment.LabelStorageKey = labelKey;
            returnRequest.Shipment.UpdatedAt = DateTimeOffset.UtcNow;
        }

        returnRequest.CarrierCode = labelResult.CarrierCode;
        var evt = ReturnRequestStateMachine.Apply(returnRequest, "LabelGenerated", "system:worker");
        db.ReturnEvents.Add(evt);

        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Label generated for {RequestNumber}, tracking: {TrackingCode}",
            msg.RequestNumber, labelResult.TrackingCode);
    }
}
