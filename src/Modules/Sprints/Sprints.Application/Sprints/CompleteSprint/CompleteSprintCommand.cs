using Application.Abstractions.CQRS;
using SharedKernel;

namespace Sprints.Application.Sprints.CompleteSprint;

public sealed record CompleteSprintCommand(Guid SprintId, Guid ActorId) : ICommand<Result>;
