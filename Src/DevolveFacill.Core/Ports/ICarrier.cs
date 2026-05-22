using System.Text.Json;

namespace DevolveFacill.Core.Ports;

public record Address(
    string Name,
    string Street,
    string Number,
    string? Complement,
    string District,
    string City,
    string State,
    string PostalCode,
    string Country
);

public record ReturnLabelRequest(
    string ReturnRequestId,
    Address SenderAddress,
    Address ReceiverAddress,
    decimal PackageWeightKg,
    decimal DeclaredValue,
    string Currency
);

public record ShippingLabelResult(
    string TrackingCode,
    string LabelUrl,
    string CarrierCode,
    int EstimatedDeliveryDays
);

public record TrackingEvent(
    string Status,
    string Description,
    string Location,
    DateTimeOffset OccurredAt,
    bool IsDelivered
);

public record DeliveryWebhookEvent(
    string TrackingCode,
    bool IsDelivered,
    DateTimeOffset DeliveredAt
);

public interface ICarrier
{
    string CarrierCode { get; }
    Task<ShippingLabelResult> GenerateReturnLabelAsync(ReturnLabelRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TrackingEvent>> GetTrackingHistoryAsync(string trackingCode, CancellationToken ct = default);
    bool ValidateWebhookSignature(byte[] payload, string signature);
    DeliveryWebhookEvent ParseDeliveryWebhook(JsonDocument payload);
}
