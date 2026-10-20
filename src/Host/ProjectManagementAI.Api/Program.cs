using System.Text;
using Identity.Api.Endpoints;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Projects.Api.Endpoints;
using Projects.Application;
using Projects.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- Application layer: MediatR + FluentValidation của từng module ----
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Identity.Application.DependencyInjection).Assembly,
    typeof(Projects.Application.DependencyInjection).Assembly));

builder.Services.AddIdentityApplication();
builder.Services.AddProjectsApplication();

// ---- Infrastructure: DbContext (schema "identity"), BCrypt, JWT generator ----
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddProjectsInfrastructure(builder.Configuration);

// ---- Authentication: JWT Bearer (khớp JwtTokenGenerator) ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.SecretKey.Length < 32)
    throw new InvalidOperationException(
        "Thiếu Jwt:SecretKey (>= 32 ký tự). Dùng: dotnet user-secrets set \"Jwt:SecretKey\" \"<chuỗi ngẫu nhiên>\" hoặc biến môi trường Jwt__SecretKey.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // giữ nguyên tên claim: sub, email, name, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience, // refresh token dùng audience khác nên bị từ chối ở đây
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapAuthEndpoints();
app.MapProjectsEndpoints();

app.Run();

public partial class Program; // cho WebApplicationFactory trong integration tests
