using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace EventBus;

/// <summary>
/// Bus sự kiện chạy trong cùng tiến trình: gọi lần lượt mọi <see cref="IIntegrationEventHandler{TEvent}"/>
/// đã đăng ký cho đúng kiểu sự kiện.
/// <para>
/// Handler chỉ là phần phụ trợ (ví dụ tạo thông báo) nên một handler ném lỗi sẽ được ghi log và bỏ qua;
/// các handler còn lại vẫn chạy và <see cref="PublishAsync{TEvent}"/> không ném lỗi cho bên phát.
/// Ngoại lệ duy nhất: khi <c>cancellationToken</c> bị hủy thì <see cref="OperationCanceledException"/> được ném tiếp.
/// </para>
/// <para>
/// Lưu ý: bus này không bền vững (tiến trình chết giữa lúc lưu và publish thì mất sự kiện); Outbox là việc Should O1.
/// Đăng ký Scoped để handler dùng chung scope (DbContext, v.v.) với bên phát.
/// </para>
/// </summary>
public sealed class InProcessIntegrationEventBus : IIntegrationEventBus
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InProcessIntegrationEventBus> _logger;

    public InProcessIntegrationEventBus(IServiceProvider services, ILogger<InProcessIntegrationEventBus> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Chỉ gọi handler đăng ký cho đúng kiểu TEvent; ToArray để không bị ảnh hưởng nếu collection đổi trong lúc chạy.
        var handlers = _services.GetServices<IIntegrationEventHandler<TEvent>>().ToArray();

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await handler.Handle(@event, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Q3: lỗi của handler không được làm hỏng thao tác của bên phát.
                _logger.LogError(ex,
                    "Handler {Handler} lỗi khi xử lý sự kiện {EventType}; bỏ qua và chạy tiếp handler khác.",
                    handler.GetType().FullName, typeof(TEvent).Name);
            }
        }
    }
}
