using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Essences;

public sealed class PlayerEssenceConfiguration : IEntityTypeConfiguration<PlayerEssence>
{
    public void Configure(EntityTypeBuilder<PlayerEssence> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EssenceDefinitionId).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => new { x.CharacterId, x.EssenceDefinitionId }).IsUnique();
        builder.Property(x => x.Level).HasDefaultValue(1);
        builder.Property(x => x.AbsorbedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
