using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Prophecies;

public sealed class PlayerProphecyInstanceConfiguration : IEntityTypeConfiguration<PlayerProphecyInstance>
{
    public void Configure(EntityTypeBuilder<PlayerProphecyInstance> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProphecyDefinitionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RerolledFromDefinitionId).HasMaxLength(128);
        builder.Property(x => x.ObjectiveParameterSnapshotJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.ProgressJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.RewardSnapshotJson).HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(x => x.RowVersion).IsConcurrencyToken();

        builder.HasOne(x => x.ProphecyDefinition)
            .WithMany()
            .HasForeignKey(x => x.ProphecyDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PlayerId, x.CharacterId, x.Scope, x.PeriodStart, x.PeriodEnd });
        builder.HasIndex(x => new { x.PlayerId, x.CharacterId, x.Status });
        builder.HasIndex(x => new { x.PlayerId, x.CharacterId, x.Scope, x.PeriodStart, x.SlotType }).IsUnique();
    }
}
