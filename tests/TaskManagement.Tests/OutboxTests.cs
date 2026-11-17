using System.Text.Json;
using EventBus;
using Infrastructure.Persistence.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using TaskManagement.Application.Abstractions;
using TaskManagement.Contracts.Events;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;
using ContractStatus = TaskManagement.Contracts.ProjectTaskStatus;

namespace TaskManagement.Tests;

/// <summary>Outbox thật trên SQLite: ghi cùng transaction, gửi tức thì sau khi lưu, lỗi gửi không làm hỏng task.</summary>
public sealed class OutboxTests
{
    private static TaskAssignedIntegrationEvent NewAssigned(Guid? taskId = null) => new(
        Guid.NewGuid(), new DateTime(2026, 11, 18, 3, 0, 0, DateTimeKind.Utc), taskId ?? Guid.NewGuid(),
        Guid.NewGuid(), "Viết báo cáo", Guid.NewGuid(), null, Guid.NewGuid());

    private static TaskStatusChangedIntegrationEvent NewStatusChanged() => new(
        Guid.NewGuid(), new DateTime(2026, 11, 18, 3, 0, 0, DateTimeKind.Utc), Guid.NewGuid(), Guid.NewGuid(),
        "Viết báo cáo", ContractStatus.Todo, ContractStatus.InProgress, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    private static ProjectTask NewTask() =>
        ProjectTask.Create(Guid.NewGuid(), "Task", null, TaskPriority.Medium, null, null, Guid.NewGuid()).Value!;

    [Fact]
    public async Task Enqueued_event_and_task_are_saved_together_with_unprocessed_row()
    {
        await using var f = await Fixture.CreateAsync();
        var task = NewTask();
        var e = NewAssigned(task.Id);
        f.Db.Tasks.Add(task);
        f.Outbox.Enqueue(e);
        // SQLite trả Kind=Unspecified khi đọc lại nên kiểm Kind=Utc ở phía ghi (Npgsql timestamptz yêu cầu Utc).
        Assert.Equal(DateTimeKind.Utc, f.Db.ChangeTracker.Entries<OutboxMessage>().Single().Entity.OccurredAtUtc.Kind);
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();

        Assert.Single(await f.Db.Tasks.ToListAsync());
        var row = await f.Db.OutboxMessages.SingleAsync();
        Assert.Equal(typeof(TaskAssignedIntegrationEvent).FullName, row.EventType);
        Assert.Null(row.ProcessedAtUtc);
        Assert.Equal(e, JsonSerializer.Deserialize<TaskAssignedIntegrationEvent>(row.Payload, OutboxMessage.JsonOptions));
    }

    [Fact]
    public async Task Payload_round_trips_status_event_with_enum_names()
    {
        await using var f = await Fixture.CreateAsync();
        var e = NewStatusChanged();
        f.Outbox.Enqueue(e);
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();

        var row = await f.Db.OutboxMessages.SingleAsync();
        Assert.Equal(typeof(TaskStatusChangedIntegrationEvent).FullName, row.EventType);
        Assert.Contains("\"InProgress\"", row.Payload);
        Assert.Equal(e, JsonSerializer.Deserialize<TaskStatusChangedIntegrationEvent>(row.Payload, OutboxMessage.JsonOptions));
    }

    [Fact]
    public async Task Failed_save_leaves_neither_task_nor_outbox_row()
    {
        await using var f = await Fixture.CreateAsync();
        f.Failure.FailSaves = 1;
        f.Db.Tasks.Add(NewTask());
        f.Outbox.Enqueue(NewAssigned());

        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Db.SaveChangesAsync());

        f.Db.ChangeTracker.Clear();
        Assert.Empty(await f.Db.Tasks.ToListAsync());
        Assert.Empty(await f.Db.OutboxMessages.ToListAsync());
        Assert.Empty(f.Bus.Published);
    }

