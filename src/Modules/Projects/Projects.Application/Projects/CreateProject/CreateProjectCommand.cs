using Application.Abstractions.CQRS;
using SharedKernel;

namespace Projects.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(Guid OwnerId, string Name, string? Description)
    : ICommand<Result<Guid>>;