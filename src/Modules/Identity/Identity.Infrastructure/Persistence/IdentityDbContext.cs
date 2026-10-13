using Identity.Domain;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : BaseDbContext
{
    public const string Schema = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
