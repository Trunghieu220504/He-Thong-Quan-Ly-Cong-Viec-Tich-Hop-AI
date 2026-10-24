namespace TaskManagement.Contracts;

/// <summary>
/// Cổng giao tiếp công khai của module TaskManagement cho các module khác (Sprints, RiskAI...).
/// Module khác chỉ được dùng interface này, không reference TaskManagement.Domain / TaskManagement.Infrastructure.
/// </summary>
public interface ITaskModuleApi
{
    /// <summary>Lấy thông tin tóm tắt của task; null nếu không tồn tại.</summary>
    Task<TaskSummary?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm số task theo từng trạng thái trong danh sách id (dùng cho burndown).
    /// Id không tồn tại bị bỏ qua, không làm lỗi.
    /// </summary>
    Task<TaskStatusCounts> CountByStatusAsync(IReadOnlyCollection<Guid> taskIds, CancellationToken cancellationToken = default);
}
