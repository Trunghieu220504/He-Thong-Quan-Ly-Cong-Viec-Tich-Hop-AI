using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Projects.Application.Projects.AddMember;
using Projects.Application.Projects.CreateProject;
using Projects.Application.Projects.GetProjectById;
using Projects.Application.Projects.GetProjects;
using SharedKernel;

namespace Projects.Api.Endpoints;

public static class ProjectsEndpoints
{
    public sealed record CreateProjectRequest(string Name, string? Description);
    public sealed record AddMemberRequest(Guid UserId, string Role);

    public static IEndpointRouteBuilder MapProjectsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization();

        group.MapPost("/", async (CreateProjectRequest req, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new CreateProjectCommand(userId, req.Name, req.Description), ct);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{result.Value}", new { id = result.Value })
                : ToProblem(result);
        });

        group.MapGet("/", async (ClaimsPrincipal user, ISender sender, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetProjectsQuery(userId, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetProjectByIdQuery(id, userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        group.MapPost("/{id:guid}/members", async (Guid id, AddMemberRequest req, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new AddMemberCommand(id, userId, req.UserId, req.Role), ct);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{id}", new { projectId = id, userId = req.UserId, role = req.Role })
                : ToProblem(result);
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue("sub"), out userId);

    private static IResult ToProblem(Result result)
    {
        var status = result.ErrorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(detail: result.Error, statusCode: status);
    }
}