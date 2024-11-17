using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.LootTables;
public class LootTableEntryConfiguration : IEntityTypeConfiguration<LootTableEntry>
{
    public void Configure(EntityTypeBuilder<LootTableEntry> builder)
    {
        builder.HasKey(lte => lte.Id);
    }
}