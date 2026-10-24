using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RiskAI.Contracts;
using TaskManagement.Contracts;

namespace RiskAI.Infrastructure;

public static class DependencyInjection
{
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

        // Dự phòng tới khi TaskManagement có implementation thật (xem UnavailableTaskModuleApi)
        services.TryAddScoped<ITaskModuleApi, UnavailableTaskModuleApi>();

        return services;
    }
}
