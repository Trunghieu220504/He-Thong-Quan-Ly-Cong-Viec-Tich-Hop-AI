using Application.Abstractions.CQRS;
using SharedKernel;

namespace TaskManagement.Application.Tasks.GetProjectTasks;

public sealed record GetProjectTasksQuery(Guid ProjectId, Guid UserId, string? Status = null, int Page = 1, int PageSize = 50)
    : IQuery<Result<PagedResult<TaskDto>>>;
