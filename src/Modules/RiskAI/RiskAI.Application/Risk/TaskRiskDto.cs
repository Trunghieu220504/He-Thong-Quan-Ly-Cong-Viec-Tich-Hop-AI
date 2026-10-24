namespace RiskAI.Application.Risk;

/// <summary>Response của GET /api/tasks/{taskId}/risk: { taskId, risk, source }.</summary>
public sealed record TaskRiskDto(Guid TaskId, string Risk, string Source);
