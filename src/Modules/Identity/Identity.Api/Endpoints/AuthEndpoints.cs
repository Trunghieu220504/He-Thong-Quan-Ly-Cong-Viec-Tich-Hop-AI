using Identity.Application.Auth.Login;
using Identity.Application.Auth.Refresh;
using Identity.Application.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;

namespace Identity.Api.Endpoints;

public static class AuthEndpoints
{
    public sealed record RegisterRequest(string Email, string Password, string DisplayName);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterCommand(req.Email, req.Password, req.DisplayName), ct);
            return result.IsSuccess
                ? Results.Created(string.Empty, new { id = result.Value })
                : ToProblem(result);
        });

        group.MapPost("/login", async (LoginRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(req.Email, req.Password), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        group.MapPost("/refresh", async (RefreshRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(req.RefreshToken), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result);
        });

        return app;
    }

    /// <summary>Map Result.ErrorType sang HTTP status (Failure ở Auth = sai thông tin đăng nhập → 401).</summary>
    private static IResult ToProblem(Result result)
    {
        var status = result.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status401Unauthorized
        };
        return Results.Problem(detail: result.Error, statusCode: status);
    }
}
