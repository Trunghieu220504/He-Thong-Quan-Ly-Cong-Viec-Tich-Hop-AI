using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Domain.Entities;

namespace Projects.Infrastructure.Persistence.Configurations;

internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.ProjectId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.JoinedAt).IsRequired();

        builder.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}