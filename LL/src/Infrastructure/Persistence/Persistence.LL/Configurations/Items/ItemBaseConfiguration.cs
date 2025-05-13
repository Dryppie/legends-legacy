using Domain.Models.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;
public class ItemBaseConfiguration : IEntityTypeConfiguration<ItemBase>
{
    public void Configure(EntityTypeBuilder<ItemBase> builder)
    {
        
    }
}