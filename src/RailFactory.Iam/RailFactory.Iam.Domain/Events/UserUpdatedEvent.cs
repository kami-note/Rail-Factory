namespace RailFactory.Iam.Domain.Events;

public sealed class UserUpdatedEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string? DisplayName { get; }
    public string? PictureUrl { get; }
    public DateTime OccurredAtUtc { get; }

    public UserUpdatedEvent(
        Guid userId,
        string email,
        string? displayName,
        string? pictureUrl,
        DateTime occurredAtUtc)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        PictureUrl = pictureUrl;
        OccurredAtUtc = occurredAtUtc;
    }
}
