using System.Security.Claims;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Ports;
using DevolveFacill.Core.UseCases;
using DevolveFacill.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevolveFacill.Api.Controllers;

[ApiController]
[Route("api/customer/orders")]
[Authorize(Roles = "customer")]
public class CustomerOrdersController(
    CustomerRepository customers,
    OrderRepository orders,
    ICommercePlatform commerce) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListOrders(CancellationToken ct)
    {
        var customer = await GetCustomerAsync(ct);
        if (customer is null) return Unauthorized();

        var useCase = new GetCustomerOrders(commerce, new OrderCacheAdapter(orders));
        var result = await useCase.ExecuteAsync(customer, ct);
        return Ok(result);
    }

    [HttpGet("{externalOrderId}")]
    public async Task<IActionResult> GetOrder(string externalOrderId, CancellationToken ct)
    {
        var order = await orders.FindByExternalIdAsync(externalOrderId, ct);
        if (order is null)
        {
            var summary = await commerce.GetOrderAsync(externalOrderId, ct);
            return Ok(summary);
        }
        return Ok(new
        {
            order.ExternalOrderId,
            order.Platform,
            order.Total,
            order.Currency,
            order.OrderedAt,
            EligibleForReturn = (DateTimeOffset.UtcNow - order.OrderedAt).TotalDays <= 30,
            ReturnDeadlineDays = Math.Max(0, 30 - (int)(DateTimeOffset.UtcNow - order.OrderedAt).TotalDays),
            Items = order.Items.Select(i => new
            {
                Id = i.Id,
                i.Sku,
                i.Name,
                i.Quantity,
                i.UnitPrice,
                i.ImageUrl
            })
        });
    }

    private async Task<Customer?> GetCustomerAsync(CancellationToken ct)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(id, out var guid)) return null;
        return await customers.FindByIdAsync(guid, ct);
    }
}

internal class OrderCacheAdapter(OrderRepository repo) : IOrderCache
{
    public Task<List<Order>> FindByCustomerIdAsync(Guid customerId, CancellationToken ct) =>
        repo.FindByCustomerIdAsync(customerId, ct);

    public Task UpsertAsync(Order order, CancellationToken ct) =>
        repo.UpsertAsync(order, ct);
}
