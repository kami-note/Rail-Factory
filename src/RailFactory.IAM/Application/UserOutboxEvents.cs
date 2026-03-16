namespace RailFactory.IAM.Application;

/// <summary>
/// Event type names for IAM outbox (must match worker and consumers).
/// </summary>
public static class UserOutboxEvents
{
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
}
