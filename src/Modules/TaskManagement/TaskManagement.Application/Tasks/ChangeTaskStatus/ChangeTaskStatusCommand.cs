using Application.Abstractions.CQRS;
using SharedKernel;

namespace TaskManagement.Application.Tasks.ChangeTaskStatus;

public sealed record ChangeTaskStatusCommand(Guid TaskId, Guid ActorId, string Status)
    : ICommand<Result<TaskDto>>;
