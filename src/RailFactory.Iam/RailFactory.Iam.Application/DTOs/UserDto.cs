using RailFactory.Iam.Domain.Entities;

namespace RailFactory.Iam.Application.DTOs;

/// <summary>
/// DTO for user representation in API responses.
/// </summary>
public sealed class UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? PictureUrl { get; init; }
    public UserStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? ExpelledAtUtc { get; init; }
}
