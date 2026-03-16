using RailFactory.IAM.Domain.Audit;

namespace RailFactory.IAM.Ports.Audit;

/// <summary>
/// Port for user audit (RF-IA-05): who created/updated, when, previous value.
/// </summary>
public interface IAuditStore
{
    Task AppendAsync(UserAuditEntry entry, CancellationToken cancellationToken = default);
}
