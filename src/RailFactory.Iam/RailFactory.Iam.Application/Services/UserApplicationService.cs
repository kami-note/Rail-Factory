using System.Text.Json;
using Microsoft.Extensions.Logging;
using RailFactory.Iam.Application.DTOs;
using RailFactory.Iam.Application.Ports;
using RailFactory.Iam.Domain.Entities;
using RailFactory.Iam.Domain.Events;
using RailFactory.Iam.Domain.Exceptions;

namespace RailFactory.Iam.Application.Services;

/// <summary>
/// Orchestrates user use cases: register/update from Google, expel, update profile.
/// Writes domain events to the outbox in the same transaction (SOLID: SRP, DIP).
/// </summary>
public sealed class UserApplicationService : IUserApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IGoogleAuthProvider _googleAuthProvider;
    private readonly ILogger<UserApplicationService> _logger;

    public UserApplicationService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOutboxWriter outboxWriter,
        IGoogleAuthProvider googleAuthProvider,
        ILogger<UserApplicationService> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _outboxWriter = outboxWriter;
        _googleAuthProvider = googleAuthProvider;
        _logger = logger;
    }

    public async Task<UserDto> RegisterOrUpdateFromGoogleAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleAuthProvider.GetUserInfoAsync(idToken, cancellationToken).ConfigureAwait(false)
            ?? throw new UnauthorizedAccessException("Invalid or expired Google ID token.");

        var existing = await _userRepository.GetByExternalIdAsync(googleUser.Sub, cancellationToken).ConfigureAwait(false);
        var now = DateTime.UtcNow;

        if (existing != null)
        {
            if (existing.Status == UserStatus.Expelled)
                throw new UserExpelledException();
            existing.Update(googleUser.Name, googleUser.Picture, now);
            await WriteDomainEventsToOutboxAsync(existing, cancellationToken).ConfigureAwait(false);
            existing.ClearDomainEvents();
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated user {UserId} from Google", existing.Id);
            return MapToDto(existing);
        }

        var user = User.Create(
            Guid.NewGuid(),
            googleUser.Email,
            googleUser.Sub,
            googleUser.Name,
            googleUser.Picture,
            now);
        _userRepository.Add(user);
        await WriteDomainEventsToOutboxAsync(user, cancellationToken).ConfigureAwait(false);
        user.ClearDomainEvents();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Registered user {UserId} from Google", user.Id);
        return MapToDto(user);
    }

    public async Task ExpelAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new UserNotFoundException();
        user.Expel(DateTime.UtcNow);
        await WriteDomainEventsToOutboxAsync(user, cancellationToken).ConfigureAwait(false);
        user.ClearDomainEvents();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Expelled user {UserId}", userId);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, string? displayName, string? pictureUrl, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new UserNotFoundException();
        user.Update(displayName, pictureUrl, DateTime.UtcNow);
        await WriteDomainEventsToOutboxAsync(user, cancellationToken).ConfigureAwait(false);
        user.ClearDomainEvents();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapToDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByGoogleIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var googleUser = await _googleAuthProvider.GetUserInfoAsync(idToken, cancellationToken).ConfigureAwait(false);
        if (googleUser == null) return null;
        var user = await _userRepository.GetByExternalIdAsync(googleUser.Sub, cancellationToken).ConfigureAwait(false);
        return user == null || user.Status == UserStatus.Expelled ? null : MapToDto(user);
    }

    private async Task WriteDomainEventsToOutboxAsync(User user, CancellationToken cancellationToken)
    {
        foreach (var evt in user.DomainEvents)
        {
            var typeName = evt.GetType().Name;
            var payload = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions);
            await _outboxWriter.EnqueueAsync(typeName, payload, cancellationToken).ConfigureAwait(false);
        }
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        PictureUrl = user.PictureUrl,
        Status = user.Status,
        CreatedAtUtc = user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc,
        ExpelledAtUtc = user.ExpelledAtUtc
    };
}
