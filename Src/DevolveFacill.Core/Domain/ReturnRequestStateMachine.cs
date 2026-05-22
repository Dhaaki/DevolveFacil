using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Domain.Enums;

namespace DevolveFacill.Core.Domain;

public class InvalidTransitionException(ReturnStatus from, string transition)
    : Exception($"Cannot apply transition '{transition}' from status '{from}'.");

public static class ReturnRequestStateMachine
{
    private record Transition(ReturnStatus[] From, ReturnStatus To, Func<ReturnRequest, bool>? Guard = null);

    private static readonly Dictionary<string, Transition> Transitions = new()
    {
        ["Submit"]            = new([ReturnStatus.Draft],          ReturnStatus.PendingLabel),
        ["LabelGenerated"]    = new([ReturnStatus.PendingLabel],   ReturnStatus.LabelGenerated),
        ["CustomerPosted"]    = new([ReturnStatus.LabelGenerated], ReturnStatus.InTransit),
        ["CarrierDelivered"]  = new([ReturnStatus.InTransit],      ReturnStatus.Delivered),
        ["QualityApproved"]   = new([ReturnStatus.Delivered],      ReturnStatus.QualityApproved),
        ["QualityRejected"]   = new([ReturnStatus.Delivered],      ReturnStatus.QualityRejected),
        ["InitiateRefund"]    = new([ReturnStatus.QualityApproved],ReturnStatus.PendingRefund,
            r => r.ResolutionType == ResolutionType.Refund),
        ["InitiateVoucher"]   = new([ReturnStatus.QualityApproved],ReturnStatus.PendingVoucher,
            r => r.ResolutionType == ResolutionType.StoreCredit),
        ["FinanceNotified"]   = new([ReturnStatus.PendingRefund],  ReturnStatus.ClosedRefund),
        ["VoucherCreated"]    = new([ReturnStatus.PendingVoucher], ReturnStatus.ClosedExchange),
        ["CustomerNotified"]  = new([ReturnStatus.QualityRejected],ReturnStatus.ClosedRejected),
        ["Cancel"]            = new([ReturnStatus.Draft, ReturnStatus.PendingLabel, ReturnStatus.LabelGenerated],
            ReturnStatus.Cancelled),
        ["MarkError"]         = new(Enum.GetValues<ReturnStatus>().Except([ReturnStatus.ClosedRefund,
            ReturnStatus.ClosedExchange, ReturnStatus.ClosedRejected, ReturnStatus.Cancelled, ReturnStatus.Error]).ToArray(),
            ReturnStatus.Error),
    };

    public static ReturnEvent Apply(ReturnRequest request, string transition, string actor, object? payload = null)
    {
        if (!Transitions.TryGetValue(transition, out var def))
            throw new InvalidTransitionException(request.Status, transition);

        if (!def.From.Contains(request.Status))
            throw new InvalidTransitionException(request.Status, transition);

        if (def.Guard != null && !def.Guard(request))
            throw new InvalidTransitionException(request.Status, $"{transition} (guard failed)");

        request.Status = def.To;
        request.UpdatedAt = DateTimeOffset.UtcNow;

        return new ReturnEvent
        {
            Id = Guid.NewGuid(),
            ReturnRequestId = request.Id,
            EventType = transition,
            Actor = actor,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload ?? new { }),
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    public static bool CanApply(ReturnRequest request, string transition)
    {
        if (!Transitions.TryGetValue(transition, out var def)) return false;
        if (!def.From.Contains(request.Status)) return false;
        if (def.Guard != null && !def.Guard(request)) return false;
        return true;
    }
}
