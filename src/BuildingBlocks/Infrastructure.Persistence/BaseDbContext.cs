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

        // Outbox: module cần sự kiện bền vững tự ghi OutboxMessage (xem Outbox/OutboxMessage.cs và
        // TaskManagement.Infrastructure.EfIntegrationEventOutbox) để dòng outbox nằm chung transaction với
        // lần SaveChanges này. Domain event ở đây vẫn chỉ được xóa sau khi lưu, chưa được chuyển thành outbox.

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entry in aggregates)
            entry.Entity.ClearDomainEvents();

        return result;
    }
}
