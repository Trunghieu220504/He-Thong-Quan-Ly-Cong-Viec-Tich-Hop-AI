using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Notifications.Application.Abstractions;
using Notifications.Contracts;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Persistence;

public sealed class NotificationRepository(
    NotificationsDbContext dbContext,
    ILogger<NotificationRepository> logger) : INotificationRepository
{
    public async Task<bool> TryAddIdempotentAsync(Notification notification, CancellationToken ct = default)
    {
        var exists = await dbContext.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.EventId == notification.EventId && n.UserId == notification.UserId, ct);

        if (exists)
            return false;

        dbContext.Notifications.Add(notification);

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Notifications_EventId_UserId"
        })
        {
            logger.LogWarning(
                ex,
                "Duplicate notification ignored for EventId {EventId} and UserId {UserId}",
                notification.EventId,
                notification.UserId);
            dbContext.Entry(notification).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.TaskId,
                n.ProjectId,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<NotificationDto?> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification is null)
            return null;

        if (!notification.IsRead)
        {
            notification.MarkAsRead();
            await dbContext.SaveChangesAsync(ct);
        }

        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.TaskId,
            notification.ProjectId,
            notification.IsRead,
            notification.CreatedAt);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unreadList = await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (unreadList.Count == 0)
            return;

        foreach (var item in unreadList)
            item.MarkAsRead();

        await dbContext.SaveChangesAsync(ct);
    }
}