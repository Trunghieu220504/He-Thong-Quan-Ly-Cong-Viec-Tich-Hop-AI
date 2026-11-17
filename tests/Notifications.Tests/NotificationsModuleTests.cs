using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notifications.Infrastructure.Email;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TaskAssignedHandler_PreservesNotification_WhenSmtpFailsOrTimesOut(bool stall)
    {
        using var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        var port = ((IPEndPoint)server.LocalEndpoint).Port;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "127.0.0.1",
            ["Smtp:Port"] = port.ToString(),
            ["Smtp:TimeoutSeconds"] = "1"
        }).Build();
        var repo = new FakeNotificationRepository();
        var pusher = new FakeRealtimePusher();
        var identity = new FakeIdentityModuleApi();
        var logger = new EmailLogger();
        var sender = new SmtpNotificationEmailSender(config, identity, logger);
        var handler = new TaskAssignedNotificationHandler(repo, identity, pusher, sender);
        var ev = new TaskAssignedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "Task", Guid.NewGuid(), null, Guid.NewGuid());

        var handling = handler.Handle(ev);
        using var peer = await server.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(5));
        if (!stall)
            peer.Close(); // The SMTP server disconnects before sending a greeting.

        // A silent server must also finish within the configured email timeout.
        await handling.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Single(repo.Items);
        Assert.Single(pusher.Pushed);
        Assert.NotNull(logger.Error);
        if (stall)
            Assert.IsAssignableFrom<OperationCanceledException>(logger.Error);

        // Retry of the event must not create or push a second notification.
        await handler.Handle(ev);
        Assert.Single(repo.Items);
        Assert.Single(pusher.Pushed);
    }

    [Fact]
    public async Task TaskStatusChangedHandler_IsIdempotent_AndDeduplicatesRecipients()
    {
        var repo = new FakeNotificationRepository();
        var pusher = new FakeRealtimePusher();
        var handler = new TaskStatusChangedNotificationHandler(repo, new FakeIdentityModuleApi(), pusher);
        var recipient = Guid.NewGuid();
        var ev = new TaskStatusChangedIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "Task",
            ProjectTaskStatus.Todo, ProjectTaskStatus.Done,
            AssigneeId: recipient, CreatedBy: recipient, ChangedBy: Guid.NewGuid());

        await handler.Handle(ev);
        await handler.Handle(ev);

        Assert.Single(repo.Items);
        Assert.Single(pusher.Pushed);
        Assert.Equal(recipient, repo.Items[0].UserId);
    }

    private sealed class EmailLogger : ILogger<SmtpNotificationEmailSender>
    {
        public Exception? Error { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) => Error = exception;
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