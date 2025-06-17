using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;
public class EquipmentInstanceConfiguration : ItemInstanceConfiguration, IEntityTypeConfiguration<EquipmentInstance>
{
    public void Configure(EntityTypeBuilder<EquipmentInstance> builder)
    {
        //builder.OwnsMany(ei => ei.InstanceModifiers);
    }
}