using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;
public class EquipmentBaseConfiguration : ItemBaseConfiguration, IEntityTypeConfiguration<EquipmentBase>
{
    public void Configure(EntityTypeBuilder<EquipmentBase> b)
    {
        b.HasMany(e => e.AttributeModifiers)
         .WithOne()
         .HasForeignKey(m => m.ItemBaseId);
    }
}
