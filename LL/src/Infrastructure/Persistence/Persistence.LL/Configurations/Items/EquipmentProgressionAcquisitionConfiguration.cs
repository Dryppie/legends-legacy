using System.Text.Json;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class EquipmentProtectionProgressConfiguration : IEntityTypeConfiguration<EquipmentProtectionProgress>
{
    public void Configure(EntityTypeBuilder<EquipmentProtectionProgress> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEProtectionProgress");
        builder.HasKey(x => new { x.CharacterId, x.PoolId });
        builder.Property(x => x.PoolId).HasMaxLength(160);
        builder.Property(x => x.SelectedDefinitionId).HasMaxLength(240);
        builder.Property(x => x.Revision).IsConcurrencyToken();
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class EquipmentProtectionReceiptConfiguration : IEntityTypeConfiguration<EquipmentProtectionReceipt>
{
    public void Configure(EntityTypeBuilder<EquipmentProtectionReceipt> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEProtectionReceipts");
        builder.HasKey(x => new { x.CharacterId, x.RunId });
        builder.Property(x => x.Outcome).HasColumnType("jsonb").HasConversion(
            x => EquipmentAcquisitionJson.Serialize(x), x => EquipmentAcquisitionJson.Deserialize<EquipmentProtectionOutcome>(x));
        builder.Property(x => x.ClaimedAtUtc).IsConcurrencyToken();
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class BaselineEquipmentRecoveryReceiptConfiguration : IEntityTypeConfiguration<BaselineEquipmentRecoveryReceipt>
{
    public void Configure(EntityTypeBuilder<BaselineEquipmentRecoveryReceipt> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEBaselineRecoveryReceipts");
        builder.HasKey(x => new { x.CharacterId, x.OperationId });
        builder.Property(x => x.Outcome).HasColumnType("jsonb").HasConversion(
            x => EquipmentAcquisitionJson.Serialize(x), x => EquipmentAcquisitionJson.Deserialize<BaselineEquipmentRecovery>(x));
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
public static class EquipmentAcquisitionJson
{
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
    public static T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Missing Equipment progression acquisition state.");
}
