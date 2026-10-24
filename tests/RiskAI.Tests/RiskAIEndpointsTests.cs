using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using RiskAI.Api.Endpoints;
using SharedKernel;

namespace RiskAI.Tests;

public class RiskAIEndpointsTests
{
    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Failure, StatusCodes.Status503ServiceUnavailable)]
    public void Loi_cua_handler_duoc_map_dung_ma_HTTP(ErrorType errorType, int expectedStatus)
    {
        var problem = Assert.IsType<ProblemHttpResult>(RiskAIEndpoints.ToProblem(ResultOf(errorType)));

        Assert.Equal(expectedStatus, problem.StatusCode);
    }

    [Fact]
    public void Ai_khong_kha_dung_tra_503_kem_thong_bao_khong_phai_500()
    {
        var problem = Assert.IsType<ProblemHttpResult>(
            RiskAIEndpoints.ToProblem(Result.Failure("Không kết nối được AI service.")));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, problem.StatusCode);
        Assert.Equal(RiskAIEndpoints.AiUnavailableTitle, problem.ProblemDetails.Title);
        Assert.Equal("Không kết nối được AI service.", problem.ProblemDetails.Detail);
    }

    private static Result ResultOf(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => Result.NotFound("Không tìm thấy task."),
        ErrorType.Forbidden => Result.Forbidden("Bạn không phải thành viên của dự án này."),
        ErrorType.Validation => Result.ValidationFailure("Dữ liệu không hợp lệ."),
        ErrorType.Failure => Result.Failure("AI service trả mã lỗi 500."),
        _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, null)
    };
}
