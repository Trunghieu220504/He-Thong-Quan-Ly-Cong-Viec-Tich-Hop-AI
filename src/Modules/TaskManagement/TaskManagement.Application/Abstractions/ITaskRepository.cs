using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Abstractions;

public interface ITaskRepository
{
    Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> GetPageByProjectAsync(
        Guid projectId, ProjectTaskStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    void Add(ProjectTask task);
}
