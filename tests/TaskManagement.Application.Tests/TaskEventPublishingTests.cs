using EventBus;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using TaskManagement.Contracts.Events;
using TaskManagement.Domain.Enums;
using ContractStatus = TaskManagement.Contracts.ProjectTaskStatus;

namespace TaskManagement.Application.Tests;

public sealed class TaskEventPublishingTests
{
    [Theory]
    [InlineData("assign")]
    [InlineData("reassign")]
    [InlineData("unassign")]
    public async Task Assignment_enqueues_correct_payload_before_save_and_publishes_after(string change)
    {
        using var f = new TaskEventFixture(assigned: change != "assign");
        var previous = f.Task.AssigneeId;
        Guid? next = change == "unassign" ? null : Guid.NewGuid();
        if (next is { } userId) f.Projects.Roles[(f.ProjectId, userId)] = "Member";
        var before = DateTime.UtcNow;

        var result = await f.AssignAsync(next);

        Assert.True(result.IsSuccess);
        var e = Assert.IsType<TaskAssignedIntegrationEvent>(Assert.Single(f.Bus.Events));
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(DateTimeKind.Utc, e.OccurredAtUtc.Kind);
        Assert.InRange(e.OccurredAtUtc, before, DateTime.UtcNow);
        Assert.Equal(f.Task.Id, e.TaskId);
        Assert.Equal(f.ProjectId, e.ProjectId);
        Assert.Equal(f.Task.Title, e.TaskTitle);
        Assert.Equal(previous, e.PreviousAssigneeId);
        Assert.Equal(next, e.AssigneeId);
        Assert.Equal(f.ActorId, e.ChangedBy);
        Assert.Equal(next, result.Value!.AssigneeId);
        Assert.Equal(new[] { "enqueue", "save", "publish" }, f.UnitOfWork.Operations);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Todo, ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Todo, ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.InProgress, ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress, ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Done, ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.Done, ProjectTaskStatus.InProgress)]
    public async Task Status_change_enqueues_old_and_new_contract_status_before_save_and_publishes_after(
        ProjectTaskStatus oldStatus, ProjectTaskStatus newStatus)
    {
        using var f = new TaskEventFixture();
        f.Task.ChangeStatus(oldStatus);
        var before = DateTime.UtcNow;

        var result = await f.StatusAsync(newStatus.ToString());

        Assert.True(result.IsSuccess);
        var e = Assert.IsType<TaskStatusChangedIntegrationEvent>(Assert.Single(f.Bus.Events));
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(DateTimeKind.Utc, e.OccurredAtUtc.Kind);
        Assert.InRange(e.OccurredAtUtc, before, DateTime.UtcNow);
        Assert.Equal(f.Task.Id, e.TaskId);
        Assert.Equal(f.ProjectId, e.ProjectId);
        Assert.Equal(f.Task.Title, e.TaskTitle);
        Assert.Equal(Enum.Parse<ContractStatus>(oldStatus.ToString()), e.OldStatus);
        Assert.Equal(Enum.Parse<ContractStatus>(newStatus.ToString()), e.NewStatus);
        Assert.Equal(f.MemberId, e.AssigneeId);
        Assert.Equal(f.CreatorId, e.CreatedBy);
        Assert.Equal(f.ActorId, e.ChangedBy);
        Assert.Equal(newStatus.ToString(), result.Value!.Status);
        Assert.Equal(new[] { "enqueue", "save", "publish" }, f.UnitOfWork.Operations);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Unchanged_assignment_does_not_publish(bool assigned)
    {
        using var f = new TaskEventFixture(assigned);
        var result = await f.AssignAsync(f.Task.AssigneeId);
        Assert.True(result.IsSuccess);
        Assert.Empty(f.Bus.Events);
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Done)]
    public async Task Unchanged_status_including_normalized_input_does_not_publish(ProjectTaskStatus status)
    {
        using var f = new TaskEventFixture();
        f.Task.ChangeStatus(status);
        var result = await f.StatusAsync(" " + status.ToString().ToLowerInvariant() + " ");
        Assert.True(result.IsSuccess);
        Assert.Empty(f.Bus.Events);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    public async Task Repeating_a_successful_patch_does_not_publish_again(string operation)
    {
        using var f = new TaskEventFixture();
        Assert.True((await f.RunAsync(operation)).IsSuccess);
        Assert.True((await f.RunAsync(operation)).IsSuccess);
        Assert.Single(f.Bus.Events);
    }

    [Fact]
    public async Task Creating_assigned_task_enqueues_initial_assignment_before_save_and_publishes_after()
    {
        using var f = new TaskEventFixture();
        var result = await f.CreateAsync(f.MemberId);
        Assert.True(result.IsSuccess);
        var e = Assert.IsType<TaskAssignedIntegrationEvent>(Assert.Single(f.Bus.Events));
        Assert.Equal(result.Value!.Id, e.TaskId);
        Assert.Equal(result.Value.Title, e.TaskTitle);
        Assert.Equal(f.ProjectId, e.ProjectId);
        Assert.Equal(f.MemberId, e.AssigneeId);
        Assert.Null(e.PreviousAssigneeId);
        Assert.Equal(f.ActorId, e.ChangedBy);
        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(DateTimeKind.Utc, e.OccurredAtUtc.Kind);
        Assert.Equal(new[] { "enqueue", "save", "publish" }, f.UnitOfWork.Operations);
    }

    [Fact]
    public async Task Creating_unassigned_task_does_not_publish()
    {
        using var f = new TaskEventFixture();
        Assert.True((await f.CreateAsync(null)).IsSuccess);
        Assert.Equal(1, f.UnitOfWork.SuccessfulSaves);
        Assert.Empty(f.Bus.Events);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    [InlineData("create")]
    public async Task Save_exception_prevents_publication(string operation)
    {
        using var f = new TaskEventFixture();
        var error = new InvalidOperationException("Persistence failed");
        f.UnitOfWork.Failure = error;
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => f.RunAsync(operation)));
        Assert.Empty(f.Bus.Events);
        Assert.Equal(0, f.UnitOfWork.SuccessfulSaves);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    [InlineData("create")]
    public async Task Pending_save_does_not_publish_until_it_completes(string operation)
    {
        using var f = new TaskEventFixture();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.UnitOfWork.BeforeSaveCompletes = () => { entered.SetResult(); return release.Task; };
        var handling = f.RunAsync(operation);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(handling.IsCompleted);
            Assert.Empty(f.Bus.Events);
        }
        finally { release.SetResult(); }
        Assert.True((await handling).IsSuccess);
        Assert.Single(f.Bus.Events);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    [InlineData("create")]
    public async Task Cancelled_save_does_not_publish(string operation)
    {
        using var f = new TaskEventFixture();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.RunAsync(operation, cancelled.Token));
        Assert.Empty(f.Bus.Events);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    [InlineData("create")]
    public async Task Publication_receives_request_cancellation_token(string operation)
    {
        using var f = new TaskEventFixture();
        using var request = new CancellationTokenSource();
        Assert.True((await f.RunAsync(operation, request.Token)).IsSuccess);
        Assert.Single(f.Bus.Events);
        Assert.Equal(request.Token, f.Bus.LastToken);
    }

    [Theory]
    [InlineData("assign", "Viewer")]
    [InlineData("status", "Viewer")]
    [InlineData("create", "Viewer")]
    [InlineData("assign", null)]
    [InlineData("status", null)]
    [InlineData("create", null)]
    public async Task Denied_access_neither_saves_nor_publishes(string operation, string? role)
    {
        using var f = new TaskEventFixture();
        f.Projects.Roles.Remove((f.ProjectId, f.ActorId));
        if (role is not null) f.Projects.Roles[(f.ProjectId, f.ActorId)] = role;
        var result = await f.RunAsync(operation);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(0, f.UnitOfWork.SuccessfulSaves);
        Assert.Empty(f.Bus.Events);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    public async Task Missing_task_neither_saves_nor_publishes(string operation)
    {
        using var f = new TaskEventFixture();
        f.Repository.Tasks.Clear();
        var result = await f.RunAsync(operation);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(0, f.UnitOfWork.SuccessfulSaves);
        Assert.Empty(f.Bus.Events);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    [InlineData("create")]
    public async Task Invalid_request_neither_saves_nor_publishes(string operation)
    {
        using var f = new TaskEventFixture();
        var result = operation switch
        {
            "assign" => await f.AssignAsync(Guid.Empty),
            "status" => await f.StatusAsync("Todo,Done"),
            _ => await f.CreateAsync(Guid.Empty)
        };
        Assert.True(result.IsFailure);
        Assert.Equal(0, f.UnitOfWork.SuccessfulSaves);
        Assert.Empty(f.Bus.Events);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("create")]
    public async Task Assignee_outside_project_neither_saves_nor_publishes(string operation)
    {
        using var f = new TaskEventFixture();
        var outsider = Guid.NewGuid();
        var result = operation == "assign" ? await f.AssignAsync(outsider) : await f.CreateAsync(outsider);
        Assert.True(result.IsFailure);
        Assert.Equal(0, f.UnitOfWork.SuccessfulSaves);
        Assert.Empty(f.Bus.Events);
    }

    [Fact]
    public async Task Distinct_changes_have_distinct_event_ids()
    {
        using var f = new TaskEventFixture();
        await f.StatusAsync("InProgress");
        await f.StatusAsync("Done");
        Assert.Equal(2, f.Bus.Events.Count);
        Assert.NotEqual(Assert.IsType<TaskStatusChangedIntegrationEvent>(f.Bus.Events[0]).EventId,
            Assert.IsType<TaskStatusChangedIntegrationEvent>(f.Bus.Events[1]).EventId);
    }

    [Theory]
    [InlineData("assign")]
    [InlineData("status")]
    [InlineData("create")]
    public async Task Real_bus_delivers_after_save_and_subscriber_failure_does_not_fail_task(string operation)
    {
        using var f = new TaskEventFixture();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInProcessEventBus();
        var subscriber = new FailingSubscriber(f.UnitOfWork);
        services.AddSingleton<IIntegrationEventHandler<TaskAssignedIntegrationEvent>>(subscriber);
        services.AddSingleton<IIntegrationEventHandler<TaskStatusChangedIntegrationEvent>>(subscriber);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        f.Bus.ForwardTo = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();

        var result = await f.RunAsync(operation);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, subscriber.Calls);
        Assert.Equal(1, subscriber.SuccessfulSavesWhenCalled);
        Assert.Single(f.Bus.Events);
    }

    private sealed class FailingSubscriber(FakeTaskUnitOfWork unitOfWork)
        : IIntegrationEventHandler<TaskAssignedIntegrationEvent>, IIntegrationEventHandler<TaskStatusChangedIntegrationEvent>
    {
        public int Calls { get; private set; }
        public int SuccessfulSavesWhenCalled { get; private set; }
        public Task Handle(TaskAssignedIntegrationEvent e, CancellationToken cancellationToken) => Fail();
        public Task Handle(TaskStatusChangedIntegrationEvent e, CancellationToken cancellationToken) => Fail();
        private Task Fail()
        {
            Calls++;
            SuccessfulSavesWhenCalled = unitOfWork.SuccessfulSaves;
            throw new InvalidOperationException("Simulated notification failure");
        }
    }
}
