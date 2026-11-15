using Microsoft.EntityFrameworkCore;
using TaskManagement.Contracts;
using TaskManagement.Infrastructure.Persistence;
using DomainStatus = TaskManagement.Domain.Enums.ProjectTaskStatus;

namespace TaskManagement.Infrastructure;

internal sealed class TaskModuleApi(TaskManagementDbContext db) : ITaskModuleApi
{
    public async Task<TaskSummary?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        return task is null
            ? null
            : new TaskSummary(task.Id, task.ProjectId, ToContract(task.Status), task.DueDate, task.AssigneeId, task.EstimatedHours, task.ProgressPercent);
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

    public async Task<ProjectTaskStats> GetProjectStatsAsync(Guid projectId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var groups = await db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId)
            .GroupBy(t => new { t.Status, t.AssigneeId })
            .Select(g => new { g.Key.Status, g.Key.AssigneeId, Total = g.Count(),
                Overdue = g.Count(t => t.DueDate < utcNow && t.Status != DomainStatus.Done) })
            .ToListAsync(cancellationToken);
        int Count(DomainStatus status) => groups.Where(g => g.Status == status).Sum(g => g.Total);
        return new(groups.Sum(g => g.Total), Count(DomainStatus.Todo), Count(DomainStatus.InProgress),
            Count(DomainStatus.Done), groups.Sum(g => g.Overdue),
            groups.GroupBy(g => g.AssigneeId).OrderBy(g => g.Key)
                .Select(g => new AssigneeTaskCount(g.Key, g.Sum(x => x.Total),
                    g.Where(x => x.Status == DomainStatus.Done).Sum(x => x.Total))).ToArray());
    }

    public async Task<IReadOnlyList<TaskSummary>> GetProjectTasksAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var tasks = await db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId).OrderBy(t => t.Id).ToListAsync(cancellationToken);
        return tasks.Select(t => new TaskSummary(t.Id, t.ProjectId, ToContract(t.Status), t.DueDate,
            t.AssigneeId, t.EstimatedHours, t.ProgressPercent)).ToArray();
    }

    private static ProjectTaskStatus ToContract(DomainStatus status) => status switch
    {
        DomainStatus.Todo => ProjectTaskStatus.Todo,
        DomainStatus.InProgress => ProjectTaskStatus.InProgress,
        DomainStatus.Done => ProjectTaskStatus.Done,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Trạng thái công việc không được hỗ trợ.")
    };
}
