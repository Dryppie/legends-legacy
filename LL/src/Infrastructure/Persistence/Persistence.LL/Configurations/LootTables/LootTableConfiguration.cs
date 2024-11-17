using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.LootTables;
public class LootTableConfiguration : IEntityTypeConfiguration<LootTable>
{
    public void Configure(EntityTypeBuilder<LootTable> builder)
    {
        builder.HasMany(l => l.Entries)
            .WithOne()
            .OnDelete(DeleteBehavior.Restrict);
    }
}