using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Application.Tasks.ChangeTaskStatus;

public sealed class ChangeTaskStatusCommandHandler(
    IValidator<ChangeTaskStatusCommand> validator,
    IProjectsModuleApi projects,
    IIdentityModuleApi identity,
    ITaskRepository tasks,
    ITaskManagementUnitOfWork unitOfWork)
    : ICommandHandler<ChangeTaskStatusCommand, Result<TaskDto>>
{
    public async Task<Result<TaskDto>> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
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

        TaskParsing.TryParseStatus(request.Status, out var status);
        var changed = task.ChangeStatus(status);
        if (changed.IsFailure)
            return Result<TaskDto>.ValidationFailure(changed.Error!);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var assigneeName = await TaskDtoMapper.GetDisplayNameAsync(identity, task.AssigneeId, cancellationToken);
        return Result<TaskDto>.Success(TaskDtoMapper.ToDto(task, assigneeName));
    }
}
