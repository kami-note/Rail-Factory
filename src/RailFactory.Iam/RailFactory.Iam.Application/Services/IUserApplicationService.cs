using RailFactory.Iam.Application.DTOs;

namespace RailFactory.Iam.Application.Services;

/// <summary>
/// Application service (use case) for user registration, update, and expulsion.
/// </summary>
public interface IUserApplicationService
{
    /// <summary>
    /// Registers a new user from Google OAuth data or updates existing user. Persists and writes domain events to the outbox.
    /// </summary>
    Task<UserDto> RegisterOrUpdateFromGoogleAsync(string idToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes (expels) the user. Persists and writes domain events to the outbox.
    /// </summary>
    Task ExpelAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the user profile. Persists and writes domain events to the outbox.
    /// </summary>
    Task<UserDto> UpdateProfileAsync(Guid userId, string? displayName, string? pictureUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by id, or null if not found.
    /// </summary>
    Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user from a Google ID token (validates token and returns user by external id), or null if invalid.
    /// </summary>
    Task<UserDto?> GetByGoogleIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
