using TaskManagement.Contracts;

namespace Sprints.Infrastructure;

internal sealed class UnavailableTaskModuleApi : ITaskModuleApi
{
    public Task<ProjectTaskStats> GetProjectStatsAsync(
        Guid projectId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProjectTaskStats(0, 0, 0, 0, 0, Array.Empty<AssigneeTaskCount>()));

    public Task<TaskSummary?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TaskSummary?>(null);

    public Task<TaskStatusCounts> CountByStatusAsync(
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TaskStatusCounts(0, 0, 0, 0));
}
