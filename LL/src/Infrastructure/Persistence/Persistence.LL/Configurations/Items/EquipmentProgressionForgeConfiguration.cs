using System.Text.Json;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class ForgeReceiptConfiguration : IEntityTypeConfiguration<ForgeReceipt>
{
    public void Configure(EntityTypeBuilder<ForgeReceipt> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEForgeReceipts");
        builder.HasKey(x => new { x.CharacterId, x.OperationId });
        builder.Property(x => x.RequestFingerprint).HasMaxLength(64);
        builder.Property(x => x.Outcome).HasColumnType("jsonb")
            .HasConversion(x => Serialize(x), json => Deserialize(json));
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
    private static string Serialize(ForgeOutcome outcome) => JsonSerializer.Serialize(outcome);
    private static ForgeOutcome Deserialize(string json) => JsonSerializer.Deserialize<ForgeOutcome>(json)
        ?? throw new InvalidOperationException("Missing Forge operation receipt.");
}

public sealed class LearnedEquipmentStyleConfiguration : IEntityTypeConfiguration<LearnedEquipmentStyle>
{
    public void Configure(EntityTypeBuilder<LearnedEquipmentStyle> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelECharacterStyles");
        builder.HasKey(x => new { x.CharacterId, x.StyleId });
        builder.Property(x => x.StyleId).HasMaxLength(160);
        builder.Property(x => x.FreeApplicationOperationId).IsConcurrencyToken();
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}
