using EventBus;
using Identity.Contracts;
using Notifications.Application.Abstractions;
using Notifications.Contracts;
using Notifications.Domain.Entities;
using TaskManagement.Contracts.Events;

namespace Notifications.Application.EventHandlers;

public sealed class TaskAssignedNotificationHandler(
    INotificationRepository repository,
    IIdentityModuleApi identityApi,
    INotificationRealtimePusher realtimePusher,
    INotificationEmailSender emailSender)
    : IIntegrationEventHandler<TaskAssignedIntegrationEvent>
{
    public async Task Handle(TaskAssignedIntegrationEvent @event, CancellationToken ct = default)
    {
        if (@event.AssigneeId is null || @event.AssigneeId.Value == @event.ChangedBy)
            return;

        var recipientId = @event.AssigneeId.Value;
        var actor = await identityApi.GetUserSummaryAsync(@event.ChangedBy, ct);
        var actorName = string.IsNullOrWhiteSpace(actor?.DisplayName) ? "Đồng đội" : actor.DisplayName;

        var notification = Notification.Create(
            @event.EventId,
            recipientId,
            NotificationTypes.TaskAssigned,
            "Bạn được giao công việc mới",
            $"{actorName} đã giao công việc \"{@event.TaskTitle}\" cho bạn.",
            @event.TaskId,
            @event.ProjectId,
            @event.OccurredAtUtc);

        var inserted = await repository.TryAddIdempotentAsync(notification, ct);
        if (!inserted)
            return;

        var dto = new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.TaskId,
            notification.ProjectId,
            notification.IsRead,
            notification.CreatedAt);

        await realtimePusher.PushToUserAsync(recipientId, dto, ct);

        await emailSender.SendTaskAssignedEmailAsync(
            recipientId,
            @event.TaskTitle,
            actorName,
            @event.ProjectId,
            @event.TaskId,
            ct);
    }
}

public sealed class TaskStatusChangedNotificationHandler(
    INotificationRepository repository,
    IIdentityModuleApi identityApi,
    INotificationRealtimePusher realtimePusher)
    : IIntegrationEventHandler<TaskStatusChangedIntegrationEvent>
{
    public async Task Handle(TaskStatusChangedIntegrationEvent @event, CancellationToken ct = default)
    {
        var recipients = new HashSet<Guid>();
        if (@event.AssigneeId.HasValue)
            recipients.Add(@event.AssigneeId.Value);
        recipients.Add(@event.CreatedBy);
        recipients.Remove(@event.ChangedBy);

        if (recipients.Count == 0)
            return;

        var actor = await identityApi.GetUserSummaryAsync(@event.ChangedBy, ct);
        var actorName = string.IsNullOrWhiteSpace(actor?.DisplayName) ? "Đồng đội" : actor.DisplayName;

        foreach (var recipientId in recipients)
        {
            var notification = Notification.Create(
                @event.EventId,
                recipientId,
                NotificationTypes.TaskStatusChanged,
                "Cập nhật trạng thái công việc",
                $"{actorName} đã chuyển trạng thái \"{@event.TaskTitle}\" từ {@event.OldStatus} sang {@event.NewStatus}.",
                @event.TaskId,
                @event.ProjectId,
                @event.OccurredAtUtc);

            var inserted = await repository.TryAddIdempotentAsync(notification, ct);
            if (!inserted)
                continue;

            var dto = new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.TaskId,
                notification.ProjectId,
                notification.IsRead,
                notification.CreatedAt);

            await realtimePusher.PushToUserAsync(recipientId, dto, ct);
        }
    }
}