using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Api.Hubs;
using Notifications.Application.Abstractions;

namespace Notifications.Api;

public static class NotificationsEndpoints
{
    public static IServiceCollection AddNotificationsRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
        services.AddScoped<INotificationRealtimePusher, SignalRNotificationRealtimePusher>();
        return services;
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("", async (
            HttpContext httpContext,
            INotificationRepository repository,
            bool unreadOnly = false,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
                return Results.Unauthorized();

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, totalCount) = await repository.GetPagedByUserAsync(userId, unreadOnly, page, pageSize, ct);
            return Results.Ok(new { items, page, pageSize, totalCount });
        });

        group.MapGet("/unread-count", async (
            HttpContext httpContext,
            INotificationRepository repository,
            CancellationToken ct = default) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
                return Results.Unauthorized();

            var count = await repository.GetUnreadCountAsync(userId, ct);
            return Results.Ok(new { count });
        });

        group.MapPatch("/{id:guid}/read", async (
            Guid id,
            HttpContext httpContext,
            INotificationRepository repository,
            CancellationToken ct = default) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
                return Results.Unauthorized();

            var updated = await repository.MarkAsReadAsync(id, userId, ct);
            if (updated is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Notification not found");
            }

            return Results.Ok(updated);
        });

        group.MapPost("/read-all", async (
            HttpContext httpContext,
            INotificationRepository repository,
            CancellationToken ct = default) =>
        {
            if (!TryGetUserId(httpContext.User, out var userId))
                return Results.Unauthorized();

            await repository.MarkAllAsReadAsync(userId, ct);
            return Results.NoContent();
        });

        app.MapHub<NotificationsHub>("/hubs/notifications");

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var raw = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out userId);
    }
}