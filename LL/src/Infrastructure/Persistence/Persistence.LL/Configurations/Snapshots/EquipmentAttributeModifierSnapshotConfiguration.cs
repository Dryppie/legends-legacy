using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Snapshots;

public sealed class EquipmentAttributeModifierSnapshotConfiguration
    : IEntityTypeConfiguration<EquipmentAttributeModifierSnapshot>
{
    public void Configure(EntityTypeBuilder<EquipmentAttributeModifierSnapshot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasOne<EquipmentSnapshot>()
            .WithMany(x => x.InstanceModifiers)
            .HasForeignKey(x => x.EquipmentSnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
