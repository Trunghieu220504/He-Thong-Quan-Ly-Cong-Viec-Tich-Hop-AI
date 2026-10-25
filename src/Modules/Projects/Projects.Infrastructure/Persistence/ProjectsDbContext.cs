using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Projects.Application.Abstractions;
using Projects.Domain.Entities;

namespace Projects.Infrastructure.Persistence;

public sealed class ProjectsDbContext : BaseDbContext, IProjectsUnitOfWork
{
    public const string Schema = "projects";
    private const string UniqueViolation = PostgresErrorCodes.UniqueViolation;

    public ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: UniqueViolation, ConstraintName: "IX_project_members_ProjectId_UserId" })
        {
            throw new DuplicateMemberException(ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
    }
}