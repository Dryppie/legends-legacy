using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Loadouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.EquipmentSlots;

public sealed class EquipmentLoadoutConfiguration : IEntityTypeConfiguration<EquipmentLoadout>
{
    public void Configure(EntityTypeBuilder<EquipmentLoadout> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.CharacterId, x.Name }).IsUnique();
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Slots).WithOne().HasForeignKey(x => x.EquipmentLoadoutId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EquipmentLoadoutSlotConfiguration : IEntityTypeConfiguration<EquipmentLoadoutSlot>
{
    public void Configure(EntityTypeBuilder<EquipmentLoadoutSlot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EquipmentLoadoutId, x.SlotType }).IsUnique();
        builder.HasOne(x => x.EquipmentInstance).WithMany().HasForeignKey(x => x.EquipmentInstanceId).OnDelete(DeleteBehavior.SetNull);
    }
}
