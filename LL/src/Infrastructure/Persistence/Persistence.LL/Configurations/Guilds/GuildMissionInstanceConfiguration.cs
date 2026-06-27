using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildMissionInstanceConfiguration : IEntityTypeConfiguration<GuildMissionInstance>
{
    public void Configure(EntityTypeBuilder<GuildMissionInstance> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.WeekKey });
        builder.HasMany(x => x.Contributions)
            .WithOne(x => x.GuildMissionInstance)
            .HasForeignKey(x => x.GuildMissionInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
