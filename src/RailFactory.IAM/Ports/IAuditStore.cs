using RailFactory.IAM.Domain;

namespace RailFactory.IAM.Ports;

/// <summary>
/// Port for user audit (RF-IA-05): who created/updated, when, previous value.
/// </summary>
public interface IAuditStore
{
    Task AppendAsync(UserAuditEntry entry, CancellationToken cancellationToken = default);
}
