using Asp.Versioning;
using System.Text.Json;
using DevolveFacill.Core.Events;
using DevolveFacill.Core.Ports;
using MassTransit;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace DevolveFacill.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks")]
[EnableCors("WebhookPolicy")]
public class WebhooksController(ICarrier carrier, IPublishEndpoint publisher) : ControllerBase
{
    [HttpPost("carrier/{carrierCode}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CarrierWebhook(string carrierCode, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        var body = ms.ToArray();

        var signature = Request.Headers["X-Signature"].FirstOrDefault() ?? string.Empty;
        if (!carrier.ValidateWebhookSignature(body, signature))
            return Unauthorized();

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch
        {
            return BadRequest(new { error = "Invalid JSON payload." });
        }

        var evt = carrier.ParseDeliveryWebhook(doc);
        if (!evt.IsDelivered) return Ok();

        await publisher.Publish(new CarrierDeliveryEvent
        {
            TrackingCode = evt.TrackingCode,
            CarrierCode = carrierCode,
            DeliveredAt = evt.DeliveredAt
        }, ct);

        return Ok();
    }
}
