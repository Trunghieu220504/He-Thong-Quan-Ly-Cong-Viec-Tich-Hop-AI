namespace RiskAI.Contracts;

/// <summary>
/// ai-service không dùng được: tắt, quá thời gian, trả mã lỗi hoặc trả dữ liệu không hợp lệ.
/// Tầng Application chuyển lỗi này thành Result.Failure, endpoint trả 503 thay vì 500.
/// </summary>
public sealed class RiskAiUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
