using Microsoft.Extensions.Caching.Distributed;
using RailFactory.Iam.Application.Ports;
using StackExchange.Redis;

namespace RailFactory.Iam.Infrastructure.Persistence;

public sealed class DistributedCacheAuthCodeStore(IDistributedCache cache, IConnectionMultiplexer? redis = null) : IAuthCodeStore
{
    private const string KeyPrefix = "auth_code:";

    public Task StoreAsync(string code, string idToken, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = KeyPrefix + code;
        return cache.SetStringAsync(key, idToken, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        }, ct);
    }

    public async Task<string?> ConsumeAsync(string code, CancellationToken ct = default)
    {
        var key = KeyPrefix + code;
        if (redis is not null)
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetDeleteAsync(key).ConfigureAwait(false);
            return value.HasValue ? value.ToString() : null;
        }

        var token = await cache.GetStringAsync(key, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(token))
            return null;
        await cache.RemoveAsync(key, ct).ConfigureAwait(false);
        return token;
    }
}

