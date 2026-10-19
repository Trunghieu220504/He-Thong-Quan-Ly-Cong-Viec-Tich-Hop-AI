using Application.Abstractions.CQRS;
using SharedKernel;

namespace Projects.Application.Projects.GetProjectById;

public sealed record GetProjectByIdQuery(Guid ProjectId, Guid UserId)
    : IQuery<Result<ProjectDetailDto>>;