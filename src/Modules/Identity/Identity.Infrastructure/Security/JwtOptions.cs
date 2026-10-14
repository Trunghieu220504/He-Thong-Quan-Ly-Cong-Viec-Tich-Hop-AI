namespace Identity.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ProjectManagementAI";
    public string Audience { get; set; } = "ProjectManagementAI.Client";

    /// <summary>Khóa ký HMAC-SHA256, tối thiểu 32 ký tự. Không commit khóa thật: dùng user-secrets / biến môi trường Jwt__SecretKey.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
