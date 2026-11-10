using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Projects.Infrastructure.Persistence;
using Sprints.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence;

namespace ProjectManagementAI.Api;

/// <summary>
/// Tự áp dụng migration khi khởi động. Mặc định TẮT; bật bằng <c>Database:MigrateOnStartup=true</c>
/// (biến môi trường <c>Database__MigrateOnStartup=true</c> trong Docker).
/// </summary>
public static class DatabaseMigrationExtensions
{
    public const string ConfigKey = "Database:MigrateOnStartup";

    // Thứ tự là hợp đồng: Projects cần Identity (người dùng), TaskManagement cần Projects, Sprints cần cả hai.
    // Khi Notifications có DbContext (task N1), thêm NotificationsDbContext vào CUỐI danh sách này.
    private static readonly Type[] ContextsInOrder =
    [
        typeof(IdentityDbContext),
        typeof(ProjectsDbContext),
        typeof(TaskManagementDbContext),
        typeof(SprintsDbContext)
    ];

    /// <summary>
    /// Chạy <c>MigrateAsync</c> lần lượt cho từng module nếu cờ bật. Lỗi ở bất kỳ module nào sẽ được ghi log rồi
    /// ném ra để API dừng ngay (fail-fast), tránh chạy với schema thiếu.
    /// </summary>
    public static async Task MigrateDatabasesAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

        if (!app.Configuration.GetValue<bool>(ConfigKey))
        {
            logger.LogInformation("{Key} đang tắt, bỏ qua bước tự migrate.", ConfigKey);
            return;
        }

        using var scope = app.Services.CreateScope();
        foreach (var contextType in ContextsInOrder)
        {
            var db = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
            try
            {
                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Migrate {Context}: đã áp dụng {Count} migration.", contextType.Name, pending);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Migrate {Context} thất bại; dừng khởi động API.", contextType.Name);
                throw;
            }
        }
    }
}
