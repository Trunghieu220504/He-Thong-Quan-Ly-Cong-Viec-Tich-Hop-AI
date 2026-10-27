using Application.Abstractions.CQRS;
using Projects.Contracts;
using SharedKernel;
using Sprints.Application.Abstractions;
using TaskManagement.Contracts;

namespace Sprints.Application.Sprints.GetSprintBurndown;

public sealed class GetSprintBurndownQueryHandler(
    IProjectsModuleApi projects,
    ITaskModuleApi tasks,
    ISprintRepository sprints)
    : IQueryHandler<GetSprintBurndownQuery, Result<SprintBurndownDto>>
{
    public async Task<Result<SprintBurndownDto>> Handle(
        GetSprintBurndownQuery request,
        CancellationToken cancellationToken)
    {
        var sprint = await sprints.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            return Result<SprintBurndownDto>.NotFound("Không tìm thấy sprint.");

        var role = await projects.GetMemberRoleAsync(sprint.ProjectId, request.ActorId, cancellationToken);
        if (role is null)
            return Result<SprintBurndownDto>.Forbidden("Bạn không phải thành viên của dự án này.");

        var taskIds = sprint.Tasks.Select(task => task.TaskId).ToArray();
        var completedTasks = (await tasks.CountByStatusAsync(taskIds, cancellationToken)).Done;
        var totalTasks = taskIds.Length;

        return Result<SprintBurndownDto>.Success(new SprintBurndownDto(
            sprint.Id,
            totalTasks,
            completedTasks,
            totalTasks - completedTasks));
    }
}

