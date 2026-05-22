using DevolveFacill.Core.Domain.Enums;
using DevolveFacill.Core.Events;
using DevolveFacill.Core.Ports;
using DevolveFacill.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DevolveFacill.Workers;

public class TrackingPollerService(
    IServiceScopeFactory scopeFactory,
    ICarrier carrier,
    IBus bus,
    IConfiguration config,
    ILogger<TrackingPollerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = int.TryParse(config["Carrier:Correios:TrackingPollIntervalHours"], out var h) ? h : 2;
        var interval = TimeSpan.FromHours(intervalHours);

        // Give the rest of the application time to start up before first poll.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollDeliveriesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error in tracking poller");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task PollDeliveriesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var shipments = await db.Shipments
            .Include(s => s.ReturnRequest)
            .Where(s => s.DeliveredAt == null
                     && s.ReturnRequest.Status == ReturnStatus.InTransit)
            .AsNoTracking()
            .ToListAsync(ct);

        if (shipments.Count == 0) return;

        logger.LogInformation("Polling tracking for {Count} in-transit shipments", shipments.Count);

        foreach (var shipment in shipments)
        {
            try
            {
                var events = await carrier.GetTrackingHistoryAsync(shipment.TrackingCode, ct);
                var deliveredEvent = events.FirstOrDefault(e => e.IsDelivered);
                if (deliveredEvent is null) continue;

                await bus.Publish(new CarrierDeliveryEvent
                {
                    TrackingCode = shipment.TrackingCode,
                    CarrierCode = shipment.CarrierCode,
                    DeliveredAt = deliveredEvent.OccurredAt
                }, ct);

                logger.LogInformation(
                    "Detected delivery via poll for tracking {Code} — event published",
                    shipment.TrackingCode);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error polling tracking for shipment {ShipmentId} ({Code})",
                    shipment.Id, shipment.TrackingCode);
            }
        }
    }
}
