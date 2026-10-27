using System.Text;
using System.Text.Json.Serialization;
using Identity.Api.Endpoints;
using Identity.Application;
using Identity.Infrastructure;
using Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Projects.Api.Endpoints;
using Projects.Application;
using Projects.Infrastructure;
using TaskManagement.Api.Endpoints;
using TaskManagement.Application;
using TaskManagement.Infrastructure;
using RiskAI.Api.Endpoints;
using RiskAI.Application;
using RiskAI.Infrastructure;
using Sprints.Api.Endpoints;
using Sprints.Application;
using Sprints.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- Application layer: MediatR + FluentValidation cÃƒÂ¡Ã‚Â»Ã‚Â§a tÃƒÂ¡Ã‚Â»Ã‚Â«ng module ----
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Identity.Application.DependencyInjection).Assembly,
    typeof(Projects.Application.DependencyInjection).Assembly,
    typeof(Sprints.Application.DependencyInjection).Assembly,
    typeof(TaskManagement.Application.DependencyInjection).Assembly));

builder.Services.AddIdentityApplication();
builder.Services.AddProjectsApplication();
builder.Services.AddSprintsApplication();
builder.Services.AddTaskManagementApplication();
builder.Services.AddRiskAIApplication();

// ---- Infrastructure: DbContext (schema "identity"), BCrypt, JWT generator ----
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddProjectsInfrastructure(builder.Configuration);
builder.Services.AddSprintsInfrastructure(builder.Configuration);

// Ãƒâ€žÃ‚ÂÃƒâ€žÃ†â€™ng kÃƒÆ’Ã‚Â½ ITaskModuleApi thÃƒÂ¡Ã‚ÂºÃ‚Â­t trÃƒâ€ Ã‚Â°ÃƒÂ¡Ã‚Â»Ã¢â‚¬Âºc Ãƒâ€žÃ¢â‚¬ËœÃƒÂ¡Ã‚Â»Ã†â€™ cÃƒÆ’Ã‚Â¡c TryAdd dÃƒÂ¡Ã‚Â»Ã‚Â± phÃƒÆ’Ã‚Â²ng cÃƒÂ¡Ã‚Â»Ã‚Â§a Sprints/RiskAI bÃƒÂ¡Ã‚Â»Ã¢â‚¬Â¹ bÃƒÂ¡Ã‚Â»Ã‚Â qua (khÃƒÆ’Ã‚Â´ng Ãƒâ€žÃ¢â‚¬ËœÃƒÂ¡Ã‚Â»Ã†â€™ hai bÃƒÂ¡Ã‚ÂºÃ‚Â£n cÃƒÆ’Ã‚Â¹ng tÃƒÂ¡Ã‚Â»Ã¢â‚¬Å“n tÃƒÂ¡Ã‚ÂºÃ‚Â¡i).
builder.Services.AddTaskManagementInfrastructure(builder.Configuration);

// ---- Authentication: JWT Bearer (khÃƒÂ¡Ã‚Â»Ã¢â‚¬Âºp JwtTokenGenerator) ----
// ---- RiskAI: HttpClient gá»i ai-service (AiService:BaseUrl, máº·c Ä‘á»‹nh http://localhost:8000) ----
builder.Services.AddRiskAIInfrastructure(builder.Configuration);

// ---- Authentication: JWT Bearer (khá»›p JwtTokenGenerator) ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.SecretKey.Length < 32)
    throw new InvalidOperationException(
        "ThiÃƒÂ¡Ã‚ÂºÃ‚Â¿u Jwt:SecretKey (>= 32 kÃƒÆ’Ã‚Â½ tÃƒÂ¡Ã‚Â»Ã‚Â±). DÃƒÆ’Ã‚Â¹ng: dotnet user-secrets set \"Jwt:SecretKey\" \"<chuÃƒÂ¡Ã‚Â»Ã¢â‚¬â€i ngÃƒÂ¡Ã‚ÂºÃ‚Â«u nhiÃƒÆ’Ã‚Âªn>\" hoÃƒÂ¡Ã‚ÂºÃ‚Â·c biÃƒÂ¡Ã‚ÂºÃ‚Â¿n mÃƒÆ’Ã‚Â´i trÃƒâ€ Ã‚Â°ÃƒÂ¡Ã‚Â»Ã‚Âng Jwt__SecretKey.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // giÃƒÂ¡Ã‚Â»Ã‚Â¯ nguyÃƒÆ’Ã‚Âªn tÃƒÆ’Ã‚Âªn claim: sub, email, name, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience, // refresh token dÃƒÆ’Ã‚Â¹ng audience khÃƒÆ’Ã‚Â¡c nÃƒÆ’Ã‚Âªn bÃƒÂ¡Ã‚Â»Ã¢â‚¬Â¹ tÃƒÂ¡Ã‚Â»Ã‚Â« chÃƒÂ¡Ã‚Â»Ã¢â‚¬Ëœi ÃƒÂ¡Ã‚Â»Ã…Â¸ Ãƒâ€žÃ¢â‚¬ËœÃƒÆ’Ã‚Â¢y
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// ---- JSON: enum nhÃƒÂ¡Ã‚ÂºÃ‚Â­n/trÃƒÂ¡Ã‚ÂºÃ‚Â£ dÃƒÂ¡Ã‚ÂºÃ‚Â¡ng chuÃƒÂ¡Ã‚Â»Ã¢â‚¬â€i ("Todo", khÃƒÆ’Ã‚Â´ng phÃƒÂ¡Ã‚ÂºÃ‚Â£i 0) Ãƒâ€žÃ¢â‚¬ËœÃƒÂ¡Ã‚Â»Ã†â€™ khÃƒÂ¡Ã‚Â»Ã¢â‚¬Âºp contract vÃƒÂ¡Ã‚Â»Ã¢â‚¬Âºi web ----
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// ---- CORS: chÃƒÂ¡Ã‚Â»Ã¢â‚¬Â° bÃƒÂ¡Ã‚ÂºÃ‚Â­t khi cÃƒÆ’Ã‚Â³ Cors:AllowedOrigins (appsettings.Development.json khai bÃƒÆ’Ã‚Â¡o origin cÃƒÂ¡Ã‚Â»Ã‚Â§a web dev) ----
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));
}

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
if (corsOrigins.Length > 0) app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapAuthEndpoints();
app.MapProjectsEndpoints();
app.MapSprintsEndpoints();
app.MapTaskManagementEndpoints();
app.MapRiskAIEndpoints();

app.Run();

public partial class Program; // cho WebApplicationFactory trong integration tests
