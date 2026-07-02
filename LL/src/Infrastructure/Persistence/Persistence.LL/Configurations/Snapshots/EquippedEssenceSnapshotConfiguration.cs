using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Snapshots;

public sealed class EquippedEssenceSnapshotConfiguration : IEntityTypeConfiguration<EquippedEssenceSnapshot>
{
    public void Configure(EntityTypeBuilder<EquippedEssenceSnapshot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EssenceDefinitionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NativeRegion).HasDefaultValue(1);
        builder.Property(x => x.PotentialTier).HasDefaultValue(1);
        builder.HasIndex(x => new { x.CharacterSnapshotId, x.SlotIndex }).IsUnique();
    }
}
