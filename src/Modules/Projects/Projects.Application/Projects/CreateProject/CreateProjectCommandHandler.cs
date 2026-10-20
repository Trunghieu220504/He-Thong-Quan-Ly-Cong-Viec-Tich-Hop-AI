using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Contracts;
using Projects.Application.Abstractions;
using Projects.Domain.Entities;
using SharedKernel;

namespace Projects.Application.Projects.CreateProject;

public sealed class CreateProjectCommandHandler(
    IValidator<CreateProjectCommand> validator,
    IIdentityModuleApi identity,
    IProjectRepository projects,
    IProjectsUnitOfWork unitOfWork)
    : ICommandHandler<CreateProjectCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<Guid>.ValidationFailure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        if (!await identity.UserExistsAsync(request.OwnerId, cancellationToken))
            return Result<Guid>.Forbidden("Tài khoản không tồn tại.");

        var created = Project.Create(request.Name, request.Description, request.OwnerId);
        if (created.IsFailure)
            return Result<Guid>.ValidationFailure(created.Error!);

        projects.Add(created.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(created.Value!.Id);
    }
}