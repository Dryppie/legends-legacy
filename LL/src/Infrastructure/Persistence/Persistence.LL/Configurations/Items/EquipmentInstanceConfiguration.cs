using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;
public class EquipmentInstanceConfiguration : ItemInstanceConfiguration, IEntityTypeConfiguration<EquipmentInstance>
{
    public void Configure(EntityTypeBuilder<EquipmentInstance> b)
    {
        // If you keep it as a separate entity (not owned):
        b.HasMany(e => e.InstanceModifiers)
         .WithOne()
         .HasForeignKey(m => m.ItemInstanceId)
         .HasPrincipalKey(e => e.Id);

        b.HasMany(e => e.ToolAffixes)
         .WithOne(x => x.EquipmentInstance)
         .HasForeignKey(x => x.EquipmentInstanceId)
         .HasPrincipalKey(e => e.Id);
    }
}
