using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Contracts;
using Projects.Application.Abstractions;
using Projects.Domain.Enums;
using SharedKernel;

namespace Projects.Application.Projects.AddMember;

public sealed class AddMemberCommandHandler(
    IValidator<AddMemberCommand> validator,
    IIdentityModuleApi identity,
    IProjectRepository projects,
    IProjectsUnitOfWork unitOfWork)
    : ICommandHandler<AddMemberCommand, Result>
{
    public async Task<Result> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.ValidationFailure(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var project = await projects.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Result.NotFound("Không tìm thấy dự án.");

        var actor = project.Members.FirstOrDefault(m => m.UserId == request.ActorId);
        if (actor is null || actor.Role is not (ProjectRole.Owner or ProjectRole.Admin))
            return Result.Forbidden("Chỉ Owner hoặc Admin của dự án mới được thêm thành viên.");

        if (!await identity.UserExistsAsync(request.UserId, cancellationToken))
            return Result.NotFound("Người dùng cần thêm không tồn tại.");

        if (project.Members.Any(m => m.UserId == request.UserId))
            return Result.Conflict("Người dùng đã là thành viên của dự án này.");

        AddMemberCommandValidator.TryParseAssignableRole(request.Role, out var role);
        var added = project.AddMember(request.UserId, role);
        if (added.IsFailure)
            return Result.ValidationFailure(added.Error!);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateMemberException)
        {
            return Result.Conflict("Người dùng đã là thành viên của dự án này.");
        }

        return Result.Success();
    }
}