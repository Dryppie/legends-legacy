using Domain.Models.Essences;
using Domain.Models.Entities.Characters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Essences;

public sealed class EssenceLoadoutConfiguration : IEntityTypeConfiguration<EssenceLoadout>
{
    public void Configure(EntityTypeBuilder<EssenceLoadout> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => new { x.CharacterId, x.Name }).IsUnique();
        builder.HasOne<Character>()
            .WithMany(x => x.EssenceLoadouts)
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Slots)
            .WithOne(x => x.EssenceLoadout)
            .HasForeignKey(x => x.EssenceLoadoutId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
