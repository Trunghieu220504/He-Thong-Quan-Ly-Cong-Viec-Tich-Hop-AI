using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Persistence;

public abstract class BaseDbContext : DbContext
{
    protected BaseDbContext(DbContextOptions options) : base(options) { }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .ToList();

        // TODO Week 3: Outbox pattern - serialize DomainEvents into outbox_messages
        // within the same transaction before base.SaveChangesAsync.
        // Steps: collect events -> insert OutboxMessage rows -> base.SaveChanges -> clear.
        // This ensures TaskStatusChangedDomainEvent is never lost if Notifications/RiskAI is down.

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in aggregates)
            entry.Entity.ClearDomainEvents();

        return result;
    }
}
