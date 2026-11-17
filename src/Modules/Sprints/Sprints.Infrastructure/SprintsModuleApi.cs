using Microsoft.EntityFrameworkCore;
using Sprints.Contracts;
using Sprints.Domain.Enums;
using Sprints.Infrastructure.Persistence;
using TaskManagement.Contracts;

namespace Sprints.Infrastructure;

public sealed class SprintsModuleApi(
    SprintsDbContext dbContext,
    ITaskModuleApi taskModuleApi) : ISprintsModuleApi
{
    public async Task<ActiveSprintOverview?> GetActiveSprintOverviewAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var activeSprint = await dbContext.Sprints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.ProjectId == projectId && s.Status == SprintStatus.Active,
                cancellationToken);

        if (activeSprint is null)
            return null;

        var taskIds = await dbContext.SprintTasks
            .AsNoTracking()
            .Where(st => st.SprintId == activeSprint.Id)
            .Select(st => st.TaskId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (taskIds.Count == 0)
        {
            return new ActiveSprintOverview(
                activeSprint.Id,
                activeSprint.Name,
                activeSprint.StartDate,
                activeSprint.EndDate,
                TotalTasks: 0,
                CompletedTasks: 0,
                RemainingTasks: 0);
        }

        var statusCounts = await taskModuleApi.CountByStatusAsync(taskIds, cancellationToken);

        var totalTasks = statusCounts.Total;
        var completedTasks = statusCounts.Done;
        var remainingTasks = totalTasks - completedTasks;

        return new ActiveSprintOverview(
            activeSprint.Id,
            activeSprint.Name,
            activeSprint.StartDate,
            activeSprint.EndDate,
            totalTasks,
            completedTasks,
            remainingTasks);
    }
}
