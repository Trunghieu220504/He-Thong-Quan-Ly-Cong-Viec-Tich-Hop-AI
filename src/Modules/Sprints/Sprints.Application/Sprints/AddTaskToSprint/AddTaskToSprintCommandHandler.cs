using Application.Abstractions.CQRS;
using Projects.Contracts;
using SharedKernel;
using Sprints.Application.Abstractions;
using TaskManagement.Contracts;

namespace Sprints.Application.Sprints.AddTaskToSprint;

public sealed class AddTaskToSprintCommandHandler(
    IProjectsModuleApi projects,
    ITaskModuleApi tasks,
    ISprintRepository sprints,
    ISprintsUnitOfWork unitOfWork)
    : ICommandHandler<AddTaskToSprintCommand, Result>
{
    public async Task<Result> Handle(AddTaskToSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await sprints.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            return Result.NotFound("Không tìm thấy sprint.");

        var role = await projects.GetMemberRoleAsync(sprint.ProjectId, request.ActorId, cancellationToken);
        if (!SprintAuthorization.IsOwnerOrAdmin(role))
            return Result.Forbidden("Chỉ Owner hoặc Admin của dự án mới được thêm task vào sprint.");

        var task = await tasks.GetTaskAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result.NotFound("Không tìm thấy task.");

        if (task.ProjectId != sprint.ProjectId)
            return Result.ValidationFailure("Task phải thuộc cùng dự án với sprint.");

        var added = sprint.AddTask(request.TaskId);
        if (added.IsFailure)
            return added;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateSprintTaskException)
        {
            return Result.Conflict("Task đã có trong sprint này.");
        }

        return Result.Success();
    }
}
