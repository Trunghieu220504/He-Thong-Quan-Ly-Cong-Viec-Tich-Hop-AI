using SharedKernel;

namespace Sprints.Domain.Entities;

public sealed class SprintTask : Entity<Guid>
{
    public Guid SprintId { get; private set; }
    public Guid TaskId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private SprintTask() { }

    internal static SprintTask Create(Guid sprintId, Guid taskId) => new()
    {
        Id = Guid.NewGuid(),
        SprintId = sprintId,
        TaskId = taskId,
        AddedAt = DateTime.UtcNow
    };
}
