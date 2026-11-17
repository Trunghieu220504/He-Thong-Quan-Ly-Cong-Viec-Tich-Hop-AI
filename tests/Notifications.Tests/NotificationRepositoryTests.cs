using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Persistence;
using Npgsql;

namespace Notifications.Tests;

public sealed class NotificationRepositoryTests
{
    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation, "IX_Notifications_EventId_UserId", true)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "PK_Notifications", false)]
    [InlineData(PostgresErrorCodes.NotNullViolation, "IX_Notifications_EventId_UserId", false)]
    [InlineData(PostgresErrorCodes.StringDataRightTruncation, null, false)]
    public async Task TryAdd_OnlyIgnoresTheEventRecipientUniqueViolation(
        string sqlState, string? constraint, bool duplicate)
    {
        var error = new DbUpdateException("Save failed", new PostgresException(
            "Database error", "ERROR", "ERROR", sqlState, constraintName: constraint));
        var interceptor = new SaveFailure(error);
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor).Options;
        await using var db = new NotificationsDbContext(options);
        var repository = new NotificationRepository(db, NullLogger<NotificationRepository>.Instance);
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(),
            NotificationTypes.TaskAssigned, "Title", "Message", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        if (duplicate)
        {
            Assert.False(await repository.TryAddIdempotentAsync(notification));
            Assert.Equal(EntityState.Detached, db.Entry(notification).State);
        }
        else
        {
            var thrown = await Assert.ThrowsAsync<DbUpdateException>(
                () => repository.TryAddIdempotentAsync(notification));
            Assert.Same(error, thrown);
        }
    }

    private sealed class SaveFailure(DbUpdateException error) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) => throw error;
    }
}
