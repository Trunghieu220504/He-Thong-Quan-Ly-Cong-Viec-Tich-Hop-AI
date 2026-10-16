using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Abstractions;
using Identity.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private const string TokenTypeClaim = "token_type";
    private const string RefreshType = "refresh";

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;
    private readonly JwtSecurityTokenHandler _handler = new() { MapInboundClaims = false };

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SecretKey) || Encoding.UTF8.GetByteCount(_options.SecretKey) < 32)
            throw new InvalidOperationException("Jwt:SecretKey phải có tối thiểu 32 ký tự.");

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
    }

    // Refresh token dùng audience riêng để JwtBearer (kiểm tra Audience) không chấp nhận nó như Access token.
    private string RefreshAudience => _options.Audience + ".refresh";

    public AuthTokens Generate(User user)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpires = now.AddDays(_options.RefreshTokenDays);

        var accessClaims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim("name", user.DisplayName),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var refreshClaims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(TokenTypeClaim, RefreshType),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return new AuthTokens(
            Write(accessClaims, _options.Audience, now, accessExpires),
            accessExpires,
            Write(refreshClaims, RefreshAudience, now, refreshExpires),
            refreshExpires);
    }

    public Guid? ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = RefreshAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        try
        {
            var principal = _handler.ValidateToken(refreshToken, parameters, out _);
            if (principal.FindFirst(TokenTypeClaim)?.Value != RefreshType) return null;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    private string Write(IEnumerable<Claim> claims, string audience, DateTime notBefore, DateTime expires)
    {
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        return _handler.WriteToken(token);
    }
}
