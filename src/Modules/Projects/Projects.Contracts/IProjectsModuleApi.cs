namespace Projects.Contracts;

public interface IProjectsModuleApi
{
    /// <summary>
    /// Vai trò của user trong dự án ("Owner" | "Admin" | "Member" | "Viewer"); null nếu không phải thành viên.
    /// </summary>
    Task<string?> GetMemberRoleAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken = default);
}