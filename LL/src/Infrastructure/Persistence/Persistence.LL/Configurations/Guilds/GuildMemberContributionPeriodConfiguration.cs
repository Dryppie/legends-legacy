using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildMemberContributionPeriodConfiguration : IEntityTypeConfiguration<GuildMemberContributionPeriod>
{
    public void Configure(EntityTypeBuilder<GuildMemberContributionPeriod> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.CharacterId, x.PeriodType, x.PeriodKey }).IsUnique();
        builder.HasIndex(x => x.LastContributedAt);
    }
}
