using Application.Abstractions.CQRS;
using Projects.Contracts;
using RiskAI.Contracts;
using RiskAI.Application.Risk.GetTaskRisk;
using SharedKernel;
using TaskManagement.Contracts;
namespace RiskAI.Application.Risk;
public sealed record GetProjectRisksQuery(Guid ProjectId, Guid UserId) : IQuery<Result<ProjectRisksDto>>;
public sealed record ProjectRisksDto(Guid ProjectId, IReadOnlyList<TaskRiskDto> Items);
public sealed class GetProjectRisksQueryHandler(IProjectsModuleApi projects, ITaskModuleApi tasks,
    IRiskAiClient ai, TimeProvider clock) : IQueryHandler<GetProjectRisksQuery, Result<ProjectRisksDto>>
{
    public async Task<Result<ProjectRisksDto>> Handle(GetProjectRisksQuery request, CancellationToken ct)
    {
        if (!await projects.ProjectExistsAsync(request.ProjectId, ct))
            return Result<ProjectRisksDto>.NotFound("Không tìm thấy dự án.");
        if (await projects.GetMemberRoleAsync(request.ProjectId, request.UserId, ct) is null)
            return Result<ProjectRisksDto>.Forbidden("Bạn không phải thành viên của dự án này.");
        var rows = await tasks.GetProjectTasksAsync(request.ProjectId, ct);
        var now = clock.GetUtcNow();
        var results = new TaskRiskDto[rows.Count];
        try
        {
            // Bound fan-out; EF work completes before concurrent HTTP calls start.
            await Parallel.ForEachAsync(Enumerable.Range(0, rows.Count),
                new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct }, async (index, token) =>
                {
                    var task = rows[index];
                    var prediction = await ai.PredictAsync(new(task.Id, task.EstimatedHours,
                        GetTaskRiskQueryHandler.DaysUntilDue(task.DueDate, now), task.ProgressPercent), token);
                    results[index] = new(task.Id, prediction.Risk, GetTaskRiskQueryHandler.Source);
                });
            return Result<ProjectRisksDto>.Success(new(request.ProjectId, results));
        }
        catch (RiskAiUnavailableException ex) { return Result<ProjectRisksDto>.Failure(ex.Message); }
    }
}
