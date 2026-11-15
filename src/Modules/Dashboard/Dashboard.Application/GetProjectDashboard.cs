using Application.Abstractions.CQRS;
using Identity.Contracts;
using Projects.Contracts;
using SharedKernel;
using Sprints.Contracts;
using TaskManagement.Contracts;
namespace Dashboard.Application;
public sealed record GetProjectDashboardQuery(Guid ProjectId, Guid UserId) : IQuery<Result<ProjectDashboardDto>>;
public sealed record DashboardTaskCounts(int Total, int Todo, int InProgress, int Done, int Overdue, double CompletionPercent);
public sealed record DashboardAssignee(Guid? AssigneeId, string DisplayName, int Total, int Done);
public sealed record ProjectDashboardDto(Guid ProjectId, DateTimeOffset GeneratedAtUtc, DashboardTaskCounts Tasks,
    IReadOnlyList<DashboardAssignee> ByAssignee, ActiveSprintOverview? ActiveSprint);
public sealed class GetProjectDashboardQueryHandler(IProjectsModuleApi projects, ITaskModuleApi tasks,
    ISprintsModuleApi sprints, IIdentityModuleApi identity, TimeProvider clock)
    : IQueryHandler<GetProjectDashboardQuery, Result<ProjectDashboardDto>>
{
    public async Task<Result<ProjectDashboardDto>> Handle(GetProjectDashboardQuery request, CancellationToken ct)
    {
        if (!await projects.ProjectExistsAsync(request.ProjectId, ct))
            return Result<ProjectDashboardDto>.NotFound("Không tìm thấy dự án.");
        if (await projects.GetMemberRoleAsync(request.ProjectId, request.UserId, ct) is null)
            return Result<ProjectDashboardDto>.Forbidden("Bạn không phải thành viên của dự án này.");
        var now = clock.GetUtcNow();
        var stats = await tasks.GetProjectStatsAsync(request.ProjectId, now.UtcDateTime, ct);
        var assignees = new List<DashboardAssignee>();
        // Small teams: one lookup per distinct assignee. Add a batch Identity contract when needed.
        foreach (var row in stats.ByAssignee)
        {
            var name = row.AssigneeId is { } id
                ? (await identity.GetUserSummaryAsync(id, ct))?.DisplayName ?? "Người dùng không còn tồn tại"
                : "Chưa giao";
            assignees.Add(new(row.AssigneeId, name, row.Total, row.Done));
        }
        var sprint = await sprints.GetActiveSprintOverviewAsync(request.ProjectId, ct);
        return Result<ProjectDashboardDto>.Success(new(request.ProjectId, now,
            new(stats.Total, stats.Todo, stats.InProgress, stats.Done, stats.Overdue,
                stats.Total == 0 ? 0 : Math.Round(100d * stats.Done / stats.Total, 2)), assignees, sprint));
    }
}
