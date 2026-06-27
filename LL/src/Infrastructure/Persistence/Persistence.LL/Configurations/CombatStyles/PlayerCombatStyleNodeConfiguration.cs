using Domain.Models.CombatStyles;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.CombatStyles;

public sealed class PlayerCombatStyleNodeConfiguration : IEntityTypeConfiguration<PlayerCombatStyleNode>
{
    public void Configure(EntityTypeBuilder<PlayerCombatStyleNode> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StyleId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NodeId).HasMaxLength(96).IsRequired();
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => new { x.CharacterId, x.StyleId, x.NodeId }).IsUnique();
        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PlayerCombatStyleNodes_Rank", "\"Rank\" >= 0");
        });
    }
}
