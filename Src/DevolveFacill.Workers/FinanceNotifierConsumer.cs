using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Domain.Enums;
using DevolveFacill.Core.Events;
using DevolveFacill.Core.Ports;
using DevolveFacill.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Workers;

public class FinanceNotifierConsumer(
    AppDbContext db,
    IErpIntegration erp,
    ILogger<FinanceNotifierConsumer> logger) : IConsumer<QualityApprovedEvent>
{
    public async Task Consume(ConsumeContext<QualityApprovedEvent> context)
    {
        var msg = context.Message;
        if (msg.ResolutionType != ResolutionType.Refund) return;

        logger.LogInformation("Notifying finance for refund on return {RequestNumber}", msg.RequestNumber);

        var returnRequest = await db.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == msg.ReturnRequestId, context.CancellationToken);

        if (returnRequest is null) return;

        // Idempotency
        if (returnRequest.FinanceNotifiedAt.HasValue)
        {
            logger.LogInformation("Finance already notified for {RequestNumber}", msg.RequestNumber);
            return;
        }

        await erp.NotifyFinanceDepartmentAsync(new FinanceNotificationRequest(
            ReturnRequestId: msg.ReturnRequestId.ToString(),
            CustomerId: msg.CustomerExternalId,
            CustomerName: msg.CustomerName,
            CustomerEmail: msg.CustomerEmail,
            RefundAmount: msg.OrderTotal,
            Currency: msg.Currency,
            BankAgency: null,
            BankAccount: null,
            BankCode: null
        ), context.CancellationToken);

        returnRequest.FinanceNotifiedAt = DateTimeOffset.UtcNow;

        var evt = ReturnRequestStateMachine.Apply(returnRequest, "FinanceNotified", "system:worker");
        db.ReturnEvents.Add(evt);

        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Finance notified for refund on {RequestNumber}", msg.RequestNumber);
    }
}
