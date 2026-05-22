using DevolveFacill.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Workers;

// Observability consumer: logs the quality routing decision.
// VoucherConsumer and FinanceNotifierConsumer each consume QualityApprovedEvent directly
// from their own queues and self-filter by ResolutionType — no re-publishing needed.
public class PostQualityConsumer(ILogger<PostQualityConsumer> logger) : IConsumer<QualityApprovedEvent>
{
    public Task Consume(ConsumeContext<QualityApprovedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation(
            "Quality approved for {RequestNumber} — routing to {ResolutionType} flow",
            msg.RequestNumber, msg.ResolutionType);
        return Task.CompletedTask;
    }
}
