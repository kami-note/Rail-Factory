using RailFactory.Iam.Application.Ports;
using StackExchange.Redis;

namespace RailFactory.Iam.Infrastructure.Persistence;

/// <summary>
/// Redis-backed OAuth state store. Uses GETDEL for atomic get-and-remove to prevent race conditions when validating state.
/// </summary>
public sealed class DistributedCacheOAuthStateStore : IOAuthStateStore
{
    private readonly IConnectionMultiplexer _redis;
    private const string KeyPrefix = "oauth_state:";

    public DistributedCacheOAuthStateStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task SaveStateAsync(string state, string nonce, TimeSpan expiration, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + state;
        await db.StringSetAsync(key, nonce, expiration).ConfigureAwait(false);
    }

    /// <summary>
    /// Atomically retrieves and deletes the state value using Redis GETDEL, so only one caller can consume a given state.
    /// </summary>
    public async Task<string?> GetAndRemoveStateAsync(string state, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = KeyPrefix + state;
        var value = await db.StringGetDeleteAsync(key).ConfigureAwait(false);
        return value.IsNullOrEmpty ? null : value.ToString();
    }
}
