namespace RailFactory.Iam.Application.Ports;

public interface IOAuthStateStore
{
    Task SaveStateAsync(string state, string nonce, TimeSpan expiration, CancellationToken ct = default);
    Task<string?> GetAndRemoveStateAsync(string state, CancellationToken ct = default);
}
