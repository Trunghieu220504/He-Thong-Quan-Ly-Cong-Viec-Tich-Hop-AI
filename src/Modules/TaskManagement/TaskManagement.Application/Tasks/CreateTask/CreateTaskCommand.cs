using Application.Abstractions.CQRS;
using SharedKernel;

namespace TaskManagement.Application.Tasks.CreateTask;

public sealed record CreateTaskCommand(
    Guid ProjectId,
    Guid ActorId,
    string Title,
    string? Description,
    string Priority,
    Guid? AssigneeId,
    DateTime? DueDate, double? EstimatedHours = null, int ProgressPercent = 0)
    : ICommand<Result<TaskDto>>;
