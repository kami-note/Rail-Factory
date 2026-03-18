using RailFactory.Iam.Application.Ports;
using StackExchange.Redis;

namespace RailFactory.Iam.Infrastructure.Persistence;

public sealed class DistributedCacheAuthCodeStore(IConnectionMultiplexer redis) : IAuthCodeStore
{
    private const string KeyPrefix = "auth_code:";

    public Task StoreAsync(string code, string idToken, TimeSpan ttl, CancellationToken ct = default)
    {
        if (redis is null)
            throw new InvalidOperationException("Redis is required for auth code storage.");

        var key = KeyPrefix + code;
        var db = redis.GetDatabase();
        return db.StringSetAsync(key, idToken, ttl);
    }

    public async Task<string?> ConsumeAsync(string code, CancellationToken ct = default)
    {
        if (redis is null)
            throw new InvalidOperationException("Redis is required for auth code consumption.");

        var key = KeyPrefix + code;
        var db = redis.GetDatabase();
        var value = await db.StringGetDeleteAsync(key).ConfigureAwait(false);
        return value.HasValue ? value.ToString() : null;
    }
}

