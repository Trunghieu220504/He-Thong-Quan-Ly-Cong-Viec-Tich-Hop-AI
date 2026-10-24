using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sprints.Domain.Entities;

namespace Sprints.Infrastructure.Persistence.Configurations;

internal sealed class SprintTaskConfiguration : IEntityTypeConfiguration<SprintTask>
{
    public void Configure(EntityTypeBuilder<SprintTask> builder)
    {
        builder.ToTable("sprint_tasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.SprintId).IsRequired();
        builder.Property(task => task.TaskId).IsRequired();
        builder.Property(task => task.AddedAt).IsRequired();

        builder.HasIndex(task => new { task.SprintId, task.TaskId })
            .IsUnique()
            .HasDatabaseName("IX_sprint_tasks_SprintId_TaskId");
    }
}
