using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Notifications.Application.Abstractions;
using Notifications.Contracts;

namespace Notifications.Api.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
}

public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst("sub")?.Value
            ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}

public sealed class SignalRNotificationRealtimePusher(
    IHubContext<NotificationsHub> hubContext) : INotificationRealtimePusher
{
    public Task PushToUserAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        return hubContext.Clients
            .User(userId.ToString())
            .SendAsync("notificationReceived", notification, ct);
    }
}