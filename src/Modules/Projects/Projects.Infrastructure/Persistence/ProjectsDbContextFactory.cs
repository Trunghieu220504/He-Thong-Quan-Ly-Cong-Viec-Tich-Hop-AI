using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Projects.Infrastructure.Persistence;

internal sealed class ProjectsDbContextFactory : IDesignTimeDbContextFactory<ProjectsDbContext>
{
    public ProjectsDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings_Default")
            ?? "Host=localhost;Port=5432;Database=pmai;Username=pmai;Password=pmai";

        var options = new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseNpgsql(cs, o => o.MigrationsHistoryTable("__EFMigrationsHistory", ProjectsDbContext.Schema))
            .Options;

        return new ProjectsDbContext(options);
    }
}