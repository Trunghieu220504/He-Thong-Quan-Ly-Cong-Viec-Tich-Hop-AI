using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Outbox;

/// <summary>
/// Ánh xạ bảng <c>outbox_messages</c>. Module nào có outbox thì gọi
/// <c>modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration())</c> trong DbContext của mình
/// (bảng nằm trong schema mặc định của module đó, cùng transaction với dữ liệu nghiệp vụ).
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.EventType).HasMaxLength(OutboxMessage.EventTypeMaxLength).IsRequired();
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.OccurredAtUtc).IsRequired();
        builder.Property(m => m.ProcessedAtUtc);

        // Bộ xử lý nền chỉ quét các dòng chưa xử lý nên chỉ lập chỉ mục phần này (bảng lớn dần vẫn quét nhanh).
        builder.HasIndex(m => m.OccurredAtUtc)
            .HasDatabaseName("IX_outbox_messages_Pending")
            .HasFilter("\"ProcessedAtUtc\" IS NULL");
    }
}
