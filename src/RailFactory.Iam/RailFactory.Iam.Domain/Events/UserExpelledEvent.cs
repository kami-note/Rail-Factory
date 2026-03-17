namespace RailFactory.Iam.Domain.Events;

public sealed class UserExpelledEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public DateTime OccurredAtUtc { get; }

    public UserExpelledEvent(Guid userId, string email, DateTime occurredAtUtc)
    {
        UserId = userId;
        Email = email;
        OccurredAtUtc = occurredAtUtc;
    }
}
