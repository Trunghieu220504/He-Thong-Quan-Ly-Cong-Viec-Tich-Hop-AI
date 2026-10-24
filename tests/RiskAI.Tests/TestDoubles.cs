using System.Net;
using System.Text;
using Projects.Contracts;
using RiskAI.Contracts;
using TaskManagement.Contracts;

namespace RiskAI.Tests;

/// <summary>Giả lập ai-service ở tầng HTTP: trả response định sẵn, ném lỗi, hoặc treo để thử timeout.</summary>
internal sealed class StubHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return await send(request, cancellationToken);
    }

    public static StubHttpHandler Json(HttpStatusCode status, string body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));

    public static StubHttpHandler Throws(Exception exception) =>
        new((_, _) => Task.FromException<HttpResponseMessage>(exception));

    public static StubHttpHandler Hangs() =>
        new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Không bao giờ tới đây.");
        });
}

internal sealed class FakeTaskModuleApi : ITaskModuleApi
{
    public Dictionary<Guid, TaskSummary> Tasks { get; } = new();

    public Task<TaskSummary?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TaskSummary?>(Tasks.GetValueOrDefault(taskId));

    public Task<TaskStatusCounts> CountByStatusAsync(
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TaskStatusCounts(0, 0, 0, 0));
}

internal sealed class FakeProjectsModuleApi : IProjectsModuleApi
{
    public Dictionary<(Guid ProjectId, Guid UserId), string> Roles { get; } = new();

    public Task<string?> GetMemberRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(Roles.TryGetValue((projectId, userId), out var role) ? role : null);

    public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Roles.Keys.Any(key => key.ProjectId == projectId));
}

internal sealed class FakeRiskAiClient : IRiskAiClient
{
    public List<RiskAiRequest> Calls { get; } = new();

    public Func<RiskAiRequest, RiskAiResult> Respond { get; set; } = request => new RiskAiResult(request.TaskId, "low");

    public Task<RiskAiResult> PredictAsync(RiskAiRequest request, CancellationToken cancellationToken = default)
    {
        Calls.Add(request);
        return Task.FromResult(Respond(request));
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
