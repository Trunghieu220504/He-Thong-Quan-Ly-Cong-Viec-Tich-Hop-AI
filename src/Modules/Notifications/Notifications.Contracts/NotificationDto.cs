namespace Notifications.Contracts;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid TaskId,
    Guid ProjectId,
    bool IsRead,
    DateTime CreatedAt);