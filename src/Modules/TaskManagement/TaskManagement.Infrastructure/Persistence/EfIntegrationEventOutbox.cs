using EventBus;
using Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using SharedKernel;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Persistence;

/// <summary>
/// Outbox dùng chính <see cref="TaskManagementDbContext"/> của request: dòng outbox nằm trong cùng
/// <c>SaveChanges</c> (cùng transaction) với thay đổi của task.
/// <para>
/// <see cref="DispatchAsync"/> gửi tức thì qua <see cref="IIntegrationEventBus"/> rồi đánh dấu đã xử lý.
/// Bus trong tiến trình nuốt lỗi của handler nên "đã gửi" ở đây nghĩa là bus đã chạy xong chứ chưa chắc handler
/// thành công; phát hiện lỗi handler và retry là việc của bộ xử lý nền (PMS-180). Lỗi gửi/đánh dấu sau khi
/// task đã lưu chỉ được ghi log, không làm hỏng request: dòng outbox còn ProcessedAtUtc = null sẽ được gửi lại.
/// </para>
/// </summary>
internal sealed class EfIntegrationEventOutbox(
    TaskManagementDbContext db,
    IIntegrationEventBus eventBus,
    ILogger<EfIntegrationEventOutbox> logger,
    TimeProvider? clock = null) : IIntegrationEventOutbox
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly List<(OutboxMessage Message, Func<CancellationToken, Task> Publish)> _pending = [];

    public void Enqueue<TEvent>(TEvent @event) where TEvent : IIntegrationEvent
    {
        var message = OutboxMessage.Create(@event, _clock.GetUtcNow().UtcDateTime);
        db.OutboxMessages.Add(message);
        _pending.Add((message, ct => eventBus.PublishAsync(@event, ct)));
    }

    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0) return;
        var batch = _pending.ToArray();
        _pending.Clear();

        var delivered = new List<OutboxMessage>(batch.Length);
        foreach (var (message, publish) in batch)
        {
            try
            {
                await publish(cancellationToken);
                delivered.Add(message);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // request bị hủy: giữ dòng chưa xử lý để gửi lại sau
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Gửi tức thì sự kiện outbox {OutboxId} ({EventType}) thất bại; để bộ xử lý nền gửi lại.",
                    message.Id, message.EventType);
            }
        }

        if (delivered.Count == 0) return;
        try
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            foreach (var message in delivered) message.MarkProcessed(now);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Task đã lưu; chưa đánh dấu được thì sự kiện có thể bị gửi lại (an toàn vì subscriber idempotent).
            logger.LogWarning(ex, "Không đánh dấu được {Count} dòng outbox đã gửi; sẽ có thể gửi lại.", delivered.Count);
        }
    }
}
