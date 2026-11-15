using Identity.Contracts;
using Notifications.Application.Abstractions;
using Notifications.Application.EventHandlers;
using Notifications.Contracts;
using Notifications.Domain.Entities;
using TaskManagement.Contracts;
using TaskManagement.Contracts.Events;

namespace Notifications.Tests;

public sealed class NotificationsModuleTests
{
    [Fact]
    public async Task TaskAssignedHandler_CreatesNotification_AndIsIdempotentOnDuplicateEventId()
    {
        var repo = new FakeNotificationRepository();
        var identity = new FakeIdentityModuleApi();
        var pusher = new FakeRealtimePusher();
        var email = new FakeEmailSender();

        var handler = new TaskAssignedNotificationHandler(repo, identity, pusher, email);
        var eventId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var ev = new TaskAssignedIntegrationEvent(
            eventId,
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cài đặt Notifications",
            assigneeId,
            null,
            actorId);

        await handler.Handle(ev);
        await handler.Handle(ev); // Gửi trùng lần 2 cùng EventId

        Assert.Single(repo.Items);
        Assert.Single(pusher.Pushed);
        Assert.Equal(1, email.SentCount);
        Assert.Equal(assigneeId, repo.Items[0].UserId);
        Assert.Equal(NotificationTypes.TaskAssigned, repo.Items[0].Type);
    }

    [Fact]
    public async Task TaskAssignedHandler_Skips_WhenUnassignedOrSelfAssigned()
    {
        var repo = new FakeNotificationRepository();
        var identity = new FakeIdentityModuleApi();
        var pusher = new FakeRealtimePusher();
        var email = new FakeEmailSender();

        var handler = new TaskAssignedNotificationHandler(repo, identity, pusher, email);
        var userId = Guid.NewGuid();

        // 1. Bỏ gán (AssigneeId = null)
        await handler.Handle(new TaskAssignedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "Task 1", null, userId, userId));

        // 2. Tự gán cho chính mình (AssigneeId == ChangedBy)
        await handler.Handle(new TaskAssignedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "Task 2", userId, null, userId));

        Assert.Empty(repo.Items);
        Assert.Empty(pusher.Pushed);
        Assert.Equal(0, email.SentCount);
    }

    [Fact]
    public async Task TaskStatusChangedHandler_NotifiesAssigneeAndCreator_ExcludingActorAndDuplicates()
    {
        var repo = new FakeNotificationRepository();
        var identity = new FakeIdentityModuleApi();
        var pusher = new FakeRealtimePusher();

        var handler = new TaskStatusChangedNotificationHandler(repo, identity, pusher);
        var creatorId = Guid.NewGuid();
        var assigneeAndActorId = Guid.NewGuid();

        var ev = new TaskStatusChangedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tích hợp SignalR",
            ProjectTaskStatus.Todo,
            ProjectTaskStatus.InProgress,
            AssigneeId: assigneeAndActorId,
            CreatedBy: creatorId,
            ChangedBy: assigneeAndActorId);

        await handler.Handle(ev);

        Assert.Single(repo.Items);
        Assert.Equal(creatorId, repo.Items[0].UserId);
        Assert.Equal(NotificationTypes.TaskStatusChanged, repo.Items[0].Type);
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Items { get; } = [];

        public Task<bool> TryAddIdempotentAsync(Notification notification, CancellationToken ct = default)
        {
            if (Items.Any(x => x.EventId == notification.EventId && x.UserId == notification.UserId))
                return Task.FromResult(false);

            Items.Add(notification);
            return Task.FromResult(true);
        }

        public Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetPagedByUserAsync(
            Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<NotificationDto>, int)>(([], 0));

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(Items.Count(x => x.UserId == userId && !x.IsRead));

        public Task<NotificationDto?> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
            => Task.FromResult<NotificationDto?>(null);

        public Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeIdentityModuleApi : IIdentityModuleApi
    {
        public Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<UserSummary?>(new UserSummary(userId, "user@pmai.local", "Nguyễn Văn A"));
    }

    private sealed class FakeRealtimePusher : INotificationRealtimePusher
    {
        public List<(Guid UserId, NotificationDto Dto)> Pushed { get; } = [];

        public Task PushToUserAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
        {
            Pushed.Add((userId, notification));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailSender : INotificationEmailSender
    {
        public int SentCount { get; private set; }

        public Task SendTaskAssignedEmailAsync(
            Guid recipientUserId, string taskTitle, string assignedByDisplayName,
            Guid projectId, Guid taskId, CancellationToken ct = default)
        {
            SentCount++;
            return Task.CompletedTask;
        }
    }
}