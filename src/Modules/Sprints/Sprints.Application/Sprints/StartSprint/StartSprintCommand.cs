using Application.Abstractions.CQRS;
using SharedKernel;

namespace Sprints.Application.Sprints.StartSprint;

public sealed record StartSprintCommand(Guid SprintId, Guid ActorId) : ICommand<Result>;
