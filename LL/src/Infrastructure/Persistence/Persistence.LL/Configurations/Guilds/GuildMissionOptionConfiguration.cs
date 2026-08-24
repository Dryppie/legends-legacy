using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildMissionOptionConfiguration : IEntityTypeConfiguration<GuildMissionOption>
{
    public void Configure(EntityTypeBuilder<GuildMissionOption> builder)
    {
        builder
            .HasIndex(x => new { x.GuildId, x.WeekKey, x.MissionDefinitionId })
            .IsUnique();
        builder.HasIndex(x => new { x.GuildId, x.WeekKey, x.IsSelected });
    }
}
