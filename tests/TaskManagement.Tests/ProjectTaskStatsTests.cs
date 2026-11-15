using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Contracts;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;
using DomainStatus = TaskManagement.Domain.Enums.ProjectTaskStatus;

namespace TaskManagement.Tests;

public sealed class ProjectTaskStatsTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Empty_project_returns_zero_counts_and_no_assignees()
    {
        await using var fixture = await Fixture.CreateAsync();
        var stats = await fixture.Api.GetProjectStatsAsync(Guid.NewGuid(), Now);
        Assert.Equal((0, 0, 0, 0, 0), (stats.Total, stats.Todo, stats.InProgress, stats.Done, stats.Overdue));
        Assert.Empty(stats.ByAssignee);
    }

    [Fact]
    public async Task Counts_statuses_and_assignees_without_other_projects_or_tracked_entities()
    {
        await using var fixture = await Fixture.CreateAsync();
        var projectId = Guid.NewGuid();
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        await fixture.SeedAsync(
            CreateTask(projectId, alice, DomainStatus.Todo, Now.AddDays(-1)),
            CreateTask(projectId, alice, DomainStatus.Done, Now.AddDays(-1)),
            CreateTask(projectId, bob, DomainStatus.InProgress, Now.AddDays(1)),
            CreateTask(projectId, null, DomainStatus.Todo),
            CreateTask(projectId, null, DomainStatus.Done),
            CreateTask(Guid.NewGuid(), alice, DomainStatus.Todo, Now.AddDays(-1)));

        var stats = await fixture.Api.GetProjectStatsAsync(projectId, Now);

        Assert.Equal((5, 2, 1, 2, 1), (stats.Total, stats.Todo, stats.InProgress, stats.Done, stats.Overdue));
        Assert.Equal(3, stats.ByAssignee.Count);
        Assert.Contains(new AssigneeTaskCount(alice, 2, 1), stats.ByAssignee);
        Assert.Contains(new AssigneeTaskCount(bob, 1, 0), stats.ByAssignee);
        Assert.Contains(new AssigneeTaskCount(null, 2, 1), stats.ByAssignee);
        Assert.Equal(stats.Total, stats.ByAssignee.Sum(a => a.Total));
        Assert.Equal(stats.Done, stats.ByAssignee.Sum(a => a.Done));
        Assert.Empty(fixture.Db.ChangeTracker.Entries());
        var sql = Assert.Single(fixture.Commands.Reads);
        Assert.Contains("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Title", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Overdue_uses_strict_cutoff_excludes_done_and_ignores_missing_due_dates()
    {
        await using var fixture = await Fixture.CreateAsync();
        var projectId = Guid.NewGuid();
        await fixture.SeedAsync(
            CreateTask(projectId, null, DomainStatus.Todo, Now.AddTicks(-10)),
            CreateTask(projectId, null, DomainStatus.InProgress, Now.AddTicks(-10)),
            CreateTask(projectId, null, DomainStatus.Todo, Now),
            CreateTask(projectId, null, DomainStatus.Todo, Now.AddTicks(10)),
            CreateTask(projectId, null, DomainStatus.Done, Now.AddDays(-1)),
            CreateTask(projectId, null, DomainStatus.InProgress));

        var stats = await fixture.Api.GetProjectStatsAsync(projectId, Now);
        Assert.Equal(6, stats.Total);
        Assert.Equal(2, stats.Overdue);
        Assert.Equal(new AssigneeTaskCount(null, 6, 1), Assert.Single(stats.ByAssignee));
    }

    [Theory]
    [InlineData(DomainStatus.Todo, 1, 0, 0)]
    [InlineData(DomainStatus.InProgress, 0, 1, 0)]
    [InlineData(DomainStatus.Done, 0, 0, 1)]
    public async Task Missing_status_groups_return_zero(DomainStatus status, int todo, int inProgress, int done)
    {
        await using var fixture = await Fixture.CreateAsync();
        var projectId = Guid.NewGuid();
        await fixture.SeedAsync(CreateTask(projectId, Guid.NewGuid(), status));
        var stats = await fixture.Api.GetProjectStatsAsync(projectId, Now);
        Assert.Equal((1, todo, inProgress, done, 0),
            (stats.Total, stats.Todo, stats.InProgress, stats.Done, stats.Overdue));
    }

    [Fact]
    public async Task Supplied_time_controls_overdue_instead_of_wall_clock()
    {
        await using var fixture = await Fixture.CreateAsync();
        var projectId = Guid.NewGuid();
        await fixture.SeedAsync(CreateTask(projectId, null, DomainStatus.Todo, Now));
        Assert.Equal(0, (await fixture.Api.GetProjectStatsAsync(projectId, Now.AddDays(-1))).Overdue);
        Assert.Equal(1, (await fixture.Api.GetProjectStatsAsync(projectId, Now.AddDays(1))).Overdue);
    }

    [Fact]
    public async Task Cancelled_request_propagates_cancellation()
    {
        await using var fixture = await Fixture.CreateAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Api.GetProjectStatsAsync(Guid.NewGuid(), Now, cancellation.Token));
    }

    private static ProjectTask CreateTask(Guid projectId, Guid? assigneeId, DomainStatus status, DateTime? dueDate = null)
    {
        var result = ProjectTask.Create(projectId, "Task for statistics", null, TaskPriority.Medium,
            assigneeId, dueDate, Guid.NewGuid());
        Assert.True(result.IsSuccess);
        var task = result.Value!;
        Assert.True(task.ChangeStatus(status).IsSuccess);
        return task;
    }

    private sealed class Fixture(TaskManagementDbContext db, ServiceProvider provider,
        SqliteConnection? connection, CommandRecorder commands) : IAsyncDisposable
    {
        public TaskManagementDbContext Db { get; } = db;
        public ITaskModuleApi Api { get; } = provider.GetRequiredService<ITaskModuleApi>();
        public CommandRecorder Commands { get; } = commands;

        public static async Task<Fixture> CreateAsync()
        {
            var commands = new CommandRecorder();
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>().AddInterceptors(commands);
            SqliteConnection? connection = null;
            var postgres = Environment.GetEnvironmentVariable("TASK_STATS_TEST_POSTGRES");
            if (string.IsNullOrEmpty(postgres))
            {
                connection = new SqliteConnection("Data Source=:memory:");
                await connection.OpenAsync();
                options.UseSqlite(connection);
            }
            else
            {
                // Never reset a caller's database: each test owns a unique disposable database.
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(postgres)
                {
                    Database = "task_stats_" + Guid.NewGuid().ToString("N")
                };
                options.UseNpgsql(builder.ConnectionString);
            }

            var db = new TaskManagementDbContext(options.Options);
            await db.Database.EnsureCreatedAsync();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "Host=unused" }).Build();
            var services = new ServiceCollection();
            services.AddTaskManagementInfrastructure(configuration);
            services.AddScoped(_ => db);
            var provider = services.BuildServiceProvider();
            commands.Reads.Clear();
            return new Fixture(db, provider, connection, commands);
        }

        public async Task SeedAsync(params ProjectTask[] tasks)
        {
            Db.Tasks.AddRange(tasks);
            await Db.SaveChangesAsync();
            Db.ChangeTracker.Clear();
            Commands.Reads.Clear();
        }

        public async ValueTask DisposeAsync()
        {
            if (connection is null)
                await Db.Database.EnsureDeletedAsync();
            await provider.DisposeAsync();
            if (connection is not null)
                await connection.DisposeAsync();
        }
    }

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<string> Reads { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Reads.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
