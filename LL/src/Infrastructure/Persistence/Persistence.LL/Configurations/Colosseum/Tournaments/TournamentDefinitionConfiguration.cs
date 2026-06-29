using Domain.Models.Colosseum.Tournaments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Colosseum.Tournaments;

public sealed class TournamentDefinitionConfiguration : IEntityTypeConfiguration<TournamentDefinition>
{
    public void Configure(EntityTypeBuilder<TournamentDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MinimumRankTier).HasMaxLength(80);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Enabled);
    }
}
