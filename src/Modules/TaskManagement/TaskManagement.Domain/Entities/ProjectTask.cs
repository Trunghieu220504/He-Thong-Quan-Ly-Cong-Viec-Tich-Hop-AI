using SharedKernel;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public sealed class ProjectTask : AggregateRoot<Guid>
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public Guid ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ProjectTask() { }

    public static Result<ProjectTask> Create(
        Guid projectId,
        string title,
        string? description,
        TaskPriority priority,
        Guid? assigneeId,
        DateTime? dueDate,
        Guid createdBy)
    {
        if (projectId == Guid.Empty)
            return Result<ProjectTask>.Failure("ProjectId không hợp lệ.");

        if (createdBy == Guid.Empty)
            return Result<ProjectTask>.Failure("CreatedBy không hợp lệ.");

        if (string.IsNullOrWhiteSpace(title))
            return Result<ProjectTask>.Failure("Tiêu đề công việc không được để trống.");

        if (title.Trim().Length > TitleMaxLength)
            return Result<ProjectTask>.Failure($"Tiêu đề công việc không được vượt quá {TitleMaxLength} ký tự.");

        if (description is not null && description.Trim().Length > DescriptionMaxLength)
            return Result<ProjectTask>.Failure($"Mô tả công việc không được vượt quá {DescriptionMaxLength} ký tự.");

        if (!Enum.IsDefined(priority))
            return Result<ProjectTask>.Failure("Mức ưu tiên không hợp lệ.");

        if (assigneeId == Guid.Empty)
            return Result<ProjectTask>.Failure("AssigneeId không hợp lệ.");

        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = ProjectTaskStatus.Todo,
            Priority = priority,
            AssigneeId = assigneeId,
            DueDate = NormalizeToUtc(dueDate),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        return Result<ProjectTask>.Success(task);
    }

    public Result ChangeStatus(ProjectTaskStatus status)
    {
        if (!Enum.IsDefined(status))
            return Result.Failure("Trạng thái công việc không hợp lệ.");

        if (Status == status)
            return Result.Success();

        Status = status;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Gán công việc cho một thành viên; <c>null</c> để bỏ gán.</summary>
    public Result Assign(Guid? assigneeId)
    {
        if (assigneeId == Guid.Empty)
            return Result.Failure("AssigneeId không hợp lệ.");

        if (AssigneeId == assigneeId)
            return Result.Success();

        AssigneeId = assigneeId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    // Npgsql chỉ ghi timestamptz với DateTime Kind=Utc; ngày không kèm múi giờ được coi là UTC.
    private static DateTime? NormalizeToUtc(DateTime? value)
    {
        if (value is not { } date) return null;

        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }
}
