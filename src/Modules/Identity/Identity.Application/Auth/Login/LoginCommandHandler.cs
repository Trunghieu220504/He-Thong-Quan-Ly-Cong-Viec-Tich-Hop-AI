using Application.Abstractions;
using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Domain;
using Identity.Domain.ValueObjects;
using SharedKernel;

namespace Identity.Application.Auth.Login;

public sealed class LoginCommandHandler(
    IValidator<LoginCommand> validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCommand, Result<AuthTokens>>
{
    // Dùng chung một thông báo cho "email không tồn tại" và "sai mật khẩu" để không lộ email nào đã đăng ký.
    private const string InvalidCredentials = "Email hoặc mật khẩu không đúng.";

    public async Task<Result<AuthTokens>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthTokens>.ValidationFailure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        Email email;
        try
        {
            email = Email.Create(request.Email);
        }
        catch (DomainException)
        {
            return Result<AuthTokens>.Failure(InvalidCredentials);
        }

        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash.Value))
            return Result<AuthTokens>.Failure(InvalidCredentials);

        user.MarkLogin();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthTokens>.Success(tokenGenerator.Generate(user));
    }
}
