using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class EquipmentMigrationReceiptConfiguration : IEntityTypeConfiguration<EquipmentMigrationReceipt>
{
    public void Configure(EntityTypeBuilder<EquipmentMigrationReceipt> builder)
    {
        builder.ToTable("EquipmentMigrationReceipts");
        builder.HasKey(x => x.OperationId);
        builder.HasIndex(x => new { x.ItemId, x.Revision }).IsUnique().HasFilter("\"RolledBackAtUtc\" IS NULL");
        builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
        builder.Property(x => x.AfterJson).HasColumnType("jsonb");
        builder.Property(x => x.ChoiceResultJson).HasColumnType("jsonb");
        builder.Property(x => x.SourceHash).HasMaxLength(64);
        builder.Property(x => x.ResultHash).HasMaxLength(64);
        builder.Property(x => x.ActorId).HasMaxLength(256);
        builder.Property(x => x.ChosenDefinitionId).HasMaxLength(256);
    }
}
