using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sprints.Contracts;
using Sprints.Domain.Entities;
using Sprints.Infrastructure;
using Sprints.Infrastructure.Persistence;
using TaskManagement.Contracts;

namespace Sprints.Tests;

public sealed class SprintOverviewTests
{
    [Fact]
    public async Task Overview_ReturnsNull_WhenProjectHasNoActiveSprint()
    {
        await using var db = CreateDb();
        var project = Guid.NewGuid();
        db.Sprints.Add(Sprint.Create(project, "Planned", null, null, null).Value!);
        var other = Sprint.Create(Guid.NewGuid(), "Other project", null, null, null).Value!;
        other.Start();
        db.Sprints.Add(other);
        await db.SaveChangesAsync();
        var tasks = new TaskApi();

        Assert.Null(await new SprintsModuleApi(db, tasks).GetActiveSprintOverviewAsync(project));
        Assert.Null(tasks.RequestedIds);
    }

    [Fact]
    public async Task Overview_ReturnsZeroCounts_WithoutCallingTasks_ForEmptySprint()
    {
        await using var db = CreateDb();
        var sprint = Sprint.Create(Guid.NewGuid(), "Empty", null, null, null).Value!;
        sprint.Start();
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();
        var tasks = new TaskApi();

        var result = await new SprintsModuleApi(db, tasks).GetActiveSprintOverviewAsync(sprint.ProjectId);

        Assert.NotNull(result);
        Assert.Equal(sprint.Id, result.Id);
        Assert.Equal(0, result.TotalTasks);
        Assert.Equal(0, result.CompletedTasks);
        Assert.Equal(0, result.RemainingTasks);
        Assert.Null(tasks.RequestedIds);
    }

    [Fact]
    public async Task Overview_UsesTaskModuleCounts_ExcludingMissingTasks()
    {
        await using var db = CreateDb();
        var sprint = Sprint.Create(Guid.NewGuid(), "Active", null, null, null).Value!;
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in ids) sprint.AddTask(id);
        sprint.Start();
        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var tasks = new TaskApi();
        using var cancellation = new CancellationTokenSource();

        var result = await new SprintsModuleApi(db, tasks)
            .GetActiveSprintOverviewAsync(sprint.ProjectId, cancellation.Token);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalTasks); // One of the three links references a removed task.
        Assert.Equal(1, result.CompletedTasks);
        Assert.Equal(1, result.RemainingTasks);
        Assert.Equal(ids.Order(), tasks.RequestedIds!.Order());
        Assert.Equal(cancellation.Token, tasks.Token);
    }

    [Fact]
    public void Infrastructure_RegistersSprintApiOnce()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "Host=localhost;Database=test" }).Build();
        var services = new ServiceCollection().AddSprintsInfrastructure(configuration);

        Assert.Single(services, d => d.ServiceType == typeof(ISprintsModuleApi));
    }

    private static SprintsDbContext CreateDb() => new(new DbContextOptionsBuilder<SprintsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class TaskApi : ITaskModuleApi
    {
        public IReadOnlyCollection<Guid>? RequestedIds { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<TaskStatusCounts> CountByStatusAsync(IReadOnlyCollection<Guid> taskIds,
            CancellationToken cancellationToken = default)
        {
            RequestedIds = taskIds;
            Token = cancellationToken;
            return Task.FromResult(new TaskStatusCounts(2, 1, 0, 1));
        }

        public Task<ProjectTaskStats> GetProjectStatsAsync(Guid projectId, DateTime utcNow,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<TaskSummary>> GetProjectTasksAsync(Guid projectId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TaskSummary?> GetTaskAsync(Guid taskId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
