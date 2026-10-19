namespace Projects.Application.Abstractions;

public interface IProjectsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class DuplicateMemberException(Exception inner)
    : Exception("Người dùng đã là thành viên của dự án này.", inner);