    [Fact]
    public async Task Dispatch_publishes_after_save_and_marks_rows_processed()
    {
        await using var f = await Fixture.CreateAsync();
        var first = NewAssigned();
        var second = NewStatusChanged();
        f.Outbox.Enqueue(first);
        f.Outbox.Enqueue(second);
        await f.Db.SaveChangesAsync();
        Assert.Empty(f.Bus.Published); // chưa Dispatch thì chưa gửi

        var before = DateTime.UtcNow;
        await f.Outbox.DispatchAsync();

        Assert.Equal(new object[] { first, second }, f.Bus.Published);
        f.Db.ChangeTracker.Clear();
        var rows = await f.Db.OutboxMessages.ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r =>
        {
            Assert.NotNull(r.ProcessedAtUtc);
            Assert.True(r.ProcessedAtUtc >= before.AddSeconds(-1));
        });
    }

    [Fact]
    public async Task Dispatch_twice_does_not_republish()
    {
        await using var f = await Fixture.CreateAsync();
        f.Outbox.Enqueue(NewAssigned());
        await f.Db.SaveChangesAsync();
        await f.Outbox.DispatchAsync();
        await f.Outbox.DispatchAsync();
        Assert.Single(f.Bus.Published);
    }

    [Fact]
    public async Task Dispatch_without_enqueued_events_does_nothing()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Outbox.DispatchAsync();
        Assert.Empty(f.Bus.Published);
        Assert.Empty(await f.Db.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Publish_failure_does_not_throw_and_row_stays_unprocessed_for_retry()
    {
        await using var f = await Fixture.CreateAsync();
        f.Bus.Failure = new InvalidOperationException("bus down");
        f.Outbox.Enqueue(NewAssigned());
        await f.Db.SaveChangesAsync();

        await f.Outbox.DispatchAsync(); // không ném: task đã lưu

        f.Db.ChangeTracker.Clear();
        Assert.Null((await f.Db.OutboxMessages.SingleAsync()).ProcessedAtUtc);
    }

    [Fact]
    public async Task One_failing_event_does_not_block_marking_the_others()
    {
        await using var f = await Fixture.CreateAsync();
        var bad = NewAssigned();
        var good = NewStatusChanged();
        f.Bus.FailWhen = e => ReferenceEquals(e, bad);
        f.Outbox.Enqueue(bad);
        f.Outbox.Enqueue(good);
        await f.Db.SaveChangesAsync();

        await f.Outbox.DispatchAsync();

        Assert.Equal(new object[] { good }, f.Bus.Published);
        f.Db.ChangeTracker.Clear();
        var rows = await f.Db.OutboxMessages.ToListAsync();
        Assert.Equal(1, rows.Count(r => r.ProcessedAtUtc is not null));
        Assert.Equal(typeof(TaskAssignedIntegrationEvent).FullName, rows.Single(r => r.ProcessedAtUtc is null).EventType);
    }

    [Fact]
    public async Task Failure_while_marking_processed_does_not_throw()
    {
        await using var f = await Fixture.CreateAsync();
        f.Outbox.Enqueue(NewAssigned());
        await f.Db.SaveChangesAsync();
        f.Failure.FailSaves = 1; // lần lưu kế tiếp = bước đánh dấu đã xử lý

        await f.Outbox.DispatchAsync();

        Assert.Single(f.Bus.Published);
        f.Db.ChangeTracker.Clear();
        Assert.Null((await f.Db.OutboxMessages.SingleAsync()).ProcessedAtUtc);
    }

    [Fact]
    public async Task Cancelled_request_propagates_and_row_stays_unprocessed()
    {
        await using var f = await Fixture.CreateAsync();
        f.Outbox.Enqueue(NewAssigned());
        await f.Db.SaveChangesAsync();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        f.Bus.ThrowCancellation = true;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Outbox.DispatchAsync(cts.Token));

        f.Db.ChangeTracker.Clear();
        Assert.Null((await f.Db.OutboxMessages.SingleAsync()).ProcessedAtUtc);
    }

    [Fact]
    public async Task Real_in_process_bus_reaches_subscribers_and_handler_failure_does_not_throw()
    {
        await using var f = await Fixture.CreateAsync(useRealBus: true);
        var e = NewAssigned();
        f.Outbox.Enqueue(e);
        await f.Db.SaveChangesAsync();

        await f.Outbox.DispatchAsync();

        Assert.Equal(e, Assert.Single(f.Subscriber.Received));
    }

    [Fact]
    public void Outbox_table_is_in_tasks_schema_with_pending_index()
    {
        using var db = new TaskManagementDbContext(new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseNpgsql("Host=unused").Options);
        var entity = db.Model.FindEntityType(typeof(OutboxMessage))!;
        Assert.Equal("outbox_messages", entity.GetTableName());
        Assert.Equal(TaskManagementDbContext.Schema, entity.GetSchema());
        var index = Assert.Single(entity.GetIndexes());
        Assert.Equal("IX_outbox_messages_Pending", index.GetDatabaseName());
        Assert.Contains("ProcessedAtUtc", index.GetFilter());
    }

    private sealed class Subscriber : IIntegrationEventHandler<TaskAssignedIntegrationEvent>
    {
        public List<TaskAssignedIntegrationEvent> Received { get; } = [];
        public Task Handle(TaskAssignedIntegrationEvent @event, CancellationToken cancellationToken)
        {
            Received.Add(@event);
            throw new InvalidOperationException("Simulated notification failure");
        }
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public int FailSaves { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (FailSaves > 0)
            {
                FailSaves--;
                throw new InvalidOperationException("Simulated database failure");
            }
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class FakeBus : IIntegrationEventBus
    {
        public List<object> Published { get; } = [];
        public Exception? Failure { get; set; }
        public Func<object, bool>? FailWhen { get; set; }
        public bool ThrowCancellation { get; set; }

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            if (ThrowCancellation) cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            if (FailWhen?.Invoke(@event!) == true) throw new InvalidOperationException("selected failure");
            Published.Add(@event!);
            return Task.CompletedTask;
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        private Fixture(SqliteConnection connection, ServiceProvider provider, IServiceScope scope,
            FailingSaveInterceptor failure, FakeBus bus, Subscriber subscriber)
        {
            _connection = connection;
            _provider = provider;
            _scope = scope;
            Failure = failure;
            Bus = bus;
            Subscriber = subscriber;
            Db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            Outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        }

        public TaskManagementDbContext Db { get; }
        public IIntegrationEventOutbox Outbox { get; }
        public FailingSaveInterceptor Failure { get; }
        public FakeBus Bus { get; }
        public Subscriber Subscriber { get; }

        public static async Task<Fixture> CreateAsync(bool useRealBus = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var failure = new FailingSaveInterceptor();
            var bus = new FakeBus();
            var subscriber = new Subscriber();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "Host=unused" }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTaskManagementInfrastructure(configuration);
            // Thay DbContext Postgres bằng SQLite trong bộ nhớ (cùng model, cùng interceptor lỗi giả lập).
            services.AddScoped(_ => new TaskManagementDbContext(new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseSqlite(connection).AddInterceptors(failure).Options));
            if (useRealBus)
            {
                services.AddInProcessEventBus();
                services.AddSingleton<IIntegrationEventHandler<TaskAssignedIntegrationEvent>>(subscriber);
            }
            else
            {
                services.AddSingleton<IIntegrationEventBus>(bus);
            }
            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var fixture = new Fixture(connection, provider, scope, failure, bus, subscriber);
            await fixture.Db.Database.EnsureCreatedAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
