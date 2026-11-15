using Notifications.Contracts;
using Notifications.Domain.Entities;

namespace Notifications.Application.Abstractions;

public interface INotificationRepository
{
    Task<bool> TryAddIdempotentAsync(Notification notification, CancellationToken ct = default);
    Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationDto?> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}

public interface INotificationRealtimePusher
{
    Task PushToUserAsync(Guid userId, NotificationDto notification, CancellationToken ct = default);
}

public interface INotificationEmailSender
{
    Task SendTaskAssignedEmailAsync(
        Guid recipientUserId,
        string taskTitle,
        string assignedByDisplayName,
        Guid projectId,
        Guid taskId,
        CancellationToken ct = default);
}