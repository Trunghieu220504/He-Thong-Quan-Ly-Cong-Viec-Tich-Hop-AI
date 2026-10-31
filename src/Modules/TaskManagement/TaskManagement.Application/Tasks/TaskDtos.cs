namespace TaskManagement.Application.Tasks;

public sealed record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
