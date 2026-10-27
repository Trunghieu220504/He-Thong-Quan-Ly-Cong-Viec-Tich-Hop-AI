using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using Sprints.Application.Sprints.AddTaskToSprint;
using Sprints.Application.Sprints.CompleteSprint;
using Sprints.Application.Sprints.CreateSprint;
using Sprints.Application.Sprints.GetProjectSprints;
using Sprints.Application.Sprints.GetSprintBurndown;
using Sprints.Application.Sprints.StartSprint;

namespace Sprints.Api.Endpoints;

public static class SprintsEndpoints
{
    public sealed record CreateSprintRequest(string Name, string? Goal, DateTime? StartDate, DateTime? EndDate);
    public sealed record AddTaskRequest(Guid TaskId);

    public static IEndpointRouteBuilder MapSprintsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Sprints").RequireAuthorization();

        group.MapPost("/projects/{projectId:guid}/sprints", async (
            Guid projectId,
            CreateSprintRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new CreateSprintCommand(
                projectId, userId, request.Name, request.Goal, request.StartDate, request.EndDate), cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{projectId}/sprints", new { id = result.Value })
                : ToProblem(result);
        });

        group.MapGet("/projects/{projectId:guid}/sprints", async (
            Guid projectId,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetProjectSprintsQuery(projectId, userId), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        group.MapPost("/sprints/{id:guid}/start", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new StartSprintCommand(id, userId), cancellationToken);
            return result.IsSuccess ? Results.Ok() : ToProblem(result);
        });

        group.MapPost("/sprints/{id:guid}/complete", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new CompleteSprintCommand(id, userId), cancellationToken);
            return result.IsSuccess ? Results.Ok() : ToProblem(result);
        });

        group.MapPost("/sprints/{id:guid}/tasks", async (
            Guid id,
            AddTaskRequest request,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new AddTaskToSprintCommand(id, userId, request.TaskId), cancellationToken);
            return result.IsSuccess ? Results.Created($"/api/sprints/{id}/tasks", new { sprintId = id, request.TaskId }) : ToProblem(result);
        });

        group.MapGet("/sprints/{id:guid}/burndown", async (
            Guid id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetSprintBurndownQuery(id, userId), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
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
