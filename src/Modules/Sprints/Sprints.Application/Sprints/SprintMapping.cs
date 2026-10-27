using Sprints.Domain.Entities;

namespace Sprints.Application.Sprints;

internal static class SprintMapping
{
    public static SprintDto ToDto(this Sprint sprint) => new(
        sprint.Id,
        sprint.ProjectId,
        sprint.Name,
        sprint.Goal,
        sprint.StartDate,
        sprint.EndDate,
        sprint.Status,
        sprint.CreatedAt,
        sprint.UpdatedAt,
        sprint.Tasks.Select(task => new SprintTaskDto(task.TaskId, task.AddedAt)).ToList());
}
