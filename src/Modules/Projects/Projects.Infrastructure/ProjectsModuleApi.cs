using Projects.Application.Abstractions;
using Projects.Contracts;

namespace Projects.Infrastructure;

internal sealed class ProjectsModuleApi(IProjectRepository projects) : IProjectsModuleApi
{
    public async Task<string?> GetMemberRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
    {
        var project = await projects.GetByIdAsync(projectId, ct);
        return project?.Members.FirstOrDefault(m => m.UserId == userId)?.Role.ToString();
    }

    public async Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken ct = default) =>
        await projects.GetByIdAsync(projectId, ct) is not null;
}