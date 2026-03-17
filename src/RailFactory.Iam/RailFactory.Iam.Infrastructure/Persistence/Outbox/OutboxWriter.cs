using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Application.Ports;
using RailFactory.Iam.Infrastructure.Persistence;

namespace RailFactory.Iam.Infrastructure.Persistence.Outbox;

/// <summary>
/// Writes outbox messages in the same DbContext as domain entities (same transaction).
/// </summary>
public sealed class OutboxWriter : IOutboxWriter
{
    private readonly IamDbContext _dbContext;

    public OutboxWriter(IamDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync(string messageType, string payload, CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = messageType,
            Payload = payload,
            CreatedAtUtc = DateTime.UtcNow,
            Processed = false
        };
        await _dbContext.OutboxMessages.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
