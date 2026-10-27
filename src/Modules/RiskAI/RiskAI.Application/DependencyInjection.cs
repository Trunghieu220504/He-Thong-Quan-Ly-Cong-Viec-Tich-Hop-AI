using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RiskAI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRiskAIApplication(this IServiceCollection services)
    {
        // Đồng hồ hệ thống cho việc tính days_until_due; test thay bằng đồng hồ cố định
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
