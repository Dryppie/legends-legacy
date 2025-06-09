using Domain.Models.Items.Equipments.Slots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.EquipmentSlots;
public class EquipmentSlotConfiguration : IEntityTypeConfiguration<EquipmentSlot>
{
    public void Configure(EntityTypeBuilder<EquipmentSlot> builder)
    {
        builder.HasKey(es => new { es.EntityId, es.EquipmentSlotType });
    }
}