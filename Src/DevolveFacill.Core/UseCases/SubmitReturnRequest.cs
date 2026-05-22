using DevolveFacill.Core.Domain;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Events;

namespace DevolveFacill.Core.UseCases;

public interface IReturnRequestPersistence
{
    Task<ReturnRequest?> FindByIdAsync(Guid id, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    void AddEvent(ReturnEvent evt);
    void AddQualityAssessment(QualityAssessment assessment);
}

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct) where T : class;
}

public class SubmitReturnRequest(IReturnRequestPersistence persistence, IEventPublisher publisher)
{
    public async Task<ReturnRequest> ExecuteAsync(Guid returnRequestId, string actorId, CancellationToken ct = default)
    {
        var request = await persistence.FindByIdAsync(returnRequestId, ct)
            ?? throw new KeyNotFoundException($"ReturnRequest {returnRequestId} not found.");

        if (request.CustomerId.ToString() != actorId)
            throw new UnauthorizedAccessException("Not your return request.");

        var evt = ReturnRequestStateMachine.Apply(request, "Submit", $"customer:{actorId}");
        persistence.AddEvent(evt);
        await persistence.SaveAsync(ct);

        await publisher.PublishAsync(new ReturnSubmittedEvent
        {
            ReturnRequestId = request.Id,
            RequestNumber = request.RequestNumber,
            CarrierCode = "correios",
            CustomerName = request.Customer.Name,
            CustomerEmail = request.Customer.Email,
            SubmittedAt = DateTimeOffset.UtcNow
        }, ct);

        return request;
    }
}
