using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using RailFactory.IAM.Application.Outbox;
using RailFactory.IAM.Infrastructure.Persistence;
using RailFactory.Shared.Messaging.Events;

namespace RailFactory.IAM.Worker.Outbox;

/// <summary>
/// Polls the outbox table and publishes UserCreated/UserUpdated to RabbitMQ (doc 03 §10).
/// </summary>
public sealed class OutboxPublisherWorker : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IServiceProvider _serviceProvider;

    public OutboxPublisherWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Log and continue; next poll will retry
                // Logger can be injected if needed
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                continue;
            }

            await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await db.OutboxMessages
            .Where(m => !m.Published)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var msg in pending)
        {
            try
            {
                if (msg.EventType == UserOutboxEvents.UserCreated)
                {
                    var payload = JsonSerializer.Deserialize<UserCreated>(msg.Payload, JsonOptions);
                    if (payload is not null)
                        await publishEndpoint.Publish(payload, cancellationToken).ConfigureAwait(false);
                }
                else if (msg.EventType == UserOutboxEvents.UserUpdated)
                {
                    var payload = JsonSerializer.Deserialize<UserUpdated>(msg.Payload, JsonOptions);
                    if (payload is not null)
                        await publishEndpoint.Publish(payload, cancellationToken).ConfigureAwait(false);
                }

                msg.Published = true;
            }
            catch (Exception)
            {
                // Leave Published = false so we retry next poll
                break;
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
