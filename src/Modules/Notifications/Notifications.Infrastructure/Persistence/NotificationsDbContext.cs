using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : BaseDbContext(options)
{
    public const string Schema = "notifications";

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("Notifications", Schema);
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventId).IsRequired();
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
            builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            builder.Property(x => x.TaskId).IsRequired();
            builder.Property(x => x.ProjectId).IsRequired();
            builder.Property(x => x.IsRead).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();

            // Unique (EventId, UserId) đảm bảo idempotent khi nhận lại sự kiện
            builder.HasIndex(x => new { x.EventId, x.UserId })
                .IsUnique()
                .HasDatabaseName("IX_Notifications_EventId_UserId");

            builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
                .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");
        });
    }
}