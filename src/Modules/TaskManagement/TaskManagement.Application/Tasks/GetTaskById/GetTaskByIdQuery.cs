using Application.Abstractions.CQRS;
using SharedKernel;

namespace TaskManagement.Application.Tasks.GetTaskById;

public sealed record GetTaskByIdQuery(Guid TaskId, Guid UserId)
    : IQuery<Result<TaskDto>>;
