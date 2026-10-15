using Application.Abstractions.CQRS;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Domain;
using Identity.Domain.ValueObjects;
using SharedKernel;

namespace Identity.Application.Auth.Register;

public sealed class RegisterCommandHandler(
    IValidator<RegisterCommand> validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IIdentityUnitOfWork unitOfWork)
    : ICommandHandler<RegisterCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<Guid>.ValidationFailure(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        User user;
        try
        {
            var email = Email.Create(request.Email);

            if (await users.ExistsByEmailAsync(email, cancellationToken))
                return Result<Guid>.Conflict("Email đã được sử dụng.");

            var hash = PasswordHash.Create(passwordHasher.Hash(request.Password));
            user = User.Create(email, hash, request.DisplayName);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.ValidationFailure(ex.Message);
        }

        users.Add(user);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            // Hai request đăng ký cùng email chạy đồng thời: request sau vi phạm unique index.
            return Result<Guid>.Conflict("Email đã được sử dụng.");
        }

        return Result<Guid>.Success(user.Id);
    }
}
