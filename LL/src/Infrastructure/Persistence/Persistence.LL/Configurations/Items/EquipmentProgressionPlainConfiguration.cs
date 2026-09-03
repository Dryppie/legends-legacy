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
        builder.Property(x => x.Baseline).HasColumnType("jsonb").HasConversion<EquipmentDataConverter>();
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class PlainEquipmentRecoveryReceiptConfiguration : IEntityTypeConfiguration<PlainEquipmentRecoveryReceipt>
{
    public void Configure(EntityTypeBuilder<PlainEquipmentRecoveryReceipt> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEPlainRecoveryReceipts");
        builder.HasKey(x => new { x.CharacterId, x.OperationId });
        builder.Property(x => x.Outcome).HasColumnType("jsonb").HasConversion(
            x => EquipmentAcquisitionJson.Serialize(x), x => EquipmentAcquisitionJson.Deserialize<PlainEquipmentRecovery>(x));
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
