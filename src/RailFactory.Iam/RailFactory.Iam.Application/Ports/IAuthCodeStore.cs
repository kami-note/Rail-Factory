namespace RailFactory.Iam.Application.Ports;

public interface IAuthCodeStore
{
    Task StoreAsync(string code, string idToken, TimeSpan ttl, CancellationToken ct = default);
    Task<string?> ConsumeAsync(string code, CancellationToken ct = default);
}

