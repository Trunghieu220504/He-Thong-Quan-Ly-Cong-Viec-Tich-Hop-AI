using Application.Abstractions.CQRS;
using Identity.Application.Abstractions;
using SharedKernel;

namespace Identity.Application.Auth.Login;

// Là Command (không phải Query) vì mỗi lần đăng nhập thành công sẽ cập nhật LastLoginAt.
public sealed record LoginCommand(string Email, string Password) : ICommand<Result<AuthTokens>>;
