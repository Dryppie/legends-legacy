using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class PlainEquipmentEntitlementConfiguration : IEntityTypeConfiguration<PlainEquipmentEntitlement>
{
    public void Configure(EntityTypeBuilder<PlainEquipmentEntitlement> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEPlainEntitlements");
        builder.HasKey(x => new { x.CharacterId, x.DefinitionId, x.Tier });
        builder.Property(x => x.DefinitionId).HasMaxLength(240);
        builder.Property(x => x.Copies).IsConcurrencyToken();
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
