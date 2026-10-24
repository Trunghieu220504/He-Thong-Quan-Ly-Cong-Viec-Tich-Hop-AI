using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RiskAI.Application.Risk.GetTaskRisk;
using SharedKernel;

namespace RiskAI.Api.Endpoints;

public static class RiskAIEndpoints
{
    public const string AiUnavailableTitle = "Dịch vụ AI tạm thời không khả dụng";

    public static IEndpointRouteBuilder MapRiskAIEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("RiskAI").RequireAuthorization();

        group.MapGet("/{taskId:guid}/risk", async (Guid taskId, ClaimsPrincipal user, ISender sender, CancellationToken ct) =>
        {
            if (!TryGetUserId(user, out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetTaskRiskQuery(taskId, userId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId) =>
        Guid.TryParse(user.FindFirstValue("sub"), out userId);

    internal static IResult ToProblem(Result result) => result.ErrorType switch
    {
        ErrorType.NotFound => Results.Problem(detail: result.Error, statusCode: StatusCodes.Status404NotFound),
        ErrorType.Forbidden => Results.Problem(detail: result.Error, statusCode: StatusCodes.Status403Forbidden),
        ErrorType.Validation => Results.Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest),
        ErrorType.Conflict => Results.Problem(detail: result.Error, statusCode: StatusCodes.Status409Conflict),
        // Trong module này Failure chỉ đến từ ai-service (tắt, timeout, trả sai) → 503 kèm thông báo, không văng 500
        ErrorType.Failure => Results.Problem(
            title: AiUnavailableTitle,
            detail: result.Error,
            statusCode: StatusCodes.Status503ServiceUnavailable),
        _ => Results.Problem(detail: result.Error, statusCode: StatusCodes.Status500InternalServerError)
    };
}
