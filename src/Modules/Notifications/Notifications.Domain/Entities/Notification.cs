namespace Notifications.Domain.Entities;

public static class NotificationTypes
{
    public const string TaskAssigned = "TaskAssigned";
    public const string TaskStatusChanged = "TaskStatusChanged";
}

public sealed class Notification
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public Guid TaskId { get; private set; }
    public Guid ProjectId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        Guid eventId,
        Guid userId,
        string type,
        string title,
        string message,
        Guid taskId,
        Guid projectId,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new Notification
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Type = type.Trim(),
            Title = title.Trim(),
            Message = message.Trim(),
            TaskId = taskId,
            ProjectId = projectId,
            IsRead = false,
            CreatedAt = createdAtUtc
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}