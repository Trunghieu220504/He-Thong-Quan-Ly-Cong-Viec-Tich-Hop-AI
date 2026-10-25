using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using TaskManagement.Application.Tasks.AssignTask;
using TaskManagement.Application.Tasks.ChangeTaskStatus;
using TaskManagement.Application.Tasks.CreateTask;
using TaskManagement.Application.Tasks.GetProjectTasks;
using TaskManagement.Application.Tasks.GetTaskById;

namespace TaskManagement.Api.Endpoints;

public static class TaskManagementEndpoints
{
    public sealed record CreateTaskRequest(
        string Title, string? Description, string Priority, Guid? AssigneeId, DateTime? DueDate);

    public sealed record ChangeTaskStatusRequest(string Status);

    public sealed record AssignTaskRequest(Guid? AssigneeId);

    public static IEndpointRouteBuilder MapTaskManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Tasks").RequireAuthorization();

        group.MapPost("/projects/{projectId:guid}/tasks", async (
            Guid projectId, CreateTaskRequest req, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new CreateTaskCommand(
                projectId, userId, req.Title, req.Description, req.Priority, req.AssigneeId, req.DueDate), ct);
            return result.IsSuccess
                ? Results.Created($"/api/tasks/{result.Value!.Id}", result.Value)
                : ToProblem(result);
        });

        group.MapGet("/projects/{projectId:guid}/tasks", async (
            Guid projectId, ClaimsPrincipal user, ISender sender, CancellationToken ct,
            string? status = null, int page = 1, int pageSize = 50) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetProjectTasksQuery(projectId, userId, status, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        group.MapGet("/tasks/{taskId:guid}", async (
            Guid taskId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetTaskByIdQuery(taskId, userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        group.MapPatch("/tasks/{taskId:guid}/status", async (
            Guid taskId, ChangeTaskStatusRequest req, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new ChangeTaskStatusCommand(taskId, userId, req.Status), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        group.MapPatch("/tasks/{taskId:guid}/assignee", async (
            Guid taskId, AssignTaskRequest req, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new AssignTaskCommand(taskId, userId, req.AssigneeId), ct);
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
