using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

internal sealed class TaskRepository(TaskManagementDbContext db) : ITaskRepository
{
    public Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> GetPageByProjectAsync(
        Guid projectId, ProjectTaskStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Tasks.AsNoTracking().Where(t => t.ProjectId == projectId);
        if (status is { } s)
            query = query.Where(t => t.Status == s);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(ProjectTask task) => db.Tasks.Add(task);
}
