using Projects.Domain.Entities;

namespace Projects.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPageByMemberAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    void Add(Project project);
}