using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Projects.Application.Abstractions;
using Projects.Infrastructure.Persistence;

namespace Projects.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default.");

        services.AddDbContext<ProjectsDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ProjectsDbContext.Schema)));

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectsUnitOfWork>(sp => sp.GetRequiredService<ProjectsDbContext>());

        return services;
    }
}