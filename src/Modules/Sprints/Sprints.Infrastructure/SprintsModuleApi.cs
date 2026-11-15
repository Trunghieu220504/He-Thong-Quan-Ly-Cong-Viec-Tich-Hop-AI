using Microsoft.EntityFrameworkCore;
using Sprints.Contracts;
using Sprints.Domain.Enums;
using Sprints.Infrastructure.Persistence;
using TaskManagement.Contracts;
namespace Sprints.Infrastructure;
internal sealed class SprintsModuleApi(SprintsDbContext db, ITaskModuleApi tasks) : ISprintsModuleApi
{
    public async Task<ActiveSprintOverview?> GetActiveSprintOverviewAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var sprint = await db.Sprints.AsNoTracking().Include(s => s.Tasks)
            .SingleOrDefaultAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active, cancellationToken);
        if (sprint is null) return null;
        var counts = await tasks.CountByStatusAsync(sprint.Tasks.Select(t => t.TaskId).ToArray(), cancellationToken);
        return new(sprint.Id, sprint.Name, sprint.StartDate, sprint.EndDate, counts.Total, counts.Done, counts.Total - counts.Done);
    }
}
