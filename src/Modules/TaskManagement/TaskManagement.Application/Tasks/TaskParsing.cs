using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks;

/// <summary>Chỉ nhận tên enum (vd. "InProgress"), không nhận số ("2") để khớp contract chuỗi với web.</summary>
internal static class TaskParsing
{
    public static bool TryParseStatus(string? value, out ProjectTaskStatus status) => TryParse(value, out status);

    public static bool TryParsePriority(string? value, out TaskPriority priority) => TryParse(value, out priority);

    private static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value) || char.IsDigit(value.Trim()[0])) return false;
        return Enum.TryParse(value.Trim(), ignoreCase: true, out result) && Enum.IsDefined(result);
    }
}
