using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventBus;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký <see cref="IIntegrationEventBus"/> chạy trong tiến trình (Scoped). Dùng TryAdd nên nếu sau này
    /// có bản cài đặt khác (ví dụ Outbox) đăng ký trước thì bản đó được giữ lại.
    /// </summary>
    public static IServiceCollection AddInProcessEventBus(this IServiceCollection services)
    {
        services.TryAddScoped<IIntegrationEventBus, InProcessIntegrationEventBus>();
        return services;
    }
}
