namespace RailFactory.Iam.Domain.Entities;

/// <summary>
/// Base type for domain entities with a unique identifier.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity(Guid id)
    {
        Id = id;
    }

    protected Entity()
    {
    }
}
