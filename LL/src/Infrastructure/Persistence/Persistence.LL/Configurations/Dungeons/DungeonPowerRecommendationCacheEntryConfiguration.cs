using Domain.Models.Dungeons.PowerRatings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Dungeons;

public sealed class DungeonPowerRecommendationCacheEntryConfiguration
    : IEntityTypeConfiguration<DungeonPowerRecommendationCacheEntry>
{
    public void Configure(EntityTypeBuilder<DungeonPowerRecommendationCacheEntry> builder)
    {
        builder.HasKey(x => x.DungeonId);

        builder.Property(x => x.DungeonId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.DungeonContentHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RecommendationJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();
    }
}
