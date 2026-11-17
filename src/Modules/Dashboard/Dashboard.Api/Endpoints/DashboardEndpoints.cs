using System.Security.Claims;
using Dashboard.Application;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
namespace Dashboard.Api.Endpoints;
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/dashboard", async (Guid projectId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetProjectDashboardQuery(projectId, userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(detail: result.Error,
                statusCode: result.ErrorType == ErrorType.NotFound ? 404 : result.ErrorType == ErrorType.Forbidden ? 403 : 500);
        }).WithTags("Dashboard").RequireAuthorization();
        return app;
    }
}
