using Application.Abstractions.CQRS;
using SharedKernel;

namespace Projects.Application.Projects.GetProjects;

public sealed record GetProjectsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IQuery<Result<PagedResult<ProjectSummaryDto>>>;