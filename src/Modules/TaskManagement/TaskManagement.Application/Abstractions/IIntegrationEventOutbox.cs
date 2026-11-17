using SharedKernel;

namespace TaskManagement.Application.Abstractions;

/// <summary>
/// Outbox sự kiện tích hợp của module Task. Thứ tự dùng trong command handler:
/// <list type="number">
/// <item><see cref="Enqueue{TEvent}"/> TRƯỚC khi lưu: sự kiện được ghi vào <c>outbox_messages</c> và được
/// <see cref="ITaskManagementUnitOfWork.SaveChangesAsync"/> lưu CÙNG transaction với thay đổi của task.</item>
/// <item>Sau khi lưu thành công, <see cref="DispatchAsync"/> gửi ngay các sự kiện vừa ghi (giữ độ trễ thấp).
/// Nếu tiến trình chết trước bước này, dòng outbox vẫn còn đó (ProcessedAtUtc = null) để bộ xử lý nền (O2) gửi lại.</item>
/// </list>
/// Lưu task thất bại thì không có dòng outbox nào được ghi, và <see cref="DispatchAsync"/> không được gọi.
/// </summary>
public interface IIntegrationEventOutbox
{
    /// <summary>Thêm sự kiện vào outbox trong transaction hiện tại; chưa ghi xuống DB cho tới lần lưu kế tiếp.</summary>
    void Enqueue<TEvent>(TEvent @event) where TEvent : IIntegrationEvent;

    /// <summary>Gửi các sự kiện đã <see cref="Enqueue{TEvent}"/> trong scope này, sau khi đã lưu thành công.</summary>
    Task DispatchAsync(CancellationToken cancellationToken = default);
}
