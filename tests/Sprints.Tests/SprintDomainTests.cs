using SharedKernel;
using Sprints.Domain.Entities;
using Sprints.Domain.Enums;

namespace Sprints.Tests;

public sealed class SprintDomainTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsPlannedSprint_AndNormalizesDatesToUtc()
    {
        var projectId = Guid.NewGuid();
        var startUnspecified = new DateTime(2026, 11, 1, 8, 0, 0, DateTimeKind.Unspecified);
        var endUnspecified = new DateTime(2026, 11, 15, 17, 0, 0, DateTimeKind.Unspecified);

        var result = Sprint.Create(projectId, "  Sprint 5  ", "  Hoàn thiện Vertical Slice  ", startUnspecified, endUnspecified);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(projectId, result.Value!.ProjectId);
        Assert.Equal("Sprint 5", result.Value.Name);
        Assert.Equal("Hoàn thiện Vertical Slice", result.Value.Goal);
        Assert.Equal(SprintStatus.Planned, result.Value.Status);
        Assert.Equal(DateTimeKind.Utc, result.Value.StartDate!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, result.Value.EndDate!.Value.Kind);
    }

    [Fact]
    public void Create_WithInvalidArguments_ReturnsValidationFailure()
    {
        var validProjectId = Guid.NewGuid();
        var start = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
        var endBeforeStart = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);

        // 1. ProjectId rỗng
        var emptyProject = Sprint.Create(Guid.Empty, "Sprint 1", null, null, null);
        Assert.False(emptyProject.IsSuccess);

        // 2. Tên rỗng hoặc khoảng trắng
        var emptyName = Sprint.Create(validProjectId, "   ", null, null, null);
        Assert.False(emptyName.IsSuccess);

        // 3. Tên vượt quá 200 ký tự
        var tooLongName = Sprint.Create(validProjectId, new string('A', 201), null, null, null);
        Assert.False(tooLongName.IsSuccess);

        // 4. EndDate <= StartDate
        var invalidDates = Sprint.Create(validProjectId, "Sprint 1", null, start, endBeforeStart);
        Assert.False(invalidDates.IsSuccess);
        var equalDates = Sprint.Create(validProjectId, "Sprint 1", null, start, start);
        Assert.False(equalDates.IsSuccess);
    }

    [Fact]
    public void Lifecycle_PlannedToActiveToCompleted_TransitionsCorrectly()
    {
        var sprint = Sprint.Create(Guid.NewGuid(), "Sprint 5", null, null, null).Value!;
        Assert.Equal(SprintStatus.Planned, sprint.Status);

        var startResult = sprint.Start();
        Assert.True(startResult.IsSuccess);
        Assert.Equal(SprintStatus.Active, sprint.Status);
        Assert.NotNull(sprint.UpdatedAt);

        var completeResult = sprint.Complete();
        Assert.True(completeResult.IsSuccess);
        Assert.Equal(SprintStatus.Completed, sprint.Status);
    }

    [Fact]
    public void Start_WhenAlreadyActiveOrCompleted_ReturnsConflict()
    {
        var sprint = Sprint.Create(Guid.NewGuid(), "Sprint 5", null, null, null).Value!;
        sprint.Start();

        // Start lần 2 khi đang Active -> Conflict
        var secondStart = sprint.Start();
        Assert.False(secondStart.IsSuccess);

        // Start khi đã Completed -> Conflict
        sprint.Complete();
        var startAfterComplete = sprint.Start();
        Assert.False(startAfterComplete.IsSuccess);
    }

    [Fact]
    public void Complete_WhenPlannedOrAlreadyCompleted_ReturnsConflict()
    {
        var sprint = Sprint.Create(Guid.NewGuid(), "Sprint 5", null, null, null).Value!;

        // Complete ngay khi còn Planned -> Conflict
        var completeWhenPlanned = sprint.Complete();
        Assert.False(completeWhenPlanned.IsSuccess);

        sprint.Start();
        Assert.True(sprint.Complete().IsSuccess);

        // Complete lần 2 khi đã Completed -> Conflict
        var secondComplete = sprint.Complete();
        Assert.False(secondComplete.IsSuccess);
    }

    [Fact]
    public void AddTask_WhenPlannedOrActive_AddsTask_AndRejectsEmptyOrDuplicateTask()
    {
        var sprint = Sprint.Create(Guid.NewGuid(), "Sprint 5", null, null, null).Value!;
        var taskId = Guid.NewGuid();

        // TaskId rỗng -> ValidationFailure
        var emptyTaskResult = sprint.AddTask(Guid.Empty);
        Assert.False(emptyTaskResult.IsSuccess);

        // Thêm hợp lệ
        var addResult = sprint.AddTask(taskId);
        Assert.True(addResult.IsSuccess);
        Assert.Single(sprint.Tasks);

        // Thêm trùng TaskId -> Conflict
        var duplicateResult = sprint.AddTask(taskId);
        Assert.False(duplicateResult.IsSuccess);
        Assert.Single(sprint.Tasks);
    }

    [Fact]
    public void AddTask_WhenSprintCompleted_ReturnsConflict()
    {
        var sprint = Sprint.Create(Guid.NewGuid(), "Sprint 5", null, null, null).Value!;
        sprint.Start();
        sprint.Complete();

        var addResult = sprint.AddTask(Guid.NewGuid());

        Assert.False(addResult.IsSuccess);
        Assert.Empty(sprint.Tasks);
    }
}