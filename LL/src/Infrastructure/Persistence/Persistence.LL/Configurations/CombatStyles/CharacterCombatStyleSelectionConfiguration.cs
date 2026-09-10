using Domain.Models.CombatStyles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.CombatStyles;

public sealed class CharacterCombatStyleSelectionConfiguration : IEntityTypeConfiguration<CharacterCombatStyleSelection>
{
    public void Configure(EntityTypeBuilder<CharacterCombatStyleSelection> builder)
    {
        builder.HasKey(x => x.CharacterId);
        builder.Property(x => x.CombatStyleId).HasMaxLength(64);
        builder.Property(x => x.RefinementId).HasMaxLength(64);
        builder.Property(x => x.MasteredUpgradeId).HasMaxLength(64);
        // The retired manual choice keeps its existing database column; current saves clear it.
        builder.Property(x => x.ChanneledPlayerEssenceId).HasColumnName("FocusPlayerEssenceId");
        builder.Property(x => x.UpgradeIds).HasColumnType("text[]");
        builder.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
