using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projects.Domain.Entities;

namespace Projects.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        // Tên bảng: project_members, Schema: projects
        builder.ToTable("project_members", "projects");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ProjectId)
            .IsRequired();

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<int>(); // Lưu enum thành số nguyên

        builder.Property(m => m.JoinedAt)
            .IsRequired();

        // 1 user chỉ là thành viên của 1 project 1 lần duy nhất
        builder.HasIndex(m => new { m.ProjectId, m.UserId })
            .IsUnique();
    }
}