using Domain.Models.Dungeons.Runs;
using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Persistence.LL.Configurations.Dungeons;

public class DungeonRunConfiguration : IEntityTypeConfiguration<DungeonRun>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<DungeonRun> builder)
    {
        builder.HasIndex(x => x.CharacterId)
            .IsUnique();

        builder.Property(x => x.RowVersion)
            .IsConcurrencyToken();
        builder.Property(x => x.EquipmentCommitment).HasColumnName("ModelECommitment").HasColumnType("jsonb").HasConversion(
            x => Persistence.LL.Configurations.Items.EquipmentAcquisitionJson.Serialize(x),
            x => Persistence.LL.Configurations.Items.EquipmentAcquisitionJson.Deserialize<Domain.Models.Items.Equipments.Progression.DungeonEquipmentCommitment>(x));

        builder.Property(x => x.State)
            .HasColumnType("jsonb")
            .HasConversion(
                state => JsonSerializer.Serialize(state ?? new DungeonRunState(), JsonOptions),
                json => string.IsNullOrWhiteSpace(json)
                    ? new DungeonRunState()
                    : JsonSerializer.Deserialize<DungeonRunState>(json, JsonOptions) ?? new DungeonRunState())
            .Metadata.SetValueComparer(new ValueComparer<DungeonRunState>(
                (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
                state => JsonSerializer.Serialize(state, JsonOptions).GetHashCode(),
                state => JsonSerializer.Deserialize<DungeonRunState>(
                    JsonSerializer.Serialize(state, JsonOptions),
                    JsonOptions) ?? new DungeonRunState()));

        builder
            .HasMany(x => x.PendingRewards)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<CharacterSnapshot>()
            .WithMany()
            .HasForeignKey(x => x.CharacterSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
