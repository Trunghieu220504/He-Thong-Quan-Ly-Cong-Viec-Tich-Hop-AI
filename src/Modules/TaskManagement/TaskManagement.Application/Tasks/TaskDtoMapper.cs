using Identity.Contracts;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Tasks;

internal static class TaskDtoMapper
{
    public static TaskDto ToDto(ProjectTask task, string? assigneeName) => new(
        task.Id,
        task.ProjectId,
        task.Title,
        task.Description,
        task.Status.ToString(),
        task.Priority.ToString(),
        task.AssigneeId,
        assigneeName,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);

    public static async Task<string?> GetDisplayNameAsync(
        IIdentityModuleApi identity, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } id) return null;
        return (await identity.GetUserSummaryAsync(id, cancellationToken))?.DisplayName;
    }
}
