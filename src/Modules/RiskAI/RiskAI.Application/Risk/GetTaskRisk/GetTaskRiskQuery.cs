using Application.Abstractions.CQRS;
using SharedKernel;

namespace RiskAI.Application.Risk.GetTaskRisk;

public sealed record GetTaskRiskQuery(Guid TaskId, Guid UserId)
    : IQuery<Result<TaskRiskDto>>;
