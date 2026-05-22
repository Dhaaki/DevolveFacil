using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Domain.Enums;
using DevolveFacill.Core.Events;

namespace DevolveFacill.Core.UseCases;

public record AssessQualityInput(
    Guid ReturnRequestId,
    string AdminName,
    QualityResult Result,
    string? Notes,
    IReadOnlyList<string> DamageImageKeys
);

public class AssessQuality(IReturnRequestPersistence persistence, IEventPublisher publisher)
{
    public async Task<ReturnRequest> ExecuteAsync(AssessQualityInput input, CancellationToken ct = default)
    {
        var request = await persistence.FindByIdAsync(input.ReturnRequestId, ct)
            ?? throw new KeyNotFoundException($"ReturnRequest {input.ReturnRequestId} not found.");

        if (request.Status != ReturnStatus.Delivered)
            throw new InvalidOperationException($"Cannot assess quality in status {request.Status}.");

        var assessment = new QualityAssessment
        {
            Id = Guid.NewGuid(),
            ReturnRequestId = request.Id,
            AssessedBy = input.AdminName,
            Result = input.Result,
            Notes = input.Notes,
            DamageImageKeys = [.. input.DamageImageKeys],
            AssessedAt = DateTimeOffset.UtcNow
        };
        persistence.AddQualityAssessment(assessment);

        var transition = input.Result == QualityResult.Approved ? "QualityApproved" : "QualityRejected";
        var evt = ReturnRequestStateMachine.Apply(request, transition, $"admin:{input.AdminName}",
            new { Result = input.Result.ToString(), input.Notes });
        persistence.AddEvent(evt);

        if (input.Result == QualityResult.Approved)
        {
            var postQualityTransition = request.ResolutionType == ResolutionType.Refund
                ? "InitiateRefund" : "InitiateVoucher";
            var postEvt = ReturnRequestStateMachine.Apply(request, postQualityTransition, "system");
            persistence.AddEvent(postEvt);

            await persistence.SaveAsync(ct);

            await publisher.PublishAsync(new QualityApprovedEvent
            {
                ReturnRequestId = request.Id,
                RequestNumber = request.RequestNumber,
                ResolutionType = request.ResolutionType,
                CustomerExternalId = request.Customer.ExternalId,
                CustomerName = request.Customer.Name,
                CustomerEmail = request.Customer.Email,
                CustomerCpf = request.Customer.Cpf,
                OrderTotal = request.Order.Total,
                Currency = request.Order.Currency
            }, ct);
        }
        else
        {
            await persistence.SaveAsync(ct);

            await publisher.PublishAsync(new QualityRejectedEvent
            {
                ReturnRequestId = request.Id,
                RequestNumber = request.RequestNumber,
                CustomerName = request.Customer.Name,
                CustomerEmail = request.Customer.Email,
                Notes = input.Notes,
                DamageImageKeys = input.DamageImageKeys
            }, ct);
        }

        return request;
    }
}
