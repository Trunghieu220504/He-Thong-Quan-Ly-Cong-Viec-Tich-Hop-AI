using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;
using TaskManagement.Contracts.Events;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Tasks.CreateTask;

public sealed class CreateTaskCommandHandler(
    IValidator<CreateTaskCommand> validator,
    IProjectsModuleApi projects,
    IIdentityModuleApi identity,
    ITaskRepository tasks,
    ITaskManagementUnitOfWork unitOfWork,
    IIntegrationEventOutbox outbox)
    : ICommandHandler<CreateTaskCommand, Result<TaskDto>>
{
    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<TaskDto>.ValidationFailure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var access = await ProjectAccess.RequireAsync(projects, request.ProjectId, request.ActorId, write: true, cancellationToken);
        if (access.IsFailure)
            return ProjectAccess.ToFailure<TaskDto>(access);

        if (request.AssigneeId is { } assigneeId)
        {
            var assignee = await ProjectAccess.RequireAssigneeIsMemberAsync(projects, request.ProjectId, assigneeId, cancellationToken);
            if (assignee.IsFailure)
                return ProjectAccess.ToFailure<TaskDto>(assignee);
        }

        TaskParsing.TryParsePriority(request.Priority, out var priority);
        var created = ProjectTask.Create(
            request.ProjectId, request.Title, request.Description, priority,
            request.AssigneeId, request.DueDate, request.ActorId, request.EstimatedHours, request.ProgressPercent);
        if (created.IsFailure)
            return Result<TaskDto>.ValidationFailure(created.Error!);

        var task = created.Value!;
        tasks.Add(task);

        // Ghi sự kiện vào outbox TRƯỚC khi lưu để nó nằm chung transaction với task; gửi tức thì sau khi lưu xong.
        if (task.AssigneeId is not null)
        {
            outbox.Enqueue(new TaskAssignedIntegrationEvent(
                Guid.NewGuid(), DateTime.UtcNow, task.Id, task.ProjectId, task.Title,
                task.AssigneeId, null, request.ActorId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await outbox.DispatchAsync(cancellationToken);

        var assigneeName = await TaskDtoMapper.GetDisplayNameAsync(identity, task.AssigneeId, cancellationToken);
        return Result<TaskDto>.Success(TaskDtoMapper.ToDto(task, assigneeName));
    }
}
