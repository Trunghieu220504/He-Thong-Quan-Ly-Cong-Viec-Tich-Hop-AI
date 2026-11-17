namespace TaskManagement.Contracts;

/// <summary>
/// Cổng giao tiếp công khai của module TaskManagement cho các module khác (Sprints, RiskAI...).
/// Module khác chỉ được dùng interface này, không reference TaskManagement.Domain / TaskManagement.Infrastructure.
/// </summary>
public interface ITaskModuleApi
{
    /// <summary>
    /// Read project statistics at the supplied UTC instant. Empty or unknown projects return zero counts.
    /// The calling module must check project existence and membership before exposing these statistics.
    /// </summary>
    Task<ProjectTaskStats> GetProjectStatsAsync(Guid projectId, DateTime utcNow, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskSummary>> GetProjectTasksAsync(Guid projectId, CancellationToken cancellationToken = default);
    /// <summary>Lấy thông tin tóm tắt của task; null nếu không tồn tại.</summary>
    Task<TaskSummary?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm số task theo từng trạng thái trong danh sách id (dùng cho burndown).
    /// Id không tồn tại bị bỏ qua, không làm lỗi.
    /// </summary>
    Task<TaskStatusCounts> CountByStatusAsync(IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken = default);
}
