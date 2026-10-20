using FluentValidation;
using Projects.Domain.Enums;

namespace Projects.Application.Projects.AddMember;

public sealed class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role)
            .Must(r => TryParseAssignableRole(r, out _))
            .WithMessage("Vai trò không hợp lệ. Chỉ chấp nhận: Admin, Member, Viewer.");
    }

    public static bool TryParseAssignableRole(string? value, out ProjectRole role)
    {
        role = default;
        if (string.IsNullOrWhiteSpace(value) || char.IsDigit(value.Trim()[0])) return false;
        return Enum.TryParse(value.Trim(), ignoreCase: true, out role)
            && Enum.IsDefined(role)
            && role != ProjectRole.Owner;
    }
}