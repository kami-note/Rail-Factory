namespace RailFactory.Iam.Domain.Events;

public sealed class UserCreatedEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string ExternalId { get; }
    public string? DisplayName { get; }
    public string? PictureUrl { get; }
    public DateTime OccurredAtUtc { get; }

    public UserCreatedEvent(
        Guid userId,
        string email,
        string externalId,
        string? displayName,
        string? pictureUrl,
        DateTime occurredAtUtc)
    {
        UserId = userId;
        Email = email;
        ExternalId = externalId;
        DisplayName = displayName;
        PictureUrl = pictureUrl;
        OccurredAtUtc = occurredAtUtc;
    }
}
