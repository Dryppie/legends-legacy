using System.Text.Json;
using Domain.Models.CombatStyles;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Snapshots;

public sealed class CharacterSnapshotConfiguration : IEntityTypeConfiguration<CharacterSnapshot>
{
    public void Configure(EntityTypeBuilder<CharacterSnapshot> builder)
    {
        var property = builder.Property(x => x.CombatStyle)
            .HasColumnType("jsonb")
            .HasConversion(
                value => Serialize(value),
                value => Deserialize(value));
        property.Metadata.SetValueComparer(new ValueComparer<CombatStyleSnapshot?>(
            (left, right) => Serialize(left) == Serialize(right),
            value => Serialize(value) == null ? 0 : Serialize(value)!.GetHashCode(),
            value => Deserialize(Serialize(value))));
    }

    private static string? Serialize(CombatStyleSnapshot? value) => value is null
        ? null : JsonSerializer.Serialize(value);
    private static CombatStyleSnapshot? Deserialize(string? value) => string.IsNullOrWhiteSpace(value)
        ? null : JsonSerializer.Deserialize<CombatStyleSnapshot>(value);
}
