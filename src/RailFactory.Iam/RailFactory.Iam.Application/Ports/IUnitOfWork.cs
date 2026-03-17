namespace RailFactory.Iam.Application.Ports;

/// <summary>
/// Port for committing a unit of work (transaction boundary).
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
