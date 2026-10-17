using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Projects.Infrastructure.Persistence;

namespace Projects.Infrastructure;

public static class ProjectsInfrastructureRegistration
{
    public static IServiceCollection AddProjectsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ProjectsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Bảng migration lưu vào schema projects
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "projects");
            }));

        return services;
    }
}