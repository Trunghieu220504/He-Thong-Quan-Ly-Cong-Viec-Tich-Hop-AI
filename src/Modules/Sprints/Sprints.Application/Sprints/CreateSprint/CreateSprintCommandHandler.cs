using Application.Abstractions.CQRS;
using FluentValidation;
using Projects.Contracts;
using SharedKernel;
using Sprints.Application.Abstractions;
using Sprints.Domain.Entities;

namespace Sprints.Application.Sprints.CreateSprint;

public sealed class CreateSprintCommandHandler(
    IValidator<CreateSprintCommand> validator,
    IProjectsModuleApi projects,
    ISprintRepository sprints,
    ISprintsUnitOfWork unitOfWork)
    : ICommandHandler<CreateSprintCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<Guid>.ValidationFailure(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage)));

        if (!await projects.ProjectExistsAsync(request.ProjectId, cancellationToken))
            return Result<Guid>.NotFound("Không tìm thấy dự án.");

        var role = await projects.GetMemberRoleAsync(request.ProjectId, request.ActorId, cancellationToken);
        if (!SprintAuthorization.IsOwnerOrAdmin(role))
            return Result<Guid>.Forbidden("Chỉ Owner hoặc Admin của dự án mới được tạo sprint.");

        var created = Sprint.Create(request.ProjectId, request.Name, request.Goal, request.StartDate, request.EndDate);
        if (created.IsFailure)
            return Result<Guid>.ValidationFailure(created.Error!);

        sprints.Add(created.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(created.Value!.Id);
    }
}
