using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Events;
using DevolveFacill.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Workers;

public class CustomerNotifierConsumer(
    AppDbContext db,
    ILogger<CustomerNotifierConsumer> logger) : IConsumer<QualityRejectedEvent>
{
    public async Task Consume(ConsumeContext<QualityRejectedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Sending rejection notification for return {RequestNumber}", msg.RequestNumber);

        var returnRequest = await db.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == msg.ReturnRequestId, context.CancellationToken);

        if (returnRequest is null) return;

        // Idempotency: already closed
        if (returnRequest.Status == Core.Domain.Enums.ReturnStatus.ClosedRejected) return;

        // TODO: send email via MailKit to msg.CustomerEmail
        // For now, log the notification
        logger.LogInformation(
            "[EMAIL] Rejection notification to {Email} for return {RequestNumber}. Notes: {Notes}",
            msg.CustomerEmail, msg.RequestNumber, msg.Notes);

        var evt = ReturnRequestStateMachine.Apply(returnRequest, "CustomerNotified", "system:worker",
            new { Reason = msg.Notes });
        db.ReturnEvents.Add(evt);

        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Rejection notification sent for {RequestNumber}", msg.RequestNumber);
    }
}
