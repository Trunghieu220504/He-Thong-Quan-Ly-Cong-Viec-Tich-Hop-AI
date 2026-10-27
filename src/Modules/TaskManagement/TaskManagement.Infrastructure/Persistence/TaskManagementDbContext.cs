using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class TaskManagementDbContext : BaseDbContext
{
    public const string Schema = "task_management";

    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagementDbContext).Assembly);
    }
}
