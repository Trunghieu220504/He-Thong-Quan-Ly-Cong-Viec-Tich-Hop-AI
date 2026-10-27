using Application.Abstractions.CQRS;
using SharedKernel;

namespace Sprints.Application.Sprints.CreateSprint;

public sealed record CreateSprintCommand(
    Guid ProjectId,
    Guid ActorId,
    string Name,
    string? Goal,
    DateTime? StartDate,
    DateTime? EndDate) : ICommand<Result<Guid>>;
