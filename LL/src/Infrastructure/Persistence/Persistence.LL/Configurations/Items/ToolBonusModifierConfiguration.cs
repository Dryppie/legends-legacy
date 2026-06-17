using Domain.Models.Items.Equipments.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public class ToolBonusModifierConfiguration : IEntityTypeConfiguration<ToolBonusModifier>
{
    public void Configure(EntityTypeBuilder<ToolBonusModifier> b)
    {
        b.HasKey(x => x.Id);

        b.Property(x => x.EquipmentBaseId)
            .HasMaxLength(128)
            .IsRequired();

        b.Property(x => x.ScopeId)
            .HasMaxLength(128);

        b.HasOne(x => x.EquipmentBase)
            .WithMany(x => x.ToolBonuses)
            .HasForeignKey(x => x.EquipmentBaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
