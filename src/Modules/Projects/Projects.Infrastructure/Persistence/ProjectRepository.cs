using Microsoft.EntityFrameworkCore;
using Projects.Application.Abstractions;
using Projects.Domain.Entities;

namespace Projects.Infrastructure.Persistence;

internal sealed class ProjectRepository(ProjectsDbContext db) : IProjectRepository
{
    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Projects.Include(p => p.Members).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPageByMemberAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Projects.AsNoTracking()
            .Where(p => p.Members.Any(m => m.UserId == userId));

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(p => p.Members)
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Add(Project project) => db.Projects.Add(project);
}