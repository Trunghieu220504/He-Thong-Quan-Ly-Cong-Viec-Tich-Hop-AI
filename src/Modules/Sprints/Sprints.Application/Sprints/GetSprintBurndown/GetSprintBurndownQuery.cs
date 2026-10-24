using Application.Abstractions.CQRS;
using SharedKernel;

namespace Sprints.Application.Sprints.GetSprintBurndown;

public sealed record GetSprintBurndownQuery(Guid SprintId, Guid ActorId)
    : IQuery<Result<SprintBurndownDto>>;
