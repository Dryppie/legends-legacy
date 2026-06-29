using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildMissionContributionConfiguration : IEntityTypeConfiguration<GuildMissionContribution>
{
    public void Configure(EntityTypeBuilder<GuildMissionContribution> builder)
    {
        builder.HasIndex(x => new { x.GuildMissionInstanceId, x.CharacterId }).IsUnique();
    }
}
