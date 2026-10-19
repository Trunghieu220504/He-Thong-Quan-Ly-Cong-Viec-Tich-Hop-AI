namespace Projects.Application.Projects;

public sealed record ProjectSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string MyRole,
    int MemberCount,
    DateTime CreatedAt);

public sealed record ProjectMemberDto(Guid UserId, string Role, DateTime JoinedAt);

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    string MyRole,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProjectMemberDto> Members);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);