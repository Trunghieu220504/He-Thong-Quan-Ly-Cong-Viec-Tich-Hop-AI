using Application.Abstractions.CQRS;
using Projects.Contracts;
using RiskAI.Contracts;
using SharedKernel;
using TaskManagement.Contracts;

namespace RiskAI.Application.Risk.GetTaskRisk;

/// <summary>
/// Lấy task qua TaskManagement.Contracts, kiểm tra user là thành viên dự án qua Projects.Contracts,
/// dựng đặc trưng rồi hỏi ai-service. Chỉ đọc: không sửa task, không lưu gì.
/// </summary>
public sealed class GetTaskRiskQueryHandler(
    ITaskModuleApi tasks,
    IProjectsModuleApi projects,
    IRiskAiClient riskAi,
    TimeProvider clock)
    : IQueryHandler<GetTaskRiskQuery, Result<TaskRiskDto>>
{
    public const string Source = "ai-service";

    public async Task<Result<TaskRiskDto>> Handle(GetTaskRiskQuery request, CancellationToken cancellationToken)
    {
        var task = await tasks.GetTaskAsync(request.TaskId, cancellationToken);
        if (task is null)
            return Result<TaskRiskDto>.NotFound("Không tìm thấy task.");

        var role = await projects.GetMemberRoleAsync(task.ProjectId, request.UserId, cancellationToken);
        if (role is null)
            return Result<TaskRiskDto>.Forbidden("Bạn không phải thành viên của dự án này.");

        var features = new RiskAiRequest(
            TaskId: task.Id,
            EstimatedHours: null, // TaskSummary chưa có EstimatedHours; bổ sung khi TaskManagement mở field này
            DaysUntilDue: DaysUntilDue(task.DueDate, clock.GetUtcNow()),
            ProgressPercent: ProgressFrom(task.Status));

        try
        {
            var prediction = await riskAi.PredictAsync(features, cancellationToken);
            return Result<TaskRiskDto>.Success(new TaskRiskDto(task.Id, prediction.Risk, Source));
        }
        catch (RiskAiUnavailableException ex)
        {
            return Result<TaskRiskDto>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Số ngày trọn vẹn còn lại tới hạn (làm tròn xuống): còn 3,5 ngày → 3; còn vài giờ → 0;
    /// quá hạn 1 giờ → -1. Null nếu task không có hạn.
    /// </summary>
    private static int? DaysUntilDue(DateTime? dueDate, DateTimeOffset now)
    {
        if (dueDate is not { } due)
            return null;

        var dueUtc = due.Kind switch
        {
            DateTimeKind.Utc => due,
            DateTimeKind.Local => due.ToUniversalTime(),
            _ => DateTime.SpecifyKind(due, DateTimeKind.Utc) // timestamptz qua Npgsql là UTC; Unspecified coi như UTC
        };

        return (int)Math.Floor((dueUtc - now.UtcDateTime).TotalDays);
    }

    /// <summary>Task chưa có % tiến độ nên suy từ trạng thái Kanban.</summary>
    private static double? ProgressFrom(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Todo => 0,
        ProjectTaskStatus.InProgress => 50,
        ProjectTaskStatus.Done => 100,
        _ => null
    };
}
