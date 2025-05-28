using Domain.Models.Soulstones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Soulstones;
public class CharacterSoulstoneUpgradeConfiguration : IEntityTypeConfiguration<CharacterSoulstoneUpgrade>
{
    public void Configure(EntityTypeBuilder<CharacterSoulstoneUpgrade> builder)
    {
        builder.HasKey(e => new { e.CharacterId, e.SoulstoneUpgradeDefinitionId });

        builder.HasOne(x => x.Character)
             .WithMany(p => p.CharacterSoulstoneUpgrades)
             .HasForeignKey(x => x.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
    }
}
