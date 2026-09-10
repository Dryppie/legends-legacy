using Domain.Models.CombatStyles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.CombatStyles;

public sealed class CharacterCombatStyleConfiguration : IEntityTypeConfiguration<CharacterCombatStyle>
{
    public void Configure(EntityTypeBuilder<CharacterCombatStyle> builder)
    {
        builder.HasKey(x => new { x.CharacterId, x.CombatStyleId });
        builder.Property(x => x.CombatStyleId).HasMaxLength(64);
        builder.Property(x => x.RefinementId).HasMaxLength(64);
        builder.Property(x => x.MasteredUpgradeId).HasMaxLength(64);
        // The retired manual choice keeps its existing database column; battles use Essence slot order.
        builder.Property(x => x.ChanneledPlayerEssenceId).HasColumnName("FocusPlayerEssenceId");
        builder.Property(x => x.UpgradeIds).HasColumnType("text[]");
        builder.Property(x => x.Level).HasDefaultValue(0);
        builder.HasOne(x => x.Character).WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("CharacterCombatStyles", table => table.HasCheckConstraint("CK_CharacterCombatStyles_Progression",
            "\"Level\" BETWEEN 0 AND 10 AND \"CurrentXp\" >= 0 AND (\"Level\" < 10 OR \"CurrentXp\" = 0)"));
    }
}
