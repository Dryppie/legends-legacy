using Domain.Models.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;
public class GuildBuildingUpgradeConfiguration : IEntityTypeConfiguration<GuildBuildingUpgrade>
{
    public void Configure(EntityTypeBuilder<GuildBuildingUpgrade> builder)
    {
        builder.HasKey(e => new { e.GuildId, e.BuildingUpgradeDefinitionId});

        builder.HasOne(x => x.Guild)
             .WithMany(p => p.GuildBuildingUpgrades)
             .HasForeignKey(x => x.GuildId)
             .OnDelete(DeleteBehavior.Cascade);
    }
}
