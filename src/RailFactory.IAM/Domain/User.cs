namespace RailFactory.IAM.Domain;

/// <summary>
/// User identity. Authentication via OAuth2 Google (RF-IA-01, RF-IA-02); tenant and role association in UserTenantRole.
/// </summary>
public sealed class User
{
    public required Guid Id { get; init; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>Google (or other IdP) subject/oid claim; used to link OAuth identity to this user.</summary>
    public string? ExternalId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
