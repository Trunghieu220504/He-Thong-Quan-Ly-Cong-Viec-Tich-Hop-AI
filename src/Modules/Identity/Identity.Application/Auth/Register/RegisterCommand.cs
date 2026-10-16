using Application.Abstractions.CQRS;
using SharedKernel;

namespace Identity.Application.Auth.Register;

public sealed record RegisterCommand(string Email, string Password, string DisplayName)
    : ICommand<Result<Guid>>;
