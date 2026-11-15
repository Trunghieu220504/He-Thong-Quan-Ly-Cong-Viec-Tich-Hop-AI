using Application.Abstractions.CQRS;
using EventBus;
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
    IIntegrationEventBus eventBus)
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (previousStatus != task.Status)
        {
            await eventBus.PublishAsync(new TaskStatusChangedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, task.Id, task.ProjectId, task.Title,
                ToContract(previousStatus), ToContract(task.Status), task.AssigneeId,
                task.CreatedBy, request.ActorId), cancellationToken);
        }

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
