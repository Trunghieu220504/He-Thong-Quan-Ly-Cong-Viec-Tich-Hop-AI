using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sprints.Domain.Entities;

namespace Sprints.Infrastructure.Persistence.Configurations;

internal sealed class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("sprints");
        builder.HasKey(sprint => sprint.Id);
        builder.Property(sprint => sprint.Id).ValueGeneratedNever();
        builder.Property(sprint => sprint.ProjectId).IsRequired();
        builder.Property(sprint => sprint.Name).HasMaxLength(200).IsRequired();
        builder.Property(sprint => sprint.Goal).HasMaxLength(2000);
        builder.Property(sprint => sprint.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(sprint => sprint.StartDate);
        builder.Property(sprint => sprint.EndDate);
        builder.Property(sprint => sprint.CreatedAt).IsRequired();
        builder.Property(sprint => sprint.UpdatedAt);
        builder.Ignore(sprint => sprint.DomainEvents);

        builder.HasMany(sprint => sprint.Tasks)
            .WithOne()
            .HasForeignKey(task => task.SprintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(sprint => sprint.Tasks).HasField("_tasks").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(sprint => sprint.ProjectId)
            .IsUnique()
            .HasDatabaseName("IX_sprints_ProjectId_Active")
            .HasFilter("\"Status\" = 'Active'");
    }
}
