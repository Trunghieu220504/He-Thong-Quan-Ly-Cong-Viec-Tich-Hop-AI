using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;
using TaskManagement.Contracts.Events;
using ContractStatus = TaskManagement.Contracts.ProjectTaskStatus;
using DomainStatus = TaskManagement.Domain.Enums.ProjectTaskStatus;

namespace TaskManagement.Application.Tasks.ChangeTaskStatus;

public sealed class ChangeTaskStatusCommandHandler(
    IValidator<ChangeTaskStatusCommand> validator,
    IProjectsModuleApi projects,
    IIdentityModuleApi identity,
    ITaskRepository tasks,
    ITaskManagementUnitOfWork unitOfWork,
    IIntegrationEventOutbox outbox)
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
        var previousStatus = task.Status;
        var changed = task.ChangeStatus(status);
        if (changed.IsFailure)
            return Result<TaskDto>.ValidationFailure(changed.Error!);

        // Ghi sự kiện vào outbox TRƯỚC khi lưu để nó nằm chung transaction với task; gửi tức thì sau khi lưu xong.
        if (previousStatus != task.Status)
        {
            outbox.Enqueue(new TaskStatusChangedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, task.Id, task.ProjectId, task.Title,
                ToContract(previousStatus), ToContract(task.Status), task.AssigneeId,
                task.CreatedBy, request.ActorId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await outbox.DispatchAsync(cancellationToken);

        var assigneeName = await TaskDtoMapper.GetDisplayNameAsync(identity, task.AssigneeId, cancellationToken);
        return Result<TaskDto>.Success(TaskDtoMapper.ToDto(task, assigneeName));
    }

    private static ContractStatus ToContract(DomainStatus status) => status switch
    {
        DomainStatus.Todo => ContractStatus.Todo,
        DomainStatus.InProgress => ContractStatus.InProgress,
        DomainStatus.Done => ContractStatus.Done,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported task status.")
    };
}
