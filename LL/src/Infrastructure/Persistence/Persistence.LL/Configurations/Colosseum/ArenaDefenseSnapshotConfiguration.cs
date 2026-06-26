using Domain.Models.Colosseum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum;

public sealed class ArenaDefenseSnapshotConfiguration : IEntityTypeConfiguration<ArenaDefenseSnapshot>
{
    public void Configure(EntityTypeBuilder<ArenaDefenseSnapshot> builder)
    {
        builder.HasKey(x => x.CharacterId);

        builder.HasOne(x => x.CharacterSnapshot)
            .WithOne()
            .HasForeignKey<ArenaDefenseSnapshot>(x => x.CharacterSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CharacterSnapshotId).IsUnique();
        builder.Property(x => x.LoadoutHash).HasMaxLength(128);
    }
}
