using Identity.Domain;

namespace Identity.Application.Abstractions;

public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public interface IJwtTokenGenerator
{
    /// <summary>Tạo Access token (15 phút) + Refresh token (7 ngày) cho user.</summary>
    AuthTokens Generate(User user);

    /// <summary>Kiểm tra Refresh token; trả về UserId nếu hợp lệ, null nếu sai/hết hạn/không phải refresh token.</summary>
    Guid? ValidateRefreshToken(string refreshToken);
}
