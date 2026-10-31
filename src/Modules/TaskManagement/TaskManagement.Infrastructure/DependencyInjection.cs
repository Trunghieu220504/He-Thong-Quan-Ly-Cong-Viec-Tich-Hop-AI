using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Abstractions;
using TaskManagement.Contracts;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaskManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default.");

        services.AddDbContext<TaskManagementDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TaskManagementDbContext.Schema)));
        services.AddScoped<ITaskModuleApi, TaskModuleApi>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskManagementUnitOfWork>(sp => sp.GetRequiredService<TaskManagementDbContext>());

        return services;
    }
}
