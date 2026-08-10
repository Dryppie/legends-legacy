using Domain.Models.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Transfers;

public sealed class PlayerTransferRecordConfiguration : IEntityTypeConfiguration<PlayerTransferRecord>
{
    public void Configure(EntityTypeBuilder<PlayerTransferRecord> builder)
    {
        builder.ToTable("PlayerTransferHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.SenderCharacterName).HasMaxLength(26).IsRequired();
        builder.Property(x => x.RecipientCharacterName).HasMaxLength(26).IsRequired();
        builder.Property(x => x.AssetId).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AssetName).HasMaxLength(160).IsRequired();

        builder.HasIndex(x => new { x.SenderAccountId, x.OccurredAt });
        builder.HasIndex(x => new { x.RecipientAccountId, x.OccurredAt });
        builder.HasIndex(x => new { x.SenderCharacterId, x.OccurredAt });
        builder.HasIndex(x => new { x.RecipientCharacterId, x.OccurredAt });
        builder.HasIndex(x => new { x.RecipientAccountId, x.SenderAccountId, x.OccurredAt });
        builder.HasIndex(x => new { x.Kind, x.OccurredAt });
        builder.HasIndex(x => x.SourceItemInstanceId);
        builder.HasIndex(x => x.DestinationItemInstanceId);
    }
}
