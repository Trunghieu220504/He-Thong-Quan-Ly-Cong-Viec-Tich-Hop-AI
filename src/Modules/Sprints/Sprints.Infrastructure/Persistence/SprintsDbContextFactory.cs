using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sprints.Infrastructure.Persistence;

internal sealed class SprintsDbContextFactory : IDesignTimeDbContextFactory<SprintsDbContext>
{
    public SprintsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=pmai;Username=pmai;Password=pmai_Tuan2_2026";

        var options = new DbContextOptionsBuilder<SprintsDbContext>()
            .UseNpgsql(
                connectionString,
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", SprintsDbContext.Schema))
            .Options;

        return new SprintsDbContext(options);
    }
}
