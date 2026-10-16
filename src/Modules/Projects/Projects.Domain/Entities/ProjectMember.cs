using SharedKernel;
using Projects.Domain.Enums;

namespace Projects.Domain.Entities;

public sealed class ProjectMember : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; } // Chỉ lưu Guid tham chiếu
    public ProjectRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    // Constructor rỗng bắt buộc cho EF Core sau này ánh xạ dữ liệu
    private ProjectMember() { }

    // Factory method nội bộ để Project Aggregate tạo thành viên
    internal static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role)
    {
        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    internal void UpdateRole(ProjectRole newRole)
    {
        Role = newRole;
    }
}