using SharedKernel;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests;

/// <summary>Unit test domain <see cref="ProjectTask"/>: Task v2 (giờ ước tính, tiến độ), chuẩn hóa hạn UTC, trạng thái.</summary>
public sealed class ProjectTaskTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();

    private static ProjectTask NewTask(double? estimatedHours = null, int progress = 0, Guid? assignee = null) =>
        ProjectTask.Create(ProjectId, "Task", null, TaskPriority.Medium, assignee, null, Creator, estimatedHours, progress).Value!;

    // ---------- Create ----------

    [Fact]
    public void Create_without_estimation_uses_defaults()
    {
        var task = ProjectTask.Create(ProjectId, "  Viết báo cáo  ", "  mô tả  ", TaskPriority.High, null, null, Creator).Value!;

        Assert.Equal("Viết báo cáo", task.Title);
        Assert.Equal("mô tả", task.Description);
        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
        Assert.Null(task.EstimatedHours);
        Assert.Equal(0, task.ProgressPercent);
        Assert.Null(task.UpdatedAt);
        Assert.NotEqual(Guid.Empty, task.Id);
    }

    [Fact]
    public void Create_keeps_estimation_and_progress()
    {
        var task = NewTask(12.5, 37);

        Assert.Equal(12.5, task.EstimatedHours);
        Assert.Equal(37, task.ProgressPercent);
        Assert.Null(task.UpdatedAt);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1000d)]
    public void Create_accepts_estimation_boundaries(double hours) =>
        Assert.Equal(hours, NewTask(hours).EstimatedHours);

    [Theory]
    [InlineData(-1d)]
    [InlineData(-0.01)]
    [InlineData(1000.01)]
    [InlineData(1001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_rejects_estimation_outside_0_to_1000(double hours)
    {
        var result = ProjectTask.Create(ProjectId, "Task", null, TaskPriority.Low, null, null, Creator, hours);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_rejects_progress_outside_0_to_100(int progress)
    {
        var result = ProjectTask.Create(ProjectId, "Task", null, TaskPriority.Low, null, null, Creator, 1, progress);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_title(string title) =>
        Assert.True(ProjectTask.Create(ProjectId, title, null, TaskPriority.Low, null, null, Creator).IsFailure);

    [Fact]
    public void Create_title_limit_is_200_characters_after_trim()
    {
        Assert.True(ProjectTask.Create(ProjectId, " " + new string('a', ProjectTask.TitleMaxLength) + " ",
            null, TaskPriority.Low, null, null, Creator).IsSuccess);
        Assert.True(ProjectTask.Create(ProjectId, new string('a', ProjectTask.TitleMaxLength + 1),
            null, TaskPriority.Low, null, null, Creator).IsFailure);
    }

    [Fact]
    public void Create_description_limit_is_2000_characters()
    {
        Assert.True(ProjectTask.Create(ProjectId, "Task", new string('d', ProjectTask.DescriptionMaxLength),
            TaskPriority.Low, null, null, Creator).IsSuccess);
        Assert.True(ProjectTask.Create(ProjectId, "Task", new string('d', ProjectTask.DescriptionMaxLength + 1),
            TaskPriority.Low, null, null, Creator).IsFailure);
    }

    [Fact]
    public void Create_rejects_empty_ids_and_undefined_priority()
    {
        Assert.True(ProjectTask.Create(Guid.Empty, "Task", null, TaskPriority.Low, null, null, Creator).IsFailure);
        Assert.True(ProjectTask.Create(ProjectId, "Task", null, TaskPriority.Low, null, null, Guid.Empty).IsFailure);
        Assert.True(ProjectTask.Create(ProjectId, "Task", null, TaskPriority.Low, Guid.Empty, null, Creator).IsFailure);
        Assert.True(ProjectTask.Create(ProjectId, "Task", null, (TaskPriority)99, null, null, Creator).IsFailure);
    }

    // ---------- Update ----------

    [Fact]
    public void Update_changes_fields_trims_text_and_sets_UpdatedAt()
    {
        var task = NewTask();

        var result = task.Update("  Tiêu đề mới ", "  ", TaskPriority.High, null, 24, 63);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tiêu đề mới", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(24, task.EstimatedHours);
        Assert.Equal(63, task.ProgressPercent);
        Assert.NotNull(task.UpdatedAt);
    }

    [Fact]
    public void Update_can_clear_estimation_with_null()
    {
        var task = NewTask(8, 10);

        Assert.True(task.Update("Task", null, TaskPriority.Medium, null, null, 10).IsSuccess);
        Assert.Null(task.EstimatedHours);
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(1001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Update_rejects_invalid_estimation_and_changes_nothing(double hours)
    {
        var task = NewTask(12.5, 37);

        var result = task.Update("Đổi tên", "mô tả", TaskPriority.High, null, hours, 80);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        AssertUnchanged(task);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Update_rejects_invalid_progress_and_changes_nothing(int progress)
    {
        var task = NewTask(12.5, 37);

        var result = task.Update("Đổi tên", "mô tả", TaskPriority.High, null, 5, progress);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        AssertUnchanged(task);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_rejects_blank_title_and_changes_nothing(string title)
    {
        var task = NewTask(12.5, 37);

        var result = task.Update(title, "mô tả", TaskPriority.High, null, 5, 80);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        AssertUnchanged(task);
    }

    [Fact]
    public void Update_rejects_too_long_title_description_and_undefined_priority()
    {
        var task = NewTask(12.5, 37);

        Assert.True(task.Update(new string('a', 201), null, TaskPriority.Low, null, 5, 5).IsFailure);
        Assert.True(task.Update("Task", new string('d', 2001), TaskPriority.Low, null, 5, 5).IsFailure);
        Assert.True(task.Update("Task", null, (TaskPriority)0, null, 5, 5).IsFailure);
        AssertUnchanged(task);
    }

    [Fact]
    public void Update_on_done_task_keeps_progress_at_100()
    {
        var task = NewTask(progress: 20);
        task.ChangeStatus(ProjectTaskStatus.Done);

        Assert.True(task.Update("Task", null, TaskPriority.Medium, null, 4, 10).IsSuccess);
        Assert.Equal(100, task.ProgressPercent);
    }

    [Fact]
    public void Update_does_not_change_status_or_assignee()
    {
        var assignee = Guid.NewGuid();
        var task = NewTask(assignee: assignee);
        task.ChangeStatus(ProjectTaskStatus.InProgress);

        task.Update("Task mới", null, TaskPriority.High, null, 3, 50);

        Assert.Equal(ProjectTaskStatus.InProgress, task.Status);
        Assert.Equal(assignee, task.AssigneeId);
    }

    // ---------- Hạn chót UTC ----------

    [Fact]
    public void DueDate_without_timezone_is_treated_as_utc()
    {
        var local = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var task = ProjectTask.Create(ProjectId, "Task", null, TaskPriority.Low, null, local, Creator).Value!;

        Assert.Equal(DateTimeKind.Utc, task.DueDate!.Value.Kind);
        Assert.Equal(local, task.DueDate.Value);
    }

    [Fact]
    public void DueDate_with_local_kind_is_converted_to_utc()
    {
        var local = new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Local);

        var task = NewTask();
        task.Update("Task", null, TaskPriority.Low, local, null, 0);

        Assert.Equal(DateTimeKind.Utc, task.DueDate!.Value.Kind);
        Assert.Equal(local.ToUniversalTime(), task.DueDate.Value);
    }

    [Fact]
    public void DueDate_can_be_cleared_with_null()
    {
        var task = ProjectTask.Create(ProjectId, "Task", null, TaskPriority.Low, null,
            new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), Creator).Value!;

        task.Update("Task", null, TaskPriority.Low, null, null, 0);

        Assert.Null(task.DueDate);
    }

    // ---------- Trạng thái và giao việc ----------

    [Fact]
    public void ChangeStatus_to_done_sets_progress_100_and_leaving_done_keeps_it()
    {
        var task = NewTask(progress: 30);

        Assert.True(task.ChangeStatus(ProjectTaskStatus.Done).IsSuccess);
        Assert.Equal(100, task.ProgressPercent);

        task.ChangeStatus(ProjectTaskStatus.InProgress);
        Assert.Equal(100, task.ProgressPercent);
    }

    [Fact]
    public void ChangeStatus_to_same_value_is_noop_and_does_not_touch_UpdatedAt()
    {
        var task = NewTask();

        Assert.True(task.ChangeStatus(ProjectTaskStatus.Todo).IsSuccess);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_rejects_undefined_value()
    {
        var task = NewTask();

        Assert.True(task.ChangeStatus((ProjectTaskStatus)0).IsFailure);
        Assert.Equal(ProjectTaskStatus.Todo, task.Status);
    }

    [Fact]
    public void Assign_is_idempotent_supports_unassign_and_rejects_empty_guid()
    {
        var assignee = Guid.NewGuid();
        var task = NewTask();

        Assert.True(task.Assign(assignee).IsSuccess);
        Assert.Equal(assignee, task.AssigneeId);
        Assert.NotNull(task.UpdatedAt);

        Assert.True(task.Assign(null).IsSuccess);
        Assert.Null(task.AssigneeId);

        Assert.True(task.Assign(Guid.Empty).IsFailure);
        Assert.Null(task.AssigneeId);
    }

    [Fact]
    public void Assign_same_assignee_does_not_touch_UpdatedAt()
    {
        var assignee = Guid.NewGuid();
        var task = NewTask(assignee: assignee);

        Assert.True(task.Assign(assignee).IsSuccess);
        Assert.Null(task.UpdatedAt);
    }

    private static void AssertUnchanged(ProjectTask task)
    {
        Assert.Equal("Task", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(TaskPriority.Medium, task.Priority);
        Assert.Equal(12.5, task.EstimatedHours);
        Assert.Equal(37, task.ProgressPercent);
        Assert.Null(task.UpdatedAt);
    }
}
