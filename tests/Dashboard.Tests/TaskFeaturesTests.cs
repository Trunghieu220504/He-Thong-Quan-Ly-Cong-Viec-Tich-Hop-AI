using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
namespace Dashboard.Tests;
public class TaskFeaturesTests
{
    [Fact]
    public void Estimation_is_validated_and_done_sets_100()
    {
        var task = ProjectTask.Create(Guid.NewGuid(), "Task", null, TaskPriority.Medium, null, null, Guid.NewGuid(), 12.5, 37).Value!;
        Assert.Equal(37, task.ProgressPercent);
        foreach (var invalid in new[] { -1d, 1001d, double.NaN, double.PositiveInfinity })
            Assert.True(task.Update("Task", null, TaskPriority.Medium, null, invalid, 20).IsFailure);
        Assert.True(task.Update("Task", null, TaskPriority.Medium, null, 8, 101).IsFailure);
        Assert.Equal(12.5, task.EstimatedHours);
        task.ChangeStatus(ProjectTaskStatus.Done); Assert.Equal(100, task.ProgressPercent);
        task.ChangeStatus(ProjectTaskStatus.InProgress); Assert.Equal(100, task.ProgressPercent);
        task.Update("Task", null, TaskPriority.High, DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified), 24, 63);
        Assert.Equal(63, task.ProgressPercent); Assert.Equal(DateTimeKind.Utc, task.DueDate!.Value.Kind);
    }
    [Fact]
    public void Task_migration_matches_current_model()
    {
        using var db = new TaskManagementDbContext(new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseNpgsql("Host=localhost;Database=not-used;Username=test;Password=test").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains("20261109120000_AddTaskEstimation", db.Database.GetMigrations());
    }
}
