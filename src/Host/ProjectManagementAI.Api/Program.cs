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
<<<<<<< HEAD
using TaskManagement.Api.Endpoints;
using TaskManagement.Application;
using TaskManagement.Infrastructure;
=======
using Sprints.Api.Endpoints;
using Sprints.Application;
using Sprints.Infrastructure;
>>>>>>> origin/develop

var builder = WebApplication.CreateBuilder(args);

// ---- Application layer: MediatR + FluentValidation cá»§a tá»«ng module ----
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(Identity.Application.DependencyInjection).Assembly,
    typeof(Projects.Application.DependencyInjection).Assembly,
    typeof(Sprints.Application.DependencyInjection).Assembly,
    typeof(TaskManagement.Application.DependencyInjection).Assembly));

builder.Services.AddIdentityApplication();
builder.Services.AddProjectsApplication();
builder.Services.AddSprintsApplication();
builder.Services.AddTaskManagementApplication();

// ---- Infrastructure: DbContext (schema "identity"), BCrypt, JWT generator ----
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddProjectsInfrastructure(builder.Configuration);
builder.Services.AddSprintsInfrastructure(builder.Configuration);

// ÄÄƒng kÃ½ ITaskModuleApi tháº­t trÆ°á»›c Ä‘á»ƒ cÃ¡c TryAdd dá»± phÃ²ng cá»§a Sprints/RiskAI bá»‹ bá» qua (khÃ´ng Ä‘á»ƒ hai báº£n cÃ¹ng tá»“n táº¡i).
builder.Services.AddTaskManagementInfrastructure(builder.Configuration);

// ---- Authentication: JWT Bearer (khá»›p JwtTokenGenerator) ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.SecretKey.Length < 32)
    throw new InvalidOperationException(
        "Thiáº¿u Jwt:SecretKey (>= 32 kÃ½ tá»±). DÃ¹ng: dotnet user-secrets set \"Jwt:SecretKey\" \"<chuá»—i ngáº«u nhiÃªn>\" hoáº·c biáº¿n mÃ´i trÆ°á»ng Jwt__SecretKey.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // giá»¯ nguyÃªn tÃªn claim: sub, email, name, role
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience, // refresh token dÃ¹ng audience khÃ¡c nÃªn bá»‹ tá»« chá»‘i á»Ÿ Ä‘Ã¢y
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// ---- JSON: enum nháº­n/tráº£ dáº¡ng chuá»—i ("Todo", khÃ´ng pháº£i 0) Ä‘á»ƒ khá»›p contract vá»›i web ----
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// ---- CORS: chá»‰ báº­t khi cÃ³ Cors:AllowedOrigins (appsettings.Development.json khai bÃ¡o origin cá»§a web dev) ----
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

app.Run();

public partial class Program; // cho WebApplicationFactory trong integration tests
