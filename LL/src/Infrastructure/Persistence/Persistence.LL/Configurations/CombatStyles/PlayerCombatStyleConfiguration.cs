using Domain.Models.CombatStyles;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.CombatStyles;

public sealed class PlayerCombatStyleConfiguration : IEntityTypeConfiguration<PlayerCombatStyle>
{
    public void Configure(EntityTypeBuilder<PlayerCombatStyle> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StyleId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SelectedFocusId).HasMaxLength(64);
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => new { x.CharacterId, x.StyleId }).IsUnique();
        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PlayerCombatStyles_Level", "\"Level\" >= 1 AND \"Level\" <= 50");
            t.HasCheckConstraint("CK_PlayerCombatStyles_Experience", "\"Experience\" >= 0");
        });
    }
}
