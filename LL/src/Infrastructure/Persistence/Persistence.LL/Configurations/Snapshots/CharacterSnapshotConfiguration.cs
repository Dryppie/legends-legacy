using System.Text.Json;
using Domain.Models.CombatStyles;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Snapshots;

public sealed class CharacterSnapshotConfiguration : IEntityTypeConfiguration<CharacterSnapshot>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CharacterSnapshot> builder)
    {
        builder.Property(x => x.CombatStyle)
            .HasColumnType("jsonb")
            .HasConversion(
                snapshot => snapshot == null ? null : JsonSerializer.Serialize(snapshot, JsonOptions),
                json => string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<CombatStyleSnapshot>(json, JsonOptions))
            .Metadata.SetValueComparer(new ValueComparer<CombatStyleSnapshot?>(
                (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
                snapshot => snapshot == null ? 0 : JsonSerializer.Serialize(snapshot, JsonOptions).GetHashCode(),
                snapshot => snapshot == null
                    ? null
                    : JsonSerializer.Deserialize<CombatStyleSnapshot>(
                        JsonSerializer.Serialize(snapshot, JsonOptions),
                        JsonOptions)));
    }
}
