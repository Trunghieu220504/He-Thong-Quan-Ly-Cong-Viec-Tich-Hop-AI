using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RiskAI.Contracts;
using RiskAI.Infrastructure;

namespace RiskAI.Tests;

public class HttpRiskAiClientTests
{
    private static readonly RiskAiRequest Request = new(
        TaskId: Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
        EstimatedHours: null,
        DaysUntilDue: 3,
        ProgressPercent: 50);

    private static HttpRiskAiClient CreateClient(StubHttpHandler handler, TimeSpan? timeout = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://ai-service.test/") };
        if (timeout is { } value)
            http.Timeout = value;

        return new HttpRiskAiClient(http, NullLogger<HttpRiskAiClient>.Instance);
    }

    [Theory]
    [InlineData("low", "low")]
    [InlineData("medium", "medium")]
    [InlineData("high", "high")]
    [InlineData("HIGH", "high")]
    [InlineData(" Medium ", "medium")]
    public async Task Muc_rui_ro_hop_le_duoc_chuan_hoa_va_tra_ve(string riskFromAi, string expected)
    {
        var client = CreateClient(StubHttpHandler.Json(HttpStatusCode.OK, $$"""{"risk":"{{riskFromAi}}"}"""));

        var result = await client.PredictAsync(Request);

        Assert.Equal(expected, result.Risk);
        Assert.Equal(Request.TaskId, result.TaskId);
    }

    [Fact]
    public async Task Goi_POST_predict_delay_voi_body_snake_case()
    {
        var handler = StubHttpHandler.Json(HttpStatusCode.OK, """{"risk":"low"}""");

        await CreateClient(handler).PredictAsync(Request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("http://ai-service.test/predict-delay", handler.LastRequest.RequestUri!.ToString());

        using var json = JsonDocument.Parse(handler.LastRequestBody!);
        var body = json.RootElement;
        Assert.Equal(
            new[] { "days_until_due", "estimated_hours", "progress_percent", "task_id" },
            body.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal("3fa85f64-5717-4562-b3fc-2c963f66afa6", body.GetProperty("task_id").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("estimated_hours").ValueKind);
        Assert.Equal(3, body.GetProperty("days_until_due").GetInt32()); // schema Python là int: gửi 2.5 sẽ bị 422
        Assert.Equal(50.0, body.GetProperty("progress_percent").GetDouble());
    }

    [Theory]
    [InlineData("""{"risk":"critical"}""")]
    [InlineData("""{"risk":"unknown"}""")]
    [InlineData("""{"risk":""}""")]
    [InlineData("""{"risk":null}""")]
    [InlineData("""{"risk":1}""")]
    [InlineData("""{}""")]
    [InlineData("""null""")]
    [InlineData("""khong-phai-json""")]
    public async Task Gia_tri_la_tu_AI_thanh_loi_co_kiem_soat(string body)
    {
        var client = CreateClient(StubHttpHandler.Json(HttpStatusCode.OK, body));

        await Assert.ThrowsAsync<RiskAiUnavailableException>(() => client.PredictAsync(Request));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(422)]
    [InlineData(404)]
    public async Task Ai_tra_ma_loi_HTTP_thi_loi_co_kiem_soat(int statusCode)
    {
        var client = CreateClient(StubHttpHandler.Json((HttpStatusCode)statusCode, """{"detail":"boom"}"""));

        var exception = await Assert.ThrowsAsync<RiskAiUnavailableException>(() => client.PredictAsync(Request));

        Assert.Contains(statusCode.ToString(), exception.Message);
    }

    [Fact]
    public async Task Ai_tat_thi_loi_co_kiem_soat()
    {
        var client = CreateClient(StubHttpHandler.Throws(new HttpRequestException("Connection refused")));

        var exception = await Assert.ThrowsAsync<RiskAiUnavailableException>(() => client.PredictAsync(Request));

        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task Ai_qua_timeout_thi_loi_co_kiem_soat()
    {
        var client = CreateClient(StubHttpHandler.Hangs(), timeout: TimeSpan.FromMilliseconds(200));

        var exception = await Assert.ThrowsAsync<RiskAiUnavailableException>(() => client.PredictAsync(Request));

        Assert.IsType<TaskCanceledException>(exception.InnerException);
    }

    [Fact]
    public async Task Nguoi_goi_huy_request_thi_khong_bao_thanh_loi_AI()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var client = CreateClient(StubHttpHandler.Hangs());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.PredictAsync(Request, cancellation.Token));
    }
}
