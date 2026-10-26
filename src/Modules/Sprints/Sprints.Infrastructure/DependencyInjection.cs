using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sprints.Application.Abstractions;
using Sprints.Infrastructure.Persistence;
using TaskManagement.Contracts;

namespace Sprints.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSprintsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default.");

        services.AddDbContext<SprintsDbContext>(options => options.UseNpgsql(
            connectionString,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", SprintsDbContext.Schema)));
        services.TryAddScoped<ITaskModuleApi, UnavailableTaskModuleApi>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ISprintsUnitOfWork>(provider => provider.GetRequiredService<SprintsDbContext>());

        return services;
    }
}
