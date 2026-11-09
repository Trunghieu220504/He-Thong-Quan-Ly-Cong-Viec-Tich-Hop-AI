using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventBus.Tests;

public class InProcessIntegrationEventBusTests
{
    private static (InProcessIntegrationEventBus Bus, ListLogger<InProcessIntegrationEventBus> Logger) CreateBus(
        Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        var provider = services.BuildServiceProvider();
        var logger = new ListLogger<InProcessIntegrationEventBus>();
        return (new InProcessIntegrationEventBus(provider, logger), logger);
    }

    private static SampleEvent NewEvent() => new(Guid.NewGuid(), "test");

    [Fact]
    public async Task Publish_GoiMoiHandlerDaDangKy_TheoThuTuDangKy()
    {
        var calls = new List<string>();
        var (bus, _) = CreateBus(s =>
        {
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("A", calls));
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("B", calls));
        });

        await bus.PublishAsync(NewEvent());

        Assert.Equal(["A", "B"], calls);
    }

    [Fact]
    public async Task Publish_KhongCoHandler_KhongNemLoi()
    {
        var (bus, logger) = CreateBus(_ => { });

        await bus.PublishAsync(NewEvent());

        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Publish_ChiGoiHandlerCuaDungKieuSuKien()
    {
        var calls = new List<string>();
        var (bus, _) = CreateBus(s =>
        {
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("sample", calls));
            s.AddSingleton<IIntegrationEventHandler<OtherEvent>>(new RecordingHandler<OtherEvent>("other", calls));
        });

        await bus.PublishAsync(NewEvent());

        Assert.Equal(["sample"], calls);
    }

    [Fact]
    public async Task Publish_HandlerLoi_KhongLamHongBenPhat_VaHandlerKhacVanChay()
    {
        var calls = new List<string>();
        var (bus, logger) = CreateBus(s =>
        {
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("A", calls));
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(
                new ThrowingHandler<SampleEvent>(new InvalidOperationException("boom"), calls));
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("C", calls));
        });

        // Bên phát không được nhận ngoại lệ (Q3).
        await bus.PublishAsync(NewEvent());

        Assert.Equal(["A", "throwing", "C"], calls);
        var error = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, error.Level);
        Assert.IsType<InvalidOperationException>(error.Exception);
        Assert.Contains(nameof(SampleEvent), error.Message);
    }

    [Fact]
    public async Task Publish_HandlerNemOperationCanceled_NhungTokenChuaHuy_DuocXemLaLoiThuong()
    {
        // Ví dụ timeout nội bộ của handler: không phải do bên phát hủy nên không được lan ra ngoài.
        var calls = new List<string>();
        var (bus, logger) = CreateBus(s =>
        {
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(
                new ThrowingHandler<SampleEvent>(new OperationCanceledException(), calls));
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("after", calls));
        });

        await bus.PublishAsync(NewEvent());

        Assert.Equal(["throwing", "after"], calls);
        Assert.Single(logger.Entries);
    }

    [Fact]
    public async Task Publish_TokenDaBiHuy_NemOperationCanceled_VaKhongGoiHandler()
    {
        var calls = new List<string>();
        var (bus, _) = CreateBus(s =>
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("A", calls)));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.PublishAsync(NewEvent(), cts.Token));

        Assert.Empty(calls);
    }

    [Fact]
    public async Task Publish_TokenBiHuyTrongLucHandlerChay_NemTiepVaKhongGoiHandlerSau()
    {
        var calls = new List<string>();
        using var cts = new CancellationTokenSource();
        var (bus, logger) = CreateBus(s =>
        {
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(
                new CancellingHandler(cts, calls));
            s.AddSingleton<IIntegrationEventHandler<SampleEvent>>(new RecordingHandler<SampleEvent>("after", calls));
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bus.PublishAsync(NewEvent(), cts.Token));

        Assert.Equal(["cancelling"], calls);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Publish_SuKienNull_NemArgumentNullException()
    {
        var (bus, _) = CreateBus(_ => { });

        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync<SampleEvent>(null!));
    }

    private sealed class CancellingHandler : IIntegrationEventHandler<SampleEvent>
    {
        private readonly CancellationTokenSource _cts;
        private readonly List<string> _calls;

        public CancellingHandler(CancellationTokenSource cts, List<string> calls)
        {
            _cts = cts;
            _calls = calls;
        }

        public async Task Handle(SampleEvent @event, CancellationToken cancellationToken)
        {
            _calls.Add("cancelling");
            await _cts.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
