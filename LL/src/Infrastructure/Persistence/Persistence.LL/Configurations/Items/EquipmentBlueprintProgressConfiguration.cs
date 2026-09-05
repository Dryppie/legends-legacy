using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class EquipmentBlueprintProgressConfiguration : IEntityTypeConfiguration<EquipmentBlueprintProgress>
{
    public void Configure(EntityTypeBuilder<EquipmentBlueprintProgress> builder)
    {
        builder.ToTable("EquipmentBlueprintProgress");
        builder.HasKey(x => new { x.CharacterId, x.FamilyId });
        builder.Property(x => x.FamilyId).HasMaxLength(100);
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
