using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Infrastructure.Persistence;
using StackExchange.Redis;

namespace RailFactory.Iam.Api;

/// <summary>
/// Runs database migrations at startup under a Redis distributed lock so only one instance performs migrations.
/// Does not block application startup or health/readiness probes.
/// </summary>
public sealed class MigrationHostedService(
    IConnectionMultiplexer redis,
    IServiceProvider services,
    ILogger<MigrationHostedService> logger) : IHostedService
{
    private const string LockKey = "iam:migration:lock";
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(2);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = RunMigrationsWithLockAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunMigrationsWithLockAsync(CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var lockValue = Guid.NewGuid().ToString("N");

        try
        {
            // Try to acquire lock (NX = only if not exists); use a unique value so we only release our own lock
            var acquired = await db.StringSetAsync(LockKey, lockValue, LockExpiry, When.NotExists).ConfigureAwait(false);
            if (!acquired)
            {
                logger.LogInformation("Migration lock held by another instance; skipping migrations.");
                return;
            }

            await ApplyMigrationsAsync(services, logger, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // No need to log
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migrations failed.");
        }
        finally
        {
            try
            {
                // Release only our lock (compare value and delete)
                var script = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
                await db.ScriptEvaluateAsync(script, new RedisKey[] { LockKey }, new RedisValue[] { lockValue }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to release migration lock.");
            }
        }
    }

    /// <summary>
    /// Applies EF Core migrations with bounded retry and exponential backoff for transient failures only.
    /// Non-transient errors are surfaced immediately (fail fast).
    /// </summary>
    internal static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();

        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Migrations applied successfully.");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                if (!IsTransientException(ex))
                {
                    logger.LogError(ex, "Non-transient error during migration; failing fast.");
                    throw;
                }

                var delayMs = (int)Math.Min(1000 * Math.Pow(2, attempt - 1), 30_000);
                logger.LogWarning(ex, "Migration attempt {Attempt}/{Max} failed (transient), retrying in {DelayMs}ms...", attempt, maxRetries, delayMs);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is Npgsql.NpgsqlException npg && npg.IsTransient
               || ex is DbUpdateException { InnerException: Npgsql.NpgsqlException npg2 } && npg2.IsTransient
               || ex is TimeoutException
               || ex is IOException
               || ex is SocketException;
    }
}
