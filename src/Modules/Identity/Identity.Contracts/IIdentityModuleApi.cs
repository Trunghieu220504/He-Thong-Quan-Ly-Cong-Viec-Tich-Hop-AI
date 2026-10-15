namespace Identity.Contracts;

/// <summary>
/// Cổng giao tiếp công khai của module Identity cho các module khác (Projects, Tasks...).
/// Module khác chỉ được dùng interface này, không reference Identity.Domain / Identity.Infrastructure.
/// </summary>
public interface IIdentityModuleApi
{
    /// <summary>Kiểm tra user có tồn tại hay không (vd. validate OwnerId, MemberId).</summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lấy thông tin hiển thị cơ bản của user; null nếu không tồn tại.</summary>
    Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record UserSummary(Guid Id, string Email, string DisplayName);
