using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks;

/// <summary>Chỉ nhận tên enum (vd. "InProgress"), không nhận số ("2") để khớp contract chuỗi với web.</summary>
internal static class TaskParsing
{
    public static bool TryParseStatus(string? value, out ProjectTaskStatus status) => TryParse(value, out status);

    public static bool TryParsePriority(string? value, out TaskPriority priority) => TryParse(value, out priority);

    // So khớp với tên enum thay vì Enum.TryParse, vì TryParse nhận cả "Low,High" (gộp cờ) và số.
    private static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        var name = value?.Trim();
        if (string.IsNullOrEmpty(name)) return false;

        var match = Enum.GetNames<TEnum>().FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        return match is not null && Enum.TryParse(match, out result);
    }
}
