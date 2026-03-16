using System.Text.Json;
using RailFactory.IAM.Application.Outbox;
using RailFactory.IAM.Domain.Audit;
using RailFactory.IAM.Domain.Outbox;
using RailFactory.IAM.Domain.User;
using RailFactory.IAM.Ports.Audit;
using RailFactory.IAM.Ports.Outbox;
using RailFactory.IAM.Ports.Persistence;
using RailFactory.Shared.Messaging.Events;

namespace RailFactory.IAM.Application.Auth;

/// <summary>
/// Handles login or first-time registration after OAuth2 callback (RF-IA-01, RF-IA-02).
/// Creates user if not exists; returns user and tenant-roles for JWT issuance.
/// User create/update is persisted in one transaction with audit and outbox (UserCreated/UserUpdated).
/// </summary>
public sealed class LoginOrRegisterHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IUserRepository _userRepository;
    private readonly IAuditStore _auditStore;
    private readonly IOutboxStore _outboxStore;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public LoginOrRegisterHandler(
        IUserRepository userRepository,
        IAuditStore auditStore,
        IOutboxStore outboxStore,
        IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditStore = auditStore;
        _outboxStore = outboxStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResult?> HandleAsync(LoginOrRegisterCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByExternalIdAsync(command.ExternalId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            user = await _userRepository.GetByEmailAsync(command.Email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            user = await CreateUserAsync(command, cancellationToken).ConfigureAwait(false);
            if (user is null) return null;
        }
        else if (user.ExternalId != command.ExternalId)
        {
            user.ExternalId = command.ExternalId;
            user.DisplayName = command.Name ?? user.DisplayName;
            user.UpdatedAtUtc = DateTime.UtcNow;

            var occurredAt = DateTime.UtcNow;
            await _auditStore.AppendAsync(new UserAuditEntry
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Action = "Updated",
                PreviousValueJson = null,
                NewValueJson = JsonSerializer.Serialize(new { user.ExternalId, user.DisplayName }),
                OccurredAtUtc = occurredAt
            }, cancellationToken).ConfigureAwait(false);

            var eventId = Guid.NewGuid();
            var userUpdated = new UserUpdated
            {
                EventId = eventId,
                OccurredAtUtc = occurredAt,
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                ExternalId = user.ExternalId
            };
            _outboxStore.Append(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                EventType = UserOutboxEvents.UserUpdated,
                Payload = JsonSerializer.Serialize(userUpdated, JsonOptions),
                Published = false,
                CreatedAtUtc = occurredAt
            });
            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var tenantRoles = await _userRepository.GetTenantRolesAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return new LoginResult { User = user, TenantRoles = tenantRoles };
    }

    private async Task<User?> CreateUserAsync(LoginOrRegisterCommand command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email.Trim(),
            DisplayName = command.Name?.Trim(),
            ExternalId = command.ExternalId,
            CreatedAtUtc = DateTime.UtcNow
        };
        var occurredAt = user.CreatedAtUtc;

        _userRepository.Add(user);
        await _auditStore.AppendAsync(new UserAuditEntry
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Action = "Created",
            NewValueJson = JsonSerializer.Serialize(new { user.Email, user.ExternalId }),
            OccurredAtUtc = occurredAt
        }, cancellationToken).ConfigureAwait(false);

        var eventId = Guid.NewGuid();
        var userCreated = new UserCreated
        {
            EventId = eventId,
            OccurredAtUtc = occurredAt,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            ExternalId = user.ExternalId
        };
        _outboxStore.Append(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = UserOutboxEvents.UserCreated,
            Payload = JsonSerializer.Serialize(userCreated, JsonOptions),
            Published = false,
            CreatedAtUtc = occurredAt
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return user;
    }
}
