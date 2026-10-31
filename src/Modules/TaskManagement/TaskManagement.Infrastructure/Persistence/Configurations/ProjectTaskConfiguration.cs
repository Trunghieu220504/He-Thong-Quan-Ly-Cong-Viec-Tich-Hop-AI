using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

internal sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        builder.ToTable("tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        // ProjectId, AssigneeId, CreatedBy chỉ là Guid tham chiếu: không FK sang schema của module khác.
        builder.Property(t => t.ProjectId).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(ProjectTask.TitleMaxLength).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(ProjectTask.DescriptionMaxLength);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.AssigneeId);
        builder.Property(t => t.DueDate);
        builder.Property(t => t.CreatedBy).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt);

        builder.HasIndex(t => new { t.ProjectId, t.Status });
        builder.HasIndex(t => t.AssigneeId);

        builder.Ignore(t => t.DomainEvents);
    }
}
