using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Projects.Infrastructure.Persistence;

internal sealed class ProjectsDbContextFactory : IDesignTimeDbContextFactory<ProjectsDbContext>
{
    public ProjectsDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost; Port=5432;Database=pmai; Username=pmai;Password=pmai_Tuan2_2026";

        var options = new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseNpgsql(cs, o => o.MigrationsHistoryTable("__EFMigrationsHistory", ProjectsDbContext.Schema))
            .Options;

        return new ProjectsDbContext(options);
    }
}