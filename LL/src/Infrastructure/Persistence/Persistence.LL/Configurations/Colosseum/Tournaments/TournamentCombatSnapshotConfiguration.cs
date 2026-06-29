using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentCombatSnapshotConfiguration : IEntityTypeConfiguration<TournamentCombatSnapshot>
{
    public void Configure(EntityTypeBuilder<TournamentCombatSnapshot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SnapshotVersion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RankTierAtSnapshot).HasMaxLength(80).IsRequired();
        builder.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId);
        builder.HasOne(x => x.CharacterSnapshot).WithMany().HasForeignKey(x => x.CharacterSnapshotId);
        builder.HasIndex(x => new { x.TournamentId, x.CharacterId }).IsUnique();
        builder.HasIndex(x => x.CharacterId);
    }
}
