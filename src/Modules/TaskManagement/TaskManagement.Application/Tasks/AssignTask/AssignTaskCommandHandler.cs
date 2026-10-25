using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Application.Tasks.AssignTask;

public sealed class AssignTaskCommandHandler(
    IValidator<AssignTaskCommand> validator,
    IProjectsModuleApi projects,
    IIdentityModuleApi identity,
    ITaskRepository tasks,
    ITaskManagementUnitOfWork unitOfWork)
    : ICommandHandler<AssignTaskCommand, Result<TaskDto>>
{
    public async Task<Result<TaskDto>> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<TaskDto>.ValidationFailure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var task = await tasks.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result<TaskDto>.NotFound("Không tìm thấy công việc.");

        var access = await ProjectAccess.RequireAsync(projects, task.ProjectId, request.ActorId, write: true, cancellationToken);
        if (access.IsFailure)
            return ProjectAccess.ToFailure<TaskDto>(access);

        if (request.AssigneeId is { } assigneeId)
        {
            var assignee = await ProjectAccess.RequireAssigneeIsMemberAsync(projects, task.ProjectId, assigneeId, cancellationToken);
            if (assignee.IsFailure)
                return ProjectAccess.ToFailure<TaskDto>(assignee);
        }

        var assigned = task.Assign(request.AssigneeId);
        if (assigned.IsFailure)
            return Result<TaskDto>.ValidationFailure(assigned.Error!);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var assigneeName = await TaskDtoMapper.GetDisplayNameAsync(identity, task.AssigneeId, cancellationToken);
        return Result<TaskDto>.Success(TaskDtoMapper.ToDto(task, assigneeName));
    }
}
