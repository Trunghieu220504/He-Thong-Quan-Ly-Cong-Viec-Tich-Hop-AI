using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sprints.Application.Abstractions;
using Sprints.Domain.Entities;

namespace Sprints.Infrastructure.Persistence;

public sealed class SprintsDbContext : BaseDbContext, ISprintsUnitOfWork
{
    public const string Schema = "sprints";

    public SprintsDbContext(DbContextOptions<SprintsDbContext> options) : base(options) { }

    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SprintTask> SprintTasks => Set<SprintTask>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_sprints_ProjectId_Active"
            })
        {
            throw new DuplicateActiveSprintException(ex);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_sprint_tasks_SprintId_TaskId"
            })
        {
            throw new DuplicateSprintTaskException(ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SprintsDbContext).Assembly);
    }
}
