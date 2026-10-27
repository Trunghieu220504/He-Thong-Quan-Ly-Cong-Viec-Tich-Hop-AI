namespace RiskAI.Contracts;

/// <summary>
/// Client gọi ai-service (FastAPI, POST /predict-delay) để dự đoán rủi ro trễ hạn của một task.
/// AI chỉ trả lời, không bao giờ tự sửa task hay dữ liệu nghiệp vụ.
/// </summary>
public interface IRiskAiClient
{
    /// <summary>Gửi đặc trưng của task và nhận mức rủi ro "low" | "medium" | "high".</summary>
    /// <exception cref="RiskAiUnavailableException">
    /// ai-service tắt, quá thời gian, trả mã lỗi HTTP hoặc trả dữ liệu không hợp lệ.
    /// </exception>
    Task<RiskAiResult> PredictAsync(RiskAiRequest request, CancellationToken cancellationToken = default);
}
