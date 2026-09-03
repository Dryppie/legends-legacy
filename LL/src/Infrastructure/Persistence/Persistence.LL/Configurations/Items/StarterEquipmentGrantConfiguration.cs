using System.Text.Json;
using Domain.Models.Entities.Characters;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class StarterEquipmentGrantConfiguration : IEntityTypeConfiguration<StarterEquipmentGrant>
{
    public void Configure(EntityTypeBuilder<StarterEquipmentGrant> builder)
    {
        // Preserve the existing storage contract while using gameplay names in code.
        builder.ToTable("ModelEStarterGrants");
        builder.HasKey(x => new { x.CharacterId, x.Kind });
        builder.HasOne<Character>().WithMany().HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.Equipment).HasColumnType("jsonb")
            .HasConversion(x => Serialize(x), json => Deserialize(json));
    }

    private static string Serialize(IReadOnlyList<EquipmentData> data) => JsonSerializer.Serialize(data);
    private static IReadOnlyList<EquipmentData> Deserialize(string json) =>
        Array.AsReadOnly(JsonSerializer.Deserialize<EquipmentData[]>(json)
            ?? throw new InvalidOperationException("Missing starter award descriptors."));
}
