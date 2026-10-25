using SharedKernel;
using Sprints.Domain.Enums;

namespace Sprints.Domain.Entities;

public sealed class Sprint : AggregateRoot<Guid>
{
    private readonly List<SprintTask> _tasks = [];

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SprintStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<SprintTask> Tasks => _tasks.AsReadOnly();

    private Sprint() { }

    public static Result<Sprint> Create(
        Guid projectId,
        string name,
        string? goal,
        DateTime? startDate,
        DateTime? endDate)
    {
        startDate = ToUtc(startDate);
        endDate = ToUtc(endDate);

        if (projectId == Guid.Empty)
            return Result<Sprint>.ValidationFailure("ProjectId không hợp lệ.");

        if (string.IsNullOrWhiteSpace(name))
            return Result<Sprint>.ValidationFailure("Tên sprint không được để trống.");

        if (name.Trim().Length > 200)
            return Result<Sprint>.ValidationFailure("Tên sprint không được vượt quá 200 ký tự.");

        if (startDate.HasValue && endDate.HasValue && endDate.Value <= startDate.Value)
            return Result<Sprint>.ValidationFailure("Ngày kết thúc phải sau ngày bắt đầu.");

        return Result<Sprint>.Success(new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name.Trim(),
            Goal = goal?.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = SprintStatus.Planned,
            CreatedAt = DateTime.UtcNow
        });
    }

    // Npgsql chỉ ghi DateTime Kind=Utc vào timestamptz: Local đổi sang UTC, Unspecified coi là UTC.
    private static DateTime? ToUtc(DateTime? value)
    {
        if (value is not { } date)
            return null;

        return date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
    }

    public Result Start()
    {
        if (Status != SprintStatus.Planned)
            return Result.Conflict("Chỉ có thể bắt đầu sprint đang ở trạng thái Planned.");

        Status = SprintStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != SprintStatus.Active)
            return Result.Conflict("Chỉ có thể hoàn tất sprint đang ở trạng thái Active.");

        Status = SprintStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddTask(Guid taskId)
    {
        if (Status == SprintStatus.Completed)
            return Result.Conflict("Không thể thêm task vào sprint đã hoàn tất.");

        if (taskId == Guid.Empty)
            return Result.ValidationFailure("TaskId không hợp lệ.");

        if (_tasks.Any(task => task.TaskId == taskId))
            return Result.Conflict("Task đã có trong sprint này.");

        _tasks.Add(SprintTask.Create(Id, taskId));
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
