namespace Sprints.Contracts;

public sealed record ActiveSprintOverview(
    Guid Id,
    string Name,
    DateTime? StartDate,
    DateTime? EndDate,
    int TotalTasks,
    int CompletedTasks,
    int RemainingTasks);

public interface ISprintsModuleApi
{
    Task<ActiveSprintOverview?> GetActiveSprintOverviewAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
