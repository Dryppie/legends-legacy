using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;
public sealed class CombatAcquisitionProgressConfiguration : IEntityTypeConfiguration<CombatAcquisitionProgress>
{
    public void Configure(EntityTypeBuilder<CombatAcquisitionProgress> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEOrdinaryProgress");
        builder.HasKey(x => new { x.CharacterId, x.PoolId });
        builder.Property(x => x.PoolId).HasMaxLength(160);
        builder.Property(x => x.Revision).IsConcurrencyToken();
        builder.Property(x => x.Plain).HasColumnType("jsonb").HasConversion(
            x => x == null ? null : EquipmentAcquisitionJson.Serialize(x),
            x => x == null ? null : EquipmentAcquisitionJson.Deserialize<PlainEquipmentCommitment>(x));
        builder.Property(x => x.Sigil).HasColumnType("jsonb").HasConversion(
            x => x == null ? null : EquipmentAcquisitionJson.Serialize(x),
            x => x == null ? null : EquipmentAcquisitionJson.Deserialize<SigilTargetCommitment>(x));
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class CombatAcquisitionSelectionReceiptConfiguration : IEntityTypeConfiguration<CombatAcquisitionSelectionReceipt>
{
    public void Configure(EntityTypeBuilder<CombatAcquisitionSelectionReceipt> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEOrdinarySelectionReceipts");
        builder.HasKey(x => new { x.CharacterId, x.OperationId });
        builder.Property(x => x.PoolId).HasMaxLength(160).IsRequired();
        builder.Property(x => x.DefinitionId).HasMaxLength(240);
        builder.Property(x => x.SigilFamilyId).HasMaxLength(160);
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
