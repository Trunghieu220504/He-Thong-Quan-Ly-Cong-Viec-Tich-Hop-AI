using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class TaskManagementDbContext : BaseDbContext, ITaskManagementUnitOfWork
{
    public const string Schema = "tasks";

    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : base(options) { }

    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementDbContext).Assembly);
    }
}
