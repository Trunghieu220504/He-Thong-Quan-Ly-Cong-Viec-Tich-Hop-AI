using Application.Abstractions.CQRS;
using SharedKernel;

namespace Sprints.Application.Sprints.GetProjectSprints;

public sealed record GetProjectSprintsQuery(Guid ProjectId, Guid ActorId)
    : IQuery<Result<IReadOnlyList<SprintDto>>>;
