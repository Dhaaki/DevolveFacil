using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Domain.Enums;
using DevolveFacill.Core.Events;
using DevolveFacill.Core.Ports;
using DevolveFacill.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Workers;

public class VoucherConsumer(
    AppDbContext db,
    ICommercePlatform commerce,
    IErpIntegration erp,
    ILogger<VoucherConsumer> logger) : IConsumer<QualityApprovedEvent>
{
    public async Task Consume(ConsumeContext<QualityApprovedEvent> context)
    {
        var msg = context.Message;
        if (msg.ResolutionType != ResolutionType.StoreCredit) return;

        logger.LogInformation("Creating store credit voucher for return {RequestNumber}", msg.RequestNumber);

        var returnRequest = await db.ReturnRequests
            .Include(r => r.Items).ThenInclude(i => i.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == msg.ReturnRequestId, context.CancellationToken);

        if (returnRequest is null) return;

        // Idempotency
        if (returnRequest.VoucherCode is not null)
        {
            logger.LogInformation("Voucher already created for {RequestNumber}", msg.RequestNumber);
            return;
        }

        var voucherResult = await commerce.CreateStoreCreditAsync(new StoreCreditRequest(
            ExternalCustomerId: msg.CustomerExternalId,
            Value: msg.OrderTotal,
            ReturnRequestId: msg.ReturnRequestId.ToString(),
            Currency: msg.Currency
        ), context.CancellationToken);

        var erpOrderId = await erp.CreateReturnOrderAsync(new ReturnOrderRequest(
            ReturnRequestId: msg.ReturnRequestId.ToString(),
            Items: returnRequest.Items.Select(i => new ReturnOrderItem(
                i.OrderItem.Sku, i.QuantityReturned, i.OrderItem.UnitPrice)).ToList(),
            CustomerDocument: msg.CustomerCpf,
            CustomerName: msg.CustomerName
        ), context.CancellationToken);

        returnRequest.VoucherCode = voucherResult.VoucherCode;
        returnRequest.ErpReturnOrderId = erpOrderId;

        var evt = ReturnRequestStateMachine.Apply(returnRequest, "VoucherCreated", "system:worker",
            new { VoucherCode = voucherResult.VoucherCode, ErpOrderId = erpOrderId });
        db.ReturnEvents.Add(evt);

        await db.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("Store credit {VoucherCode} issued for {RequestNumber}", voucherResult.VoucherCode, msg.RequestNumber);
    }
}
