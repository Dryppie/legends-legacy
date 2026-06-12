using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Essences;

public sealed class MonsterResonanceConfiguration : IEntityTypeConfiguration<CreatureResonance>
{
    public void Configure(EntityTypeBuilder<CreatureResonance> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatureId).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => new { x.CharacterId, x.CreatureId }).IsUnique();
    }
}
