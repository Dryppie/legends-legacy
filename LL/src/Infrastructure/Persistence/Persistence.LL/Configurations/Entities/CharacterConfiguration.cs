using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Entities;
public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder
            .HasIndex(c => c.UserId);

        builder
            .Property(c => c.NormalizedName)
            .HasMaxLength(80);

        builder
            .Property(c => c.RaidTrophies)
            .HasDefaultValue(0L);

        builder
            .HasIndex(c => c.NormalizedName)
            .IsUnique()
            .HasFilter("\"EntityType\" = 1 AND \"NormalizedName\" IS NOT NULL");

        builder
            .Property(c => c.EquippedTitleDisplayPosition)
            .HasConversion<int>();

        builder
            .HasOne(c => c.EquippedTitleDefinition)
            .WithMany()
            .HasForeignKey(c => c.EquippedTitleDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasMany(c => c.ColosseumMatches)
            .WithOne()
            .HasForeignKey(c => c.CharacterAId);
        builder
            .HasMany(c => c.ColosseumMatches)
            .WithOne()
            .HasForeignKey(c => c.CharacterBId);

        //builder.HasOne(c => c.Guild)
        //    .WithMany(g => g.Members)
        //    .HasForeignKey(c => c.GuildId)
        //    .OnDelete(DeleteBehavior.SetNull);
    }
}
