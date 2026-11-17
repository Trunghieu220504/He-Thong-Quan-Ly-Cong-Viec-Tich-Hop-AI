using Application.Abstractions.CQRS;
using SharedKernel;
namespace TaskManagement.Application.Tasks.UpdateTask;
public sealed record UpdateTaskCommand(Guid TaskId, Guid ActorId, string Title, string? Description,
    string Priority, DateTime? DueDate, double? EstimatedHours, int ProgressPercent) : ICommand<Result<TaskDto>>;
