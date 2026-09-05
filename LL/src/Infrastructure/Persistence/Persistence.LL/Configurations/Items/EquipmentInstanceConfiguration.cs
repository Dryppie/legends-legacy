using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;
public class EquipmentInstanceConfiguration : ItemInstanceConfiguration, IEntityTypeConfiguration<EquipmentInstance>
{
    public void Configure(EntityTypeBuilder<EquipmentInstance> b)
    {
        b.Property(e => e.Version).IsRowVersion();
        b.Property(e => e.ProgressionData).HasColumnName("ModelEData").HasConversion<EquipmentDataConverter>().HasColumnType("jsonb");

        // If you keep it as a separate entity (not owned):
        b.HasMany(e => e.InstanceModifiers)
         .WithOne()
         .HasForeignKey(m => m.ItemInstanceId)
         .HasPrincipalKey(e => e.Id);
    }
}
