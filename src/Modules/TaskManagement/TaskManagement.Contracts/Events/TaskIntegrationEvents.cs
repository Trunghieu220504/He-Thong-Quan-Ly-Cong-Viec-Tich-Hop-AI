using SharedKernel;

namespace TaskManagement.Contracts.Events;

/// <summary>
/// Phát sau khi lưu thành công việc đổi người được giao. Không phát khi giá trị không đổi.
/// <paramref name="AssigneeId"/> null nghĩa là bỏ giao việc (người nhận thông báo: không ai).
/// Giao thức at-least-once nên handler phải idempotent theo <paramref name="EventId"/>.
/// </summary>
public sealed record TaskAssignedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid TaskId,
    Guid ProjectId,
    string TaskTitle,
    Guid? AssigneeId,
    Guid? PreviousAssigneeId,
    Guid ChangedBy) : IIntegrationEvent;

/// <summary>
/// Phát sau khi lưu thành công việc đổi trạng thái. Không phát khi trạng thái không đổi.
/// Giao thức at-least-once nên handler phải idempotent theo <paramref name="EventId"/>.
/// </summary>
public sealed record TaskStatusChangedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAtUtc,
    Guid TaskId,
    Guid ProjectId,
    string TaskTitle,
    ProjectTaskStatus OldStatus,
    ProjectTaskStatus NewStatus,
    Guid? AssigneeId,
    Guid CreatedBy,
    Guid ChangedBy) : IIntegrationEvent;
