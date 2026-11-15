using Application.Abstractions.CQRS;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application.Abstractions;
namespace TaskManagement.Application.Tasks.UpdateTask;
public sealed class UpdateTaskCommandHandler(ITaskRepository tasks, ITaskManagementUnitOfWork unitOfWork,
    IProjectsModuleApi projects, IIdentityModuleApi identity) : ICommandHandler<UpdateTaskCommand, Result<TaskDto>>
{
    public async Task<Result<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        var task = await tasks.GetByIdAsync(request.TaskId, ct);
        if (task is null) return Result<TaskDto>.NotFound("Không tìm thấy task.");
        var access = await ProjectAccess.RequireAsync(projects, task.ProjectId, request.ActorId, true, ct);
        if (access.IsFailure) return ProjectAccess.ToFailure<TaskDto>(access);
        if (!TaskParsing.TryParsePriority(request.Priority, out var priority))
            return Result<TaskDto>.ValidationFailure("Mức ưu tiên không hợp lệ.");
        var updated = task.Update(request.Title, request.Description, priority, request.DueDate, request.EstimatedHours, request.ProgressPercent);
        if (updated.IsFailure) return Result<TaskDto>.ValidationFailure(updated.Error!);
        await unitOfWork.SaveChangesAsync(ct);
        return Result<TaskDto>.Success(TaskDtoMapper.ToDto(task, await TaskDtoMapper.GetDisplayNameAsync(identity, task.AssigneeId, ct)));
    }
}
