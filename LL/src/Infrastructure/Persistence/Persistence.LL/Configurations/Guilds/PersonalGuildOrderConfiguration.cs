using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class PersonalGuildOrderConfiguration : IEntityTypeConfiguration<PersonalGuildOrder>
{
    public void Configure(EntityTypeBuilder<PersonalGuildOrder> builder)
    {
        builder
            .HasIndex(x => new
            {
                x.GuildId,
                x.CharacterId,
                x.PeriodType,
                x.PeriodKey,
                x.MissionDefinitionId
            })
            .IsUnique();
    }
}
