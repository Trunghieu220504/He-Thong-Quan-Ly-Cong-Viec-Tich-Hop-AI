namespace TaskManagement.Application.Abstractions;

public interface ITaskManagementUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
