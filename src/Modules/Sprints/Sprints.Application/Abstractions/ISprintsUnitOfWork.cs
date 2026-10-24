namespace Sprints.Application.Abstractions;

public interface ISprintsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
