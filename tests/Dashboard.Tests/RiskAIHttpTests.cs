using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dashboard.Api.Endpoints;
using Dashboard.Application;
using Identity.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Projects.Contracts;
using RiskAI.Api.Endpoints;
using RiskAI.Application;
using RiskAI.Contracts;
using Sprints.Contracts;
using TaskManagement.Contracts;

namespace Dashboard.Tests;
public class RiskAIHttpTests
{
    [Fact]
    public async Task Real_http_checks_authorization_dashboard_and_batch_risks()
    {
        var data = new Contracts();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("local-test-only-signing-key-32-characters"));
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblies(
            typeof(GetProjectDashboardQuery).Assembly, typeof(RiskAI.Application.DependencyInjection).Assembly));
        builder.Services.AddDashboardApplication();
        builder.Services.AddRiskAIApplication();
        builder.Services.AddSingleton<IProjectsModuleApi>(data);
        builder.Services.AddSingleton<ITaskModuleApi>(data);
        builder.Services.AddSingleton<ISprintsModuleApi>(data);
        builder.Services.AddSingleton<IIdentityModuleApi>(data);
        builder.Services.AddSingleton<IRiskAiClient>(data);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
        {
            o.MapInboundClaims = false;
            o.TokenValidationParameters = new() { ValidateIssuer = true, ValidIssuer = "tests", ValidateAudience = true,
                ValidAudience = "tests", ValidateLifetime = true, IssuerSigningKey = key, ValidateIssuerSigningKey = true };
        });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication(); app.UseAuthorization();
        app.MapDashboardEndpoints(); app.MapRiskAIEndpoints();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        string path = $"/api/projects/{data.ProjectId}/dashboard";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        void Login(Guid id)
        {
            var token = new JwtSecurityToken("tests", "tests", [new Claim("sub", id.ToString())],
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new(key, SecurityAlgorithms.HmacSha256));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        }
        Login(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/projects/{Guid.NewGuid()}/dashboard")).StatusCode);
        Assert.Equal(0, data.Reads);
        Login(data.UserId);
        foreach (var role in new[] { "Owner", "Admin", "Member", "Viewer" })
        {
            data.Role = role;
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = (await response.Content.ReadFromJsonAsync<ProjectDashboardDto>())!;
            Assert.Equal(50, body.Tasks.CompletionPercent);
            Assert.Equal(1, body.Tasks.Overdue);
            Assert.Equal("Chưa giao", body.ByAssignee.Single(x => x.AssigneeId is null).DisplayName);
            Assert.Equal("Tester", body.ByAssignee.Single(x => x.AssigneeId is not null).DisplayName);
            Assert.Equal(1, body.ActiveSprint!.RemainingTasks);
        }
        data.Empty = true;
        var empty = (await client.GetFromJsonAsync<ProjectDashboardDto>(path))!;
        Assert.Equal(0, empty.Tasks.CompletionPercent); Assert.Null(empty.ActiveSprint);
        data.Empty = false;
        var risks = $"/api/projects/{data.ProjectId}/risks";
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(risks)).StatusCode);
        Assert.Equal(12.5, data.LastRequest!.EstimatedHours); Assert.Equal(37, data.LastRequest.ProgressPercent);
        data.FailAi = true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync(risks)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync($"/api/tasks/{data.TaskId}/risk")).StatusCode);
        Login(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(risks)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/projects/{Guid.NewGuid()}/risks")).StatusCode);
        await app.StopAsync();
    }
    private sealed class Contracts : IProjectsModuleApi, ITaskModuleApi, ISprintsModuleApi, IIdentityModuleApi, IRiskAiClient
    {
        public Guid ProjectId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid TaskId { get; } = Guid.NewGuid();
        public string Role { get; set; } = "Viewer";
        public int Reads { get; private set; }
        public bool Empty { get; set; }
        public bool FailAi { get; set; }
        public RiskAiRequest? LastRequest { get; private set; }
        public Task<bool> ProjectExistsAsync(Guid id, CancellationToken ct = default) => Task.FromResult(id == ProjectId);
        public Task<string?> GetMemberRoleAsync(Guid id, Guid user, CancellationToken ct = default) => Task.FromResult<string?>(id == ProjectId && user == UserId ? Role : null);
        public Task<ProjectTaskStats> GetProjectStatsAsync(Guid id, DateTime now, CancellationToken ct = default)
        {
            Reads++;
            return Task.FromResult(Empty ? new ProjectTaskStats(0,0,0,0,0,[]) : new(2,0,1,1,1,[new(null,1,0),new(UserId,1,1)]));
        }
        public Task<ActiveSprintOverview?> GetActiveSprintOverviewAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<ActiveSprintOverview?>(Empty ? null : new(Guid.NewGuid(), "Sprint", null, null, 2, 1, 1));
        public Task<UserSummary?> GetUserSummaryAsync(Guid id, CancellationToken ct = default) => Task.FromResult<UserSummary?>(new(id,"test@example.invalid","Tester"));
        public Task<bool> UserExistsAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<TaskSummary?> GetTaskAsync(Guid id, CancellationToken ct = default) => Task.FromResult<TaskSummary?>(id == TaskId ? Row() : null);
        private TaskSummary Row() => new(TaskId,ProjectId,ProjectTaskStatus.InProgress,null,UserId,12.5,37);
        public Task<IReadOnlyList<TaskSummary>> GetProjectTasksAsync(Guid id, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TaskSummary>>(Empty ? [] : [Row()]);
        public Task<TaskStatusCounts> CountByStatusAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<RiskAiResult> PredictAsync(RiskAiRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return FailAi ? Task.FromException<RiskAiResult>(new RiskAiUnavailableException("AI offline")) : Task.FromResult(new RiskAiResult(request.TaskId,"medium"));
        }
    }
}
