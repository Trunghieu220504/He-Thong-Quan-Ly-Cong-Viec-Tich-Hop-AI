using TaskManagement.Contracts;

namespace RiskAI.Infrastructure;

/// <summary>
/// Dự phòng khi module TaskManagement chưa đăng ký implementation thật của <see cref="ITaskModuleApi"/>:
/// giữ cho API khởi động được (môi trường Development bật ValidateOnBuild) và endpoint risk trả 404.
/// Đăng ký bằng TryAdd, nên bản thật mà TaskManagement đăng ký bằng AddScoped luôn được ưu tiên.
/// </summary>
internal sealed class UnavailableTaskModuleApi : ITaskModuleApi
{
    public Task<TaskSummary?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult<TaskSummary?>(null);

    public Task<TaskStatusCounts> CountByStatusAsync(
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TaskStatusCounts(0, 0, 0, 0));
}
