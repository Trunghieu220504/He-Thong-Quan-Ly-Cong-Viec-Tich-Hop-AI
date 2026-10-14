using Application.Abstractions.CQRS;
using Identity.Application.Abstractions;
using SharedKernel;

namespace Identity.Application.Auth.Refresh;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<Result<AuthTokens>>;
