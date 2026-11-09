using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using TaskManagement.Contracts;
using TaskManagement.Contracts.Events;

namespace EventBus.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInProcessEventBus_DangKyBusTrongProcess_VaResolveDuocTrongScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInProcessEventBus();
        using var provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();

        Assert.IsType<InProcessIntegrationEventBus>(bus);
    }

    [Fact]
    public void AddInProcessEventBus_KhongGhiDeBanCaiDatDaDangKyTruoc()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IIntegrationEventBus, FakeBus>();
        services.AddInProcessEventBus();
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();

        Assert.IsType<FakeBus>(scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>());
    }

    [Fact]
    public async Task BusTuDI_PhatSuKienTask_ToiHandlerDaDangKy()
    {
        var received = new List<TaskAssignedIntegrationEvent>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInProcessEventBus();
        services.AddScoped<IIntegrationEventHandler<TaskAssignedIntegrationEvent>>(
            _ => new CollectingHandler(received));
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
        var evt = new TaskAssignedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "Viết E1", Guid.NewGuid(), null, Guid.NewGuid());

        await bus.PublishAsync(evt);

        Assert.Equal([evt], received);
    }

    private sealed class FakeBus : IIntegrationEventBus
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent => Task.CompletedTask;
    }

    private sealed class CollectingHandler : IIntegrationEventHandler<TaskAssignedIntegrationEvent>
    {
        private readonly List<TaskAssignedIntegrationEvent> _received;

        public CollectingHandler(List<TaskAssignedIntegrationEvent> received) => _received = received;

        public Task Handle(TaskAssignedIntegrationEvent @event, CancellationToken cancellationToken)
        {
            _received.Add(@event);
            return Task.CompletedTask;
        }
    }
}

public class TaskEventContractTests
{
    [Fact]
    public void SuKienTask_LaIIntegrationEvent_CuaSharedKernel()
    {
        Assert.True(typeof(IIntegrationEvent).IsAssignableFrom(typeof(TaskAssignedIntegrationEvent)));
        Assert.True(typeof(IIntegrationEvent).IsAssignableFrom(typeof(TaskStatusChangedIntegrationEvent)));
        Assert.Equal("SharedKernel", typeof(IIntegrationEvent).Namespace);
    }

    [Fact]
    public void TaskManagementContracts_KhongThamChieuAssemblyEventBus()
    {
        var referenced = typeof(TaskAssignedIntegrationEvent).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name);

        Assert.DoesNotContain("EventBus", referenced);
    }

    [Fact]
    public void TaskStatusChangedEvent_MangDuTruongTheoContract()
    {
        var evt = new TaskStatusChangedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(),
            "T", ProjectTaskStatus.Todo, ProjectTaskStatus.Done, null, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ProjectTaskStatus.Todo, evt.OldStatus);
        Assert.Equal(ProjectTaskStatus.Done, evt.NewStatus);
        Assert.Null(evt.AssigneeId);
    }
}
