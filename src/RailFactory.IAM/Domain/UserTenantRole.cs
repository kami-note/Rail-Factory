namespace RailFactory.IAM.Domain;

/// <summary>
/// Associates a user with a tenant and role (RF-IA-03, RF-IA-06). One user can have multiple tenant-role pairs.
/// </summary>
public sealed class UserTenantRole
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; set; }
    public required string TenantId { get; set; }
    public required Role Role { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
