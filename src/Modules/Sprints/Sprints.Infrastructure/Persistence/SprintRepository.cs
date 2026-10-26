using Microsoft.EntityFrameworkCore;
using Sprints.Application.Abstractions;
using Sprints.Domain.Entities;
using Sprints.Domain.Enums;

namespace Sprints.Infrastructure.Persistence;

internal sealed class SprintRepository(SprintsDbContext db) : ISprintRepository
{
    public Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Sprints.Include(sprint => sprint.Tasks)
            .FirstOrDefaultAsync(sprint => sprint.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sprint>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default) =>
        await db.Sprints.AsNoTracking()
            .Where(sprint => sprint.ProjectId == projectId)
            .Include(sprint => sprint.Tasks)
            .OrderByDescending(sprint => sprint.CreatedAt)
            .ThenBy(sprint => sprint.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveSprintAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        db.Sprints.AnyAsync(
            sprint => sprint.ProjectId == projectId && sprint.Status == SprintStatus.Active,
            cancellationToken);

    public void Add(Sprint sprint) => db.Sprints.Add(sprint);
}
