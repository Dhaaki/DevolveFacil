using System.Text.Json;
using DevolveFacill.Core.Ports;

namespace DevolveFacill.Adapters.Carrier.Mock;

public class MockCarrier : ICarrier
{
    public string CarrierCode => "mock";

    public Task<ShippingLabelResult> GenerateReturnLabelAsync(ReturnLabelRequest request, CancellationToken ct = default)
        => Task.FromResult(new ShippingLabelResult(
            TrackingCode: $"BR{Random.Shared.NextInt64(100000000, 999999999)}BR",
            LabelUrl: "https://via.placeholder.com/label.pdf",
            CarrierCode: CarrierCode,
            EstimatedDeliveryDays: 7
        ));

    public Task<IReadOnlyList<TrackingEvent>> GetTrackingHistoryAsync(string trackingCode, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TrackingEvent>>(
        [
            new("Posted",     "Objeto postado",              "São Paulo/SP",      DateTimeOffset.UtcNow.AddDays(-3), false),
            new("InTransit",  "Objeto em trânsito",          "Curitiba/PR",       DateTimeOffset.UtcNow.AddDays(-1), false),
            new("Delivered",  "Objeto entregue ao destinatário", "Joinville/SC",  DateTimeOffset.UtcNow,             true),
        ]);

    public bool ValidateWebhookSignature(byte[] payload, string signature) => true;

    public DeliveryWebhookEvent ParseDeliveryWebhook(JsonDocument payload)
        => new(
            TrackingCode: payload.RootElement.GetProperty("tracking_code").GetString() ?? string.Empty,
            IsDelivered: true,
            DeliveredAt: DateTimeOffset.UtcNow
        );
}
