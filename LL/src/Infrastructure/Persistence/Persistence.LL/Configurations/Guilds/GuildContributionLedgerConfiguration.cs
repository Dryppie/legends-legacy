using Domain.Models.Guilds.Missions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Guilds;

public class GuildContributionLedgerConfiguration : IEntityTypeConfiguration<GuildContributionLedger>
{
    public void Configure(EntityTypeBuilder<GuildContributionLedger> builder)
    {
        builder.HasIndex(x => new { x.GuildId, x.CharacterId, x.OccurredAt });
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}
