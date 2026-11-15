using Microsoft.EntityFrameworkCore;
using TaskManagement.Contracts;
using TaskManagement.Infrastructure.Persistence;
using DomainStatus = TaskManagement.Domain.Enums.ProjectTaskStatus;

namespace TaskManagement.Infrastructure;

internal sealed class TaskModuleApi(TaskManagementDbContext db) : ITaskModuleApi
{
    public async Task<ProjectTaskStats> GetProjectStatsAsync(
        Guid projectId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var grouped = await db.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .GroupBy(t => t.AssigneeId)
            .Select(g => new
            {
                AssigneeId = g.Key,
                Total = g.Count(),
                Todo = g.Count(t => t.Status == DomainStatus.Todo),
                InProgress = g.Count(t => t.Status == DomainStatus.InProgress),
                Done = g.Count(t => t.Status == DomainStatus.Done),
                Overdue = g.Count(t => t.DueDate < utcNow && t.Status != DomainStatus.Done)
            })
            .OrderBy(g => g.AssigneeId)
            .ToListAsync(cancellationToken);

        return new ProjectTaskStats(
            grouped.Sum(g => g.Total), grouped.Sum(g => g.Todo),
            grouped.Sum(g => g.InProgress), grouped.Sum(g => g.Done), grouped.Sum(g => g.Overdue),
            grouped.Select(g => new AssigneeTaskCount(g.AssigneeId, g.Total, g.Done)).ToArray());
    }

    public async Task<TaskSummary?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        return task is null
            ? null
            : new TaskSummary(task.Id, task.ProjectId, ToContract(task.Status), task.DueDate, task.AssigneeId);
    }

    public async Task<TaskStatusCounts> CountByStatusAsync(
        IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0) return new TaskStatusCounts(0, 0, 0, 0);

        var ids = taskIds.Distinct().ToArray();
        var grouped = await db.Tasks.AsNoTracking()
            .Where(t => ids.Contains(t.Id))
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Count(DomainStatus status) => grouped.FirstOrDefault(g => g.Status == status)?.Count ?? 0;

        var todo = Count(DomainStatus.Todo);
        var inProgress = Count(DomainStatus.InProgress);
        var done = Count(DomainStatus.Done);
        return new TaskStatusCounts(todo + inProgress + done, todo, inProgress, done);
    }

    private static ProjectTaskStatus ToContract(DomainStatus status) => status switch
    {
        DomainStatus.Todo => ProjectTaskStatus.Todo,
        DomainStatus.InProgress => ProjectTaskStatus.InProgress,
        DomainStatus.Done => ProjectTaskStatus.Done,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Trạng thái công việc không được hỗ trợ.")
    };
}
