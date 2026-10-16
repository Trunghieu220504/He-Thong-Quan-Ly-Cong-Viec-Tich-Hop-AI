namespace Identity.Application.Abstractions;

/// <summary>Unit of work riêng của module Identity (tránh đụng IUnitOfWork của module khác khi đăng ký DI).</summary>
public interface IIdentityUnitOfWork
{
    /// <exception cref="DuplicateEmailException">Email đã tồn tại (vi phạm unique index).</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class DuplicateEmailException(Exception inner)
    : Exception("Email đã được sử dụng.", inner);
