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

// ---- Application layer: MediatR + FluentValidation cÃ¡Â»Â§a tÃ¡Â»Â«ng module ----
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Identity.Application.DependencyInjection).Assembly,
    typeof(Projects.Application.DependencyInjection).Assembly,
    typeof(Sprints.Application.DependencyInjection).Assembly,
    typeof(TaskManagement.Application.DependencyInjection).Assembly));
    typeof(RiskAI.Application.DependencyInjection).Assembly));

builder.Services.AddIdentityApplication();
builder.Services.AddProjectsApplication();
builder.Services.AddSprintsApplication();
builder.Services.AddTaskManagementApplication();
builder.Services.AddRiskAIApplication();

// ---- Infrastructure: DbContext (schema "identity"), BCrypt, JWT generator ----
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddProjectsInfrastructure(builder.Configuration);
builder.Services.AddSprintsInfrastructure(builder.Configuration);

// Ã„ÂÃ„Æ’ng kÃƒÂ½ ITaskModuleApi thÃ¡ÂºÂ­t trÃ†Â°Ã¡Â»â€ºc Ã„â€˜Ã¡Â»Æ’ cÃƒÂ¡c TryAdd dÃ¡Â»Â± phÃƒÂ²ng cÃ¡Â»Â§a Sprints/RiskAI bÃ¡Â»â€¹ bÃ¡Â»Â qua (khÃƒÂ´ng Ã„â€˜Ã¡Â»Æ’ hai bÃ¡ÂºÂ£n cÃƒÂ¹ng tÃ¡Â»â€œn tÃ¡ÂºÂ¡i).
builder.Services.AddTaskManagementInfrastructure(builder.Configuration);

// ---- Authentication: JWT Bearer (khÃ¡Â»â€ºp JwtTokenGenerator) ----
// ---- RiskAI: HttpClient gọi ai-service (AiService:BaseUrl, mặc định http://localhost:8000) ----
builder.Services.AddRiskAIInfrastructure(builder.Configuration);

// ---- Authentication: JWT Bearer (khớp JwtTokenGenerator) ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.SecretKey.Length < 32)
    throw new InvalidOperationException(
        "ThiÃ¡ÂºÂ¿u Jwt:SecretKey (>= 32 kÃƒÂ½ tÃ¡Â»Â±). DÃƒÂ¹ng: dotnet user-secrets set \"Jwt:SecretKey\" \"<chuÃ¡Â»â€”i ngÃ¡ÂºÂ«u nhiÃƒÂªn>\" hoÃ¡ÂºÂ·c biÃ¡ÂºÂ¿n mÃƒÂ´i trÃ†Â°Ã¡Â»Âng Jwt__SecretKey.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // giÃ¡Â»Â¯ nguyÃƒÂªn tÃƒÂªn claim: sub, email, name, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience, // refresh token dÃƒÂ¹ng audience khÃƒÂ¡c nÃƒÂªn bÃ¡Â»â€¹ tÃ¡Â»Â« chÃ¡Â»â€˜i Ã¡Â»Å¸ Ã„â€˜ÃƒÂ¢y
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// ---- JSON: enum nhÃ¡ÂºÂ­n/trÃ¡ÂºÂ£ dÃ¡ÂºÂ¡ng chuÃ¡Â»â€”i ("Todo", khÃƒÂ´ng phÃ¡ÂºÂ£i 0) Ã„â€˜Ã¡Â»Æ’ khÃ¡Â»â€ºp contract vÃ¡Â»â€ºi web ----
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// ---- CORS: chÃ¡Â»â€° bÃ¡ÂºÂ­t khi cÃƒÂ³ Cors:AllowedOrigins (appsettings.Development.json khai bÃƒÂ¡o origin cÃ¡Â»Â§a web dev) ----
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
