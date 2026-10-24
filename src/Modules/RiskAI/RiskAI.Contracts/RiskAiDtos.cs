namespace RiskAI.Contracts;

/// <summary>
/// Đặc trưng gửi sang ai-service. Khi gửi được serialize snake_case:
/// task_id, estimated_hours, days_until_due, progress_percent (khớp ai-service/app/schemas.py).
/// </summary>
/// <param name="DaysUntilDue">Số ngày còn lại tới hạn, là số nguyên (schema Python là int); âm khi đã quá hạn.</param>
/// <param name="ProgressPercent">0–100.</param>
public sealed record RiskAiRequest(
    Guid TaskId,
    double? EstimatedHours,
    int? DaysUntilDue,
    double? ProgressPercent);

/// <summary>Kết quả dự đoán; Risk luôn là "low" | "medium" | "high" (chữ thường).</summary>
public sealed record RiskAiResult(Guid TaskId, string Risk);
