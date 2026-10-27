using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RiskAI.Contracts;

namespace RiskAI.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký client gọi ai-service. RiskAI không tự đăng ký ITaskModuleApi/IProjectsModuleApi:
    /// hai interface này do module TaskManagement và Projects cung cấp, thiếu thì Host báo lỗi DI ngay khi khởi động.
    /// </summary>
    public static IServiceCollection AddRiskAIInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiServiceOptions>()
            .Bind(configuration.GetSection(AiServiceOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "AiService:BaseUrl phải là URL tuyệt đối.")
            .Validate(o => o.TimeoutSeconds is > 0 and <= 60, "AiService:TimeoutSeconds phải trong khoảng 1–60.")
            .ValidateOnStart();

        services.AddHttpClient<IRiskAiClient, HttpRiskAiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AiServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
