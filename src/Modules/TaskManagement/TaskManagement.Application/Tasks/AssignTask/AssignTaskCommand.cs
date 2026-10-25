using Application.Abstractions.CQRS;
using SharedKernel;

namespace TaskManagement.Application.Tasks.AssignTask;

/// <summary><paramref name="AssigneeId"/> = null nghĩa là bỏ gán.</summary>
public sealed record AssignTaskCommand(Guid TaskId, Guid ActorId, Guid? AssigneeId)
    : ICommand<Result<TaskDto>>;
