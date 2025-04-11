using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;
public class ItemInstanceConfiguration : IEntityTypeConfiguration<ItemInstance>
{
    public void Configure(EntityTypeBuilder<ItemInstance> builder)
    {
        builder.HasKey(i => i.Id);
    }
}