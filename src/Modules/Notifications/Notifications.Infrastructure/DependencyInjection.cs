using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    NotificationsDbContext.Schema)));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationEmailSender, SmtpNotificationEmailSender>();

        return services;
    }
}