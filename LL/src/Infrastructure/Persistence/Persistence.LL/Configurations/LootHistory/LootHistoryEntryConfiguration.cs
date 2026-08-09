using Domain.Models.LootHistory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.LootHistory;

public sealed class LootHistoryEntryConfiguration : IEntityTypeConfiguration<LootHistoryEntry>
{
    public void Configure(EntityTypeBuilder<LootHistoryEntry> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ItemSnapshotJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Source)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasOne(x => x.Character)
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CharacterId, x.ReceivedAt });
    }
}
