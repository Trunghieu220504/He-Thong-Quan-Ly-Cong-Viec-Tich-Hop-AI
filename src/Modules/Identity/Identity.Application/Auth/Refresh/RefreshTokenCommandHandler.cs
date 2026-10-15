using Application.Abstractions.CQRS;
using Identity.Application.Abstractions;
using SharedKernel;

namespace Identity.Application.Auth.Refresh;

public sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IJwtTokenGenerator tokenGenerator)
    : ICommandHandler<RefreshTokenCommand, Result<AuthTokens>>
{
    private const string InvalidToken = "Refresh token không hợp lệ hoặc đã hết hạn.";

    public async Task<Result<AuthTokens>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<AuthTokens>.ValidationFailure("Refresh token không được để trống.");

        var userId = tokenGenerator.ValidateRefreshToken(request.RefreshToken);
        if (userId is null)
            return Result<AuthTokens>.Failure(InvalidToken);

        var user = await users.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
            return Result<AuthTokens>.Failure(InvalidToken);

        return Result<AuthTokens>.Success(tokenGenerator.Generate(user));
    }
}
