using Application.Abstractions.CQRS;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Application.Tasks.GetTaskById;

public sealed class GetTaskByIdQueryHandler(
    IProjectsModuleApi projects,
    IIdentityModuleApi identity,
    ITaskRepository tasks)
    : IQueryHandler<GetTaskByIdQuery, Result<TaskDto>>
{
    public async Task<Result<TaskDto>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result<TaskDto>.NotFound("Không tìm thấy công việc.");

        var access = await ProjectAccess.RequireAsync(projects, task.ProjectId, request.UserId, write: false, cancellationToken);
        if (access.IsFailure)
            return ProjectAccess.ToFailure<TaskDto>(access);

        var assigneeName = await TaskDtoMapper.GetDisplayNameAsync(identity, task.AssigneeId, cancellationToken);
        return Result<TaskDto>.Success(TaskDtoMapper.ToDto(task, assigneeName));
    }
}
