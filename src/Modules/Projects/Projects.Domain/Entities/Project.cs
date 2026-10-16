using SharedKernel;
using Projects.Domain.Enums;

namespace Projects.Domain.Entities;

public sealed class Project : AggregateRoot<Guid>
{
    private readonly List<ProjectMember> _members = new();

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private Project() { }

    public static Result<Project> Create(string name, string? description, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Project>.Failure("Tên dự án không được để trống.");
        }

        if (name.Trim().Length > 200)
        {
            return Result<Project>.Failure("Tên dự án không được vượt quá 200 ký tự.");
        }

        if (ownerId == Guid.Empty)
        {
            return Result<Project>.Failure("OwnerId không hợp lệ.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        // Khi tạo dự án, tự động thêm Owner vào danh sách Member
        project._members.Add(ProjectMember.Create(project.Id, ownerId, ProjectRole.Owner));

        return Result<Project>.Success(project);
    }

    public Result AddMember(Guid userId, ProjectRole role)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure("UserId không hợp lệ.");
        }

        if (_members.Any(m => m.UserId == userId))
        {
            return Result.Failure("Người dùng đã là thành viên của dự án này.");
        }

        _members.Add(ProjectMember.Create(Id, userId, role));
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }
}