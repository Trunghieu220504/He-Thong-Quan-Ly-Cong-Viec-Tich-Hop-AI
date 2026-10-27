using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Sprints.Infrastructure.Persistence;

public sealed class SprintsDbContext : BaseDbContext
{
    public const string Schema = "sprints";

    public SprintsDbContext(DbContextOptions<SprintsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SprintsDbContext).Assembly);
    }
}
