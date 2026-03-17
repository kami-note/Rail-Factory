using RailFactory.Iam.Domain.Events;

namespace RailFactory.Iam.Domain.Entities;

/// <summary>
/// User aggregate root. Identity is tied to an external provider (e.g. Google).
/// Supports creation, update, and soft delete (expulsion).
/// </summary>
public class User : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public string Email { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? PictureUrl { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public UserStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? ExpelledAtUtc { get; private set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private User()
        : base()
    {
    }

    private User(
        Guid id,
        string email,
        string externalId,
        string? displayName,
        string? pictureUrl,
        DateTime createdAtUtc)
        : base(id)
    {
        Email = email;
        ExternalId = externalId;
        DisplayName = displayName;
        PictureUrl = pictureUrl;
        Status = UserStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Creates a new active user and raises <see cref="UserCreatedEvent"/>.
    /// </summary>
    public static User Create(
        Guid id,
        string email,
        string externalId,
        string? displayName,
        string? pictureUrl,
        DateTime createdAtUtc)
    {
        var user = new User(id, email, externalId, displayName, pictureUrl, createdAtUtc);
        user.Raise(new UserCreatedEvent(
            user.Id,
            user.Email,
            user.ExternalId,
            user.DisplayName,
            user.PictureUrl,
            user.CreatedAtUtc));
        return user;
    }

    /// <summary>
    /// Updates profile and raises <see cref="UserUpdatedEvent"/>.
    /// </summary>
    public void Update(string? displayName, string? pictureUrl, DateTime updatedAtUtc)
    {
        if (Status == UserStatus.Expelled)
            throw new InvalidOperationException("Cannot update an expelled user.");

        DisplayName = displayName;
        PictureUrl = pictureUrl;
        UpdatedAtUtc = updatedAtUtc;
        Raise(new UserUpdatedEvent(Id, Email, displayName, pictureUrl, updatedAtUtc));
    }

    /// <summary>
    /// Soft-deletes the user (expulsion) and raises <see cref="UserExpelledEvent"/>.
    /// </summary>
    public void Expel(DateTime expelledAtUtc)
    {
        if (Status == UserStatus.Expelled)
            throw new InvalidOperationException("User is already expelled.");

        Status = UserStatus.Expelled;
        ExpelledAtUtc = expelledAtUtc;
        Raise(new UserExpelledEvent(Id, Email, expelledAtUtc));
    }

    private void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears domain events after they have been persisted/published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
