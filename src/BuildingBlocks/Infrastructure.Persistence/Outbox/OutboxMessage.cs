using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel;

namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// Một dòng của bảng <c>outbox_messages</c>: sự kiện tích hợp được ghi CÙNG transaction với thay đổi nghiệp vụ,
/// nên "đã lưu task" và "đã ghi nhận sự kiện" thành công hoặc thất bại cùng nhau.
/// <para>
/// <see cref="ProcessedAtUtc"/> = null nghĩa là chưa chắc đã gửi tới subscriber. Giao hàng là at-least-once:
/// bên nhận phải idempotent theo EventId (Notifications đã dùng unique index EventId + UserId).
/// </para>
/// </summary>
public sealed class OutboxMessage
{
    public const int EventTypeMaxLength = 300;

    /// <summary>Cấu hình JSON dùng chung để ghi và đọc lại payload (enum dạng chuỗi cho dễ đọc khi gỡ lỗi).</summary>
    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    private OutboxMessage() { }

    public Guid Id { get; private set; }

    /// <summary>Tên đầy đủ (namespace + tên) của kiểu sự kiện, dùng để dựng lại kiểu khi gửi lại.</summary>
    public string EventType { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public static OutboxMessage Create<TEvent>(TEvent @event, DateTime occurredAtUtc) where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        var type = @event.GetType();
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = type.FullName ?? type.Name,
            // Serialize theo kiểu thật của đối tượng, không theo TEvent (TEvent có thể là interface).
            Payload = JsonSerializer.Serialize(@event, type, JsonOptions),
            OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc)
        };
    }

    public void MarkProcessed(DateTime processedAtUtc) =>
        ProcessedAtUtc = DateTime.SpecifyKind(processedAtUtc, DateTimeKind.Utc);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
