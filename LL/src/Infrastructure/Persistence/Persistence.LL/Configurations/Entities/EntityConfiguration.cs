using Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Entities;

public sealed class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_Entities_CharacterCurrencyBalances_NonNegative",
            "\"EntityType\" <> 1 OR (" +
            "\"Cinders\" IS NOT NULL AND \"Cinders\" >= 0 AND " +
            "\"Soulstones\" IS NOT NULL AND \"Soulstones\" >= 0 AND " +
            "\"FateEcho\" IS NOT NULL AND \"FateEcho\" >= 0 AND " +
            "\"SigilFragments\" IS NOT NULL AND \"SigilFragments\" >= 0 AND " +
            "\"GuildFavor\" IS NOT NULL AND \"GuildFavor\" >= 0 AND " +
            "\"TowerTokens\" IS NOT NULL AND \"TowerTokens\" >= 0 AND " +
            "\"RaidTrophies\" IS NOT NULL AND \"RaidTrophies\" >= 0)"));
    }
}
