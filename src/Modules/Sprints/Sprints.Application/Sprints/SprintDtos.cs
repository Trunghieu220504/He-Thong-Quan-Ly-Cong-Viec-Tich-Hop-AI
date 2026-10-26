using Sprints.Domain.Enums;

namespace Sprints.Application.Sprints;

public sealed record SprintDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Goal,
    DateTime? StartDate,
    DateTime? EndDate,
    SprintStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<SprintTaskDto> Tasks);

public sealed record SprintTaskDto(Guid TaskId, DateTime AddedAt);

public sealed record SprintBurndownDto(
    Guid SprintId,
    int TotalTasks,
    int CompletedTasks,
    int RemainingTasks);
