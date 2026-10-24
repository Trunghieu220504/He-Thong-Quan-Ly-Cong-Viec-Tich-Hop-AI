using Application.Abstractions.CQRS;
using Projects.Contracts;
using SharedKernel;
using Sprints.Application.Abstractions;

namespace Sprints.Application.Sprints.CompleteSprint;

public sealed class CompleteSprintCommandHandler(
    IProjectsModuleApi projects,
    ISprintRepository sprints,
    ISprintsUnitOfWork unitOfWork)
    : ICommandHandler<CompleteSprintCommand, Result>
{
    public async Task<Result> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await sprints.GetByIdAsync(request.SprintId, cancellationToken);
        if (sprint is null)
            return Result.NotFound("Không tìm thấy sprint.");

        var role = await projects.GetMemberRoleAsync(sprint.ProjectId, request.ActorId, cancellationToken);
        if (!SprintAuthorization.IsOwnerOrAdmin(role))
            return Result.Forbidden("Chỉ Owner hoặc Admin của dự án mới được hoàn tất sprint.");

        var completed = sprint.Complete();
        if (completed.IsFailure)
            return completed;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
