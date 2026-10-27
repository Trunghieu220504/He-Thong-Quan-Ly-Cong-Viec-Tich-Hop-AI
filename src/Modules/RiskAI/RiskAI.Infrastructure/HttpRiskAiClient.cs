using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RiskAI.Contracts;

namespace RiskAI.Infrastructure;

/// <summary>
/// Gọi POST /predict-delay của ai-service. Mọi lỗi phía AI (tắt, timeout, mã lỗi HTTP, JSON sai,
/// mức rủi ro lạ) đều chuyển thành <see cref="RiskAiUnavailableException"/> để không văng 500.
/// </summary>
internal sealed class HttpRiskAiClient(HttpClient http, ILogger<HttpRiskAiClient> logger) : IRiskAiClient
{
    // ai-service (FastAPI/Pydantic) dùng snake_case: task_id, estimated_hours, days_until_due, progress_percent
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private static readonly HashSet<string> AllowedRisks = ["low", "medium", "high"];

    public async Task<RiskAiResult> PredictAsync(RiskAiRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("predict-delay", request, Json, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "ai-service trả mã {StatusCode} cho task {TaskId}", (int)response.StatusCode, request.TaskId);
                throw new RiskAiUnavailableException($"AI service trả mã lỗi {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<PredictDelayResponse>(Json, cancellationToken);
            var risk = body?.Risk?.Trim().ToLowerInvariant();

            if (risk is null || !AllowedRisks.Contains(risk))
            {
                logger.LogWarning(
                    "ai-service trả mức rủi ro không hợp lệ '{Risk}' cho task {TaskId}", body?.Risk, request.TaskId);
                throw new RiskAiUnavailableException("AI service trả mức rủi ro không hợp lệ.");
            }

            return new RiskAiResult(request.TaskId, risk);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Không kết nối được ai-service");
            throw new RiskAiUnavailableException("Không kết nối được AI service.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout hết hạn (người gọi không huỷ) → coi là AI không phản hồi
            logger.LogWarning(ex, "ai-service quá thời gian phản hồi");
            throw new RiskAiUnavailableException("AI service quá thời gian phản hồi.", ex);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "ai-service trả JSON không hợp lệ");
            throw new RiskAiUnavailableException("AI service trả dữ liệu không hợp lệ.", ex);
        }
    }

    private sealed record PredictDelayResponse(string? Risk);
}
