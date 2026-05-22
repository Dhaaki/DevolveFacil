using DevolveFacill.Core.Domain.Enums;

namespace DevolveFacill.Api.DTOs;

// ----- Requests -----

public record CreateReturnRequestDto(
    string OrderExternalId,
    ResolutionType ResolutionType,
    string ReasonCode,
    string? ReasonNotes,
    IReadOnlyList<ReturnItemDto> Items
);

public record ReturnItemDto(Guid OrderItemId, int QuantityReturned);

public record QualityAssessmentDto(
    QualityResult Result,
    string? Notes,
    IReadOnlyList<string> DamageImageKeys
);

// ----- Responses -----

public record ReturnSummaryResponse(
    Guid Id,
    string RequestNumber,
    string Status,
    string ResolutionType,
    string CustomerName,
    string? CarrierCode,
    DateTimeOffset CreatedAt
);

public record ReturnDetailResponse(
    Guid Id,
    string RequestNumber,
    string Status,
    string ResolutionType,
    string ReasonCode,
    string? ReasonNotes,
    string? VoucherCode,
    string? ErpReturnOrderId,
    DateTimeOffset CreatedAt,
    CustomerBriefResponse Customer,
    OrderBriefResponse Order,
    ShipmentResponse? Shipment,
    IReadOnlyList<ReturnItemResponse> Items,
    IReadOnlyList<ReturnEventResponse> Events
);

public record CustomerBriefResponse(string Name, string Email, string Cpf);

public record OrderBriefResponse(string ExternalOrderId, decimal Total, string Currency);

public record ShipmentResponse(
    string TrackingCode,
    string CarrierCode,
    string Status,
    DateOnly? EstimatedDeliveryDate,
    DateTimeOffset? DeliveredAt
);

public record ReturnItemResponse(
    Guid Id,
    string Sku,
    string Name,
    int QuantityReturned,
    decimal UnitPrice,
    string? Condition
);

public record ReturnEventResponse(
    string EventType,
    string Actor,
    DateTimeOffset OccurredAt
);

public record StatsResponse(Dictionary<string, int> ByStatus);
