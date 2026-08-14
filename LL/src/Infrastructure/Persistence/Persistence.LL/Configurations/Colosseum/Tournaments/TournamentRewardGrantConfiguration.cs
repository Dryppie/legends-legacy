using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentRewardGrantConfiguration : IEntityTypeConfiguration<TournamentRewardGrant>
{
    public void Configure(EntityTypeBuilder<TournamentRewardGrant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RewardKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.CatalystSelectionCaches).HasDefaultValue(0);
        builder.Property(x => x.BlueprintSelectionBoxes).HasDefaultValue(0);
        builder.Property(x => x.SigilFragments).HasDefaultValue(0);
        builder.HasOne(x => x.Tournament).WithMany().HasForeignKey(x => x.TournamentId);
        builder.HasIndex(x => new { x.TournamentId, x.CharacterId, x.RewardKey }).IsUnique();
        builder.HasIndex(x => new { x.CharacterId, x.Status });
    }
}
