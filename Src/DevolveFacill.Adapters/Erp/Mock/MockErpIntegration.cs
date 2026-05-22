using DevolveFacill.Core.Ports;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Adapters.Erp.Mock;

public class MockErpIntegration(ILogger<MockErpIntegration> logger) : IErpIntegration
{
    public Task<string> CreateReturnOrderAsync(ReturnOrderRequest request, CancellationToken ct = default)
    {
        var erpId = $"ERP-RET-{Guid.NewGuid():N}"[..16].ToUpper();
        logger.LogInformation("[MOCK ERP] Created return order {ErpId} for return {ReturnId}", erpId, request.ReturnRequestId);
        return Task.FromResult(erpId);
    }

    public Task NotifyFinanceDepartmentAsync(FinanceNotificationRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[MOCK ERP] Finance notification sent for return {ReturnId} — amount {Amount} {Currency} to {Email}",
            request.ReturnRequestId, request.RefundAmount, request.Currency, request.CustomerEmail);
        return Task.CompletedTask;
    }
}
