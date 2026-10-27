using Application.Abstractions.CQRS;
using SharedKernel;

namespace Sprints.Application.Sprints.AddTaskToSprint;

public sealed record AddTaskToSprintCommand(Guid SprintId, Guid ActorId, Guid TaskId) : ICommand<Result>;
