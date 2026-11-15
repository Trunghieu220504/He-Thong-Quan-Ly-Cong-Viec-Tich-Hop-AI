namespace TaskManagement.Contracts;

/// <summary>
/// Trạng thái task. Tên là ProjectTaskStatus (không phải TaskStatus) để không trùng System.Threading.Tasks.TaskStatus.
/// </summary>
public enum ProjectTaskStatus
{
    Todo,
    InProgress,
    Done
}

/// <summary>Thông tin tóm tắt của task cho module khác; AssigneeId/DueDate có thể null.</summary>
public sealed record TaskSummary(
    Guid Id,
    Guid ProjectId,
    ProjectTaskStatus Status,
    DateTime? DueDate,
    Guid? AssigneeId);

/// <summary>Kết quả đếm theo trạng thái; Total là số task tìm thấy (= Todo + InProgress + Done).</summary>
public sealed record TaskStatusCounts(int Total, int Todo, int InProgress, int Done);

/// <summary>Overdue means DueDate is strictly before the supplied UTC instant and status is not Done.</summary>
public sealed record ProjectTaskStats(
    int Total, int Todo, int InProgress, int Done, int Overdue,
    IReadOnlyList<AssigneeTaskCount> ByAssignee);

/// <summary>A null AssigneeId represents unassigned tasks.</summary>
public sealed record AssigneeTaskCount(Guid? AssigneeId, int Total, int Done);
