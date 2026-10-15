using Identity.Application.Abstractions;
using Identity.Contracts;

namespace Identity.Infrastructure;

internal sealed class IdentityModuleApi(IUserRepository users) : IIdentityModuleApi
{
    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await users.GetByIdAsync(userId, cancellationToken) is not null;

    public async Task<UserSummary?> GetUserSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : new UserSummary(user.Id, user.Email.Value, user.DisplayName);
    }
}
