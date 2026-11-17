using EventBus;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.EventHandlers;
using TaskManagement.Contracts.Events;

namespace Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventHandler<TaskAssignedIntegrationEvent>, TaskAssignedNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<TaskStatusChangedIntegrationEvent>, TaskStatusChangedNotificationHandler>();
        return services;
    }
}