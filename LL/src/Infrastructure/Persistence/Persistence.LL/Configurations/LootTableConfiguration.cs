using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations;
public class LootTableConfiguration : IEntityTypeConfiguration<LootTable>
{
    public void Configure(EntityTypeBuilder<LootTable> builder)
    {
        builder.HasKey(lt => lt.Id);
    }
}