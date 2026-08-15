using Domain.Models.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Economy;

public sealed class EconomyLedgerEntryConfiguration : IEntityTypeConfiguration<EconomyLedgerEntry>
{
    public void Configure(EntityTypeBuilder<EconomyLedgerEntry> builder)
    {
        builder.ToTable("EconomyLedger");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();
        builder.Property(x => x.AssetType)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(x => x.AssetId).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AssetName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(160).IsRequired();
        builder.Property(x => x.RiskDecision).HasMaxLength(64);
        builder.Property(x => x.RuleHits).HasMaxLength(2_000);

        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.SenderAccountId, x.OccurredAt });
        builder.HasIndex(x => new { x.RecipientAccountId, x.OccurredAt });
        builder.HasIndex(x => new { x.SenderCharacterId, x.OccurredAt });
        builder.HasIndex(x => new { x.RecipientCharacterId, x.OccurredAt });
        builder.HasIndex(x => new { x.GuildId, x.OccurredAt });
        builder.HasIndex(x => new { x.EventType, x.OccurredAt });
        builder.HasIndex(x => new { x.AssetId, x.OccurredAt });
        builder.HasIndex(x => x.ReferenceId);
        builder.HasIndex(x => x.SourceItemInstanceId);
        builder.HasIndex(x => x.DestinationItemInstanceId);
    }
}
