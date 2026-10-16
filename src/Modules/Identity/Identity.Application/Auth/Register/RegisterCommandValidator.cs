using FluentValidation;

namespace Identity.Application.Auth.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        // BCrypt chỉ dùng tối đa 72 byte đầu của mật khẩu.
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(72);
        RuleFor(x => x.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(100);
    }
}
