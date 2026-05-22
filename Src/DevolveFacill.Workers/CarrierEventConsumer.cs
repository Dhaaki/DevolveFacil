using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Events;
using DevolveFacill.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Workers;

public class CarrierEventConsumer(
    AppDbContext db,
    ILogger<CarrierEventConsumer> logger) : IConsumer<CarrierDeliveryEvent>
{
    public async Task Consume(ConsumeContext<CarrierDeliveryEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Processing delivery for tracking code {TrackingCode}", msg.TrackingCode);

        var shipment = await db.Shipments
            .Include(s => s.ReturnRequest)
            .ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(s => s.TrackingCode == msg.TrackingCode, context.CancellationToken);

        if (shipment is null)
        {
            logger.LogWarning("Shipment with tracking {TrackingCode} not found", msg.TrackingCode);
            return;
        }

        // Idempotency
        if (shipment.ReturnRequest.Status == Core.Domain.Enums.ReturnStatus.Delivered)
        {
            logger.LogInformation("ReturnRequest {Id} already delivered — skipping", shipment.ReturnRequestId);
            return;
        }

        shipment.Status = "Delivered";
        shipment.DeliveredAt = msg.DeliveredAt;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;

        var evt = ReturnRequestStateMachine.Apply(shipment.ReturnRequest, "CarrierDelivered", "system:worker",
            new { TrackingCode = msg.TrackingCode, DeliveredAt = msg.DeliveredAt });
        db.ReturnEvents.Add(evt);

        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Return {Id} marked as DELIVERED", shipment.ReturnRequestId);
    }
}
