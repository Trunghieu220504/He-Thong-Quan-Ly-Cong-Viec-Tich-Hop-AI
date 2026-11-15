using EventBus;
using Identity.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Projects.Contracts;
using SharedKernel;
using TaskManagement.Application;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Tasks.AssignTask;
using TaskManagement.Application.Tasks.ChangeTaskStatus;
using TaskManagement.Application.Tasks.CreateTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tests;

internal sealed class TaskEventFixture : IDisposable
{
    public Guid ProjectId { get; } = Guid.NewGuid();
    public Guid ActorId { get; } = Guid.NewGuid();
    public Guid CreatorId { get; } = Guid.NewGuid();
    public Guid MemberId { get; } = Guid.NewGuid();
    public ProjectTask Task { get; }
    public FakeTaskRepository Repository { get; } = new();
    public FakeTaskUnitOfWork UnitOfWork { get; } = new();
    public RecordingEventBus Bus { get; }
    public FakeProjectsApi Projects { get; } = new();
    private readonly ServiceProvider _services;

    public TaskEventFixture(bool assigned = true)
    {
        Task = ProjectTask.Create(ProjectId, "Event task", null, TaskPriority.Medium,
            assigned ? MemberId : null, null, CreatorId).Value!;
        Repository.Tasks.Add(Task);
        Projects.Roles[(ProjectId, ActorId)] = "Owner";
        Projects.Roles[(ProjectId, MemberId)] = "Member";
        Bus = new RecordingEventBus(UnitOfWork);
        var services = new ServiceCollection();
        services.AddTaskManagementApplication();
        services.AddSingleton<ITaskRepository>(Repository);
        services.AddSingleton<ITaskManagementUnitOfWork>(UnitOfWork);
        services.AddSingleton<IProjectsModuleApi>(Projects);
        services.AddSingleton<IIdentityModuleApi, FakeIdentityApi>();
        services.AddSingleton<IIntegrationEventBus>(Bus);
        _services = services.BuildServiceProvider();
    }

    public Task<Result<TaskDto>> RunAsync(string operation, CancellationToken ct = default) => operation switch
    {
        "assign" => AssignAsync(null, ct),
        "status" => StatusAsync("InProgress", ct),
        "create" => CreateAsync(MemberId, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    public Task<Result<TaskDto>> AssignAsync(Guid? assignee, CancellationToken ct = default) =>
        ActivatorUtilities.CreateInstance<AssignTaskCommandHandler>(_services)
            .Handle(new AssignTaskCommand(Task.Id, ActorId, assignee), ct);

    public Task<Result<TaskDto>> StatusAsync(string status, CancellationToken ct = default) =>
        ActivatorUtilities.CreateInstance<ChangeTaskStatusCommandHandler>(_services)
            .Handle(new ChangeTaskStatusCommand(Task.Id, ActorId, status), ct);

    public Task<Result<TaskDto>> CreateAsync(Guid? assignee, CancellationToken ct = default) =>
        ActivatorUtilities.CreateInstance<CreateTaskCommandHandler>(_services)
            .Handle(new CreateTaskCommand(ProjectId, ActorId, "Created assigned task", null,
                "Medium", assignee, null), ct);

    public void Dispose() => _services.Dispose();
}

internal sealed class FakeTaskRepository : ITaskRepository
{
    public List<ProjectTask> Tasks { get; } = [];
    public Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tasks.SingleOrDefault(t => t.Id == id));
    public void Add(ProjectTask task) => Tasks.Add(task);
    public Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> GetPageByProjectAsync(
        Guid projectId, ProjectTaskStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class FakeTaskUnitOfWork : ITaskManagementUnitOfWork
{
    public int SuccessfulSaves { get; private set; }
    public Exception? Failure { get; set; }
    public List<string> Operations { get; } = [];
    public Func<Task>? BeforeSaveCompletes { get; set; }
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null) throw Failure;
        if (BeforeSaveCompletes is not null) await BeforeSaveCompletes();
        SuccessfulSaves++;
        Operations.Add("save");
        return 1;
    }
}

internal sealed class RecordingEventBus(FakeTaskUnitOfWork unitOfWork) : IIntegrationEventBus
{
    public List<IIntegrationEvent> Events { get; } = [];
    public CancellationToken LastToken { get; private set; }
    public IIntegrationEventBus? ForwardTo { get; set; }
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        Assert.True(unitOfWork.SuccessfulSaves > 0, "Publishing before successful save is forbidden.");
        LastToken = cancellationToken;
        unitOfWork.Operations.Add("publish");
        Events.Add(@event);
        if (ForwardTo is not null) await ForwardTo.PublishAsync(@event, cancellationToken);
    }
}

internal sealed class FakeProjectsApi : IProjectsModuleApi
{
    public Dictionary<(Guid, Guid), string> Roles { get; } = [];
    public Task<string?> GetMemberRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Roles.GetValueOrDefault((projectId, userId)));
    public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}

internal sealed class FakeIdentityApi : IIdentityModuleApi
{
    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UserSummary?>(new UserSummary(userId, "member@example.test", "Member"));
}
