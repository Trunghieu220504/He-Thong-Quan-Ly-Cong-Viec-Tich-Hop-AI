using Sprints.Domain.Entities;

namespace Sprints.Application.Abstractions;

public interface ISprintRepository
{
    Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sprint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveSprintAsync(Guid projectId, CancellationToken cancellationToken = default);
    void Add(Sprint sprint);
}
