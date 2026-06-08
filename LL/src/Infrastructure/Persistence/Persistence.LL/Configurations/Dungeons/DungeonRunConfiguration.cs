using Domain.Models.Dungeons.Runs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Dungeons;

public class DungeonRunConfiguration : IEntityTypeConfiguration<DungeonRun>
{
    public void Configure(EntityTypeBuilder<DungeonRun> builder)
    {
        builder
            .HasMany(x => x.PendingRewards)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
