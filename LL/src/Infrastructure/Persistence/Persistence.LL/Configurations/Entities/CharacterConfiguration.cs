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
            .HasMany(c => c.ColosseumMatches)
            .WithOne()
            .HasForeignKey(c => c.CharacterAId);
        builder
            .HasMany(c => c.ColosseumMatches)
            .WithOne()
            .HasForeignKey(c => c.CharacterBId);
    }
}