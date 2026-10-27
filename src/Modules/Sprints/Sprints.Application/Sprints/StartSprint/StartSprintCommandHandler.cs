using Application.Abstractions.CQRS;
using Projects.Contracts;
using SharedKernel;
using Sprints.Application.Abstractions;

namespace Sprints.Application.Sprints.StartSprint;

public sealed class StartSprintCommandHandler(
    IProjectsModuleApi projects,
    ISprintRepository sprints,
    ISprintsUnitOfWork unitOfWork)
    : ICommandHandler<StartSprintCommand, Result>
{
    public async Task<Result> Handle(StartSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await sprints.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            return Result.NotFound("Không tìm thấy sprint.");

        var role = await projects.GetMemberRoleAsync(sprint.ProjectId, request.ActorId, cancellationToken);
        if (!SprintAuthorization.IsOwnerOrAdmin(role))
            return Result.Forbidden("Chỉ Owner hoặc Admin của dự án mới được bắt đầu sprint.");

        if (await sprints.HasActiveSprintAsync(sprint.ProjectId, cancellationToken))
            return Result.Conflict("Dự án đã có sprint đang hoạt động.");

        var started = sprint.Start();
        if (started.IsFailure)
            return started;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateActiveSprintException)
        {
            return Result.Conflict("Dự án đã có sprint đang hoạt động.");
        }

        return Result.Success();
    }
}
