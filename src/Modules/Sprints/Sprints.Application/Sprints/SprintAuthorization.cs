namespace Sprints.Application.Sprints;

internal static class SprintAuthorization
{
    public static bool IsOwnerOrAdmin(string? role) =>
        string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
}
