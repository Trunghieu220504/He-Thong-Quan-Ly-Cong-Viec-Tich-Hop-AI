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
