using System.Text.Json;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class EquipmentUpgradeReceiptConfiguration
    : IEntityTypeConfiguration<EquipmentUpgradeReceipt>
{
    public void Configure(EntityTypeBuilder<EquipmentUpgradeReceipt> builder)
    {
        builder.ToTable("EquipmentUpgradeReceipts");
        builder.HasKey(receipt => new { receipt.CharacterId, receipt.OperationId });
        builder.Property(receipt => receipt.RequestFingerprint).HasMaxLength(64);
        builder.Property(receipt => receipt.Outcome)
            .HasColumnType("jsonb")
            .HasConversion(
                outcome => SerializeOutcome(outcome),
                json => DeserializeOutcome(json));
        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(receipt => receipt.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static string SerializeOutcome(EquipmentUpgradeOutcome outcome) =>
        JsonSerializer.Serialize(outcome);

    private static EquipmentUpgradeOutcome DeserializeOutcome(string json) =>
        JsonSerializer.Deserialize<EquipmentUpgradeOutcome>(json)
        ?? throw new InvalidOperationException("Missing equipment-upgrade receipt outcome.");
}
