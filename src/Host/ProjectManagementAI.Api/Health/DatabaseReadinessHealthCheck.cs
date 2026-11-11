using Identity.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProjectManagementAI.Api.Health;

/// <summary>
/// Kiểm tra API đã sẵn sàng nhận request chưa: mở được kết nối tới PostgreSQL.
/// Các module dùng chung một cơ sở dữ liệu (mỗi module một schema) nên chỉ cần thử một DbContext.
/// Đăng ký với tag <see cref="ReadyTag"/> để chỉ <c>/health/ready</c> chạy check này, còn <c>/health</c> chỉ báo tiến trình còn sống.
/// </summary>
public sealed class DatabaseReadinessHealthCheck : IHealthCheck
{
    public const string Name = "database";
    public const string ReadyTag = "ready";

    private readonly IdentityDbContext _db;

    public DatabaseReadinessHealthCheck(IdentityDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // CanConnectAsync trả false khi không kết nối được (không ném lỗi kết nối).
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Kết nối PostgreSQL thành công.")
                : HealthCheckResult.Unhealthy("Không kết nối được PostgreSQL.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Không kết nối được PostgreSQL.", ex);
        }
    }
}
