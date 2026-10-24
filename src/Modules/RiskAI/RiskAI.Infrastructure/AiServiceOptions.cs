namespace RiskAI.Infrastructure;

/// <summary>
/// Cấu hình section "AiService". Mặc định trỏ vào ai-service chạy local;
/// môi trường khác ghi đè bằng biến môi trường AiService__BaseUrl, không hard-code URL production.
/// </summary>
public sealed class AiServiceOptions
{
    public const string SectionName = "AiService";

    public string BaseUrl { get; set; } = "http://localhost:8000";

    public int TimeoutSeconds { get; set; } = 5;
}
