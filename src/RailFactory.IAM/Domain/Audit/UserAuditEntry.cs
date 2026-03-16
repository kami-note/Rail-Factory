namespace RailFactory.IAM.Domain.Audit;

/// <summary>
/// Audit record for user changes (RF-IA-05): who created/updated, when, previous value.
/// </summary>
public sealed class UserAuditEntry
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; set; }
    public required string Action { get; set; } // e.g. "Created", "Updated", "RoleAssigned"
    public string? PerformedByUserId { get; set; } // null for system or first creation
    public DateTime OccurredAtUtc { get; set; }
    public string? PreviousValueJson { get; set; }
    public string? NewValueJson { get; set; }
}
