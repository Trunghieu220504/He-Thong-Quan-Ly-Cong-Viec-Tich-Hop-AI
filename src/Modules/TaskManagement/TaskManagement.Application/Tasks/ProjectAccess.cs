using Projects.Contracts;
using SharedKernel;

namespace TaskManagement.Application.Tasks;

/// <summary>Kiểm tra quyền theo vai trò trong dự án, chỉ qua <see cref="IProjectsModuleApi"/>.</summary>
internal static class ProjectAccess
{
    private const string Viewer = "Viewer";

    /// <summary>Thành viên bất kỳ được xem; ghi (tạo/sửa) yêu cầu vai trò khác Viewer.</summary>
    public static async Task<Result> RequireAsync(
        IProjectsModuleApi projects, Guid projectId, Guid userId, bool write, CancellationToken cancellationToken)
    {
        var role = await projects.GetMemberRoleAsync(projectId, userId, cancellationToken);
        if (role is null)
        {
            return await projects.ProjectExistsAsync(projectId, cancellationToken)
                ? Result.Forbidden("Bạn không phải thành viên của dự án này.")
                : Result.NotFound("Không tìm thấy dự án.");
        }

        if (write && string.Equals(role, Viewer, StringComparison.OrdinalIgnoreCase))
            return Result.Forbidden("Vai trò Viewer chỉ được xem công việc, không được tạo hoặc chỉnh sửa.");

        return Result.Success();
    }

    /// <summary>Người được giao phải là thành viên của chính dự án chứa công việc.</summary>
    public static async Task<Result> RequireAssigneeIsMemberAsync(
        IProjectsModuleApi projects, Guid projectId, Guid assigneeId, CancellationToken cancellationToken)
    {
        var role = await projects.GetMemberRoleAsync(projectId, assigneeId, cancellationToken);
        return role is null
            ? Result.ValidationFailure("Người được giao phải là thành viên của dự án.")
            : Result.Success();
    }

    public static Result<T> ToFailure<T>(Result failure) => failure.ErrorType switch
    {
        ErrorType.NotFound => Result<T>.NotFound(failure.Error!),
        ErrorType.Forbidden => Result<T>.Forbidden(failure.Error!),
        _ => Result<T>.ValidationFailure(failure.Error!)
    };
}
