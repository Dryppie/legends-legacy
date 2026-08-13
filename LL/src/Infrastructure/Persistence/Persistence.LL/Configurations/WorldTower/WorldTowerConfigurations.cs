using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.WorldTower;

public sealed class TowerFloorProgressConfiguration : IEntityTypeConfiguration<TowerFloorProgress>
{
    public void Configure(EntityTypeBuilder<TowerFloorProgress> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServerId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ServerId, x.FloorNumber }).IsUnique();
        builder.HasIndex(x => x.FirstClearAttemptId).IsUnique();
        builder.HasOne<TowerAttempt>()
            .WithOne()
            .HasForeignKey<TowerFloorProgress>(x => x.FirstClearAttemptId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TowerRallyConfiguration : IEntityTypeConfiguration<TowerRally>
{
    public void Configure(EntityTypeBuilder<TowerRally> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServerId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ServerId, x.FloorNumber, x.Mode, x.Status });
        builder.HasMany(x => x.Participants)
            .WithOne(x => x.TowerRally)
            .HasForeignKey(x => x.TowerRallyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Applications)
            .WithOne(x => x.TowerRally)
            .HasForeignKey(x => x.TowerRallyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Attempt)
            .WithOne(x => x.TowerRally)
            .HasForeignKey<TowerAttempt>(x => x.TowerRallyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TowerRallyApplicationConfiguration : IEntityTypeConfiguration<TowerRallyApplication>
{
    public void Configure(EntityTypeBuilder<TowerRallyApplication> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CharacterName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GuildName).HasMaxLength(64);
        builder.HasIndex(x => new { x.TowerRallyId, x.CharacterId }).IsUnique();
        builder.HasIndex(x => new { x.TowerRallyId, x.AccountId }).IsUnique();
        builder.HasIndex(x => new { x.TowerRallyId, x.Status });
        builder.HasIndex(x => x.CharacterId);
        builder.HasOne(x => x.CharacterSnapshot)
            .WithMany()
            .HasForeignKey(x => x.CharacterSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TowerRallyParticipantConfiguration : IEntityTypeConfiguration<TowerRallyParticipant>
{
    public void Configure(EntityTypeBuilder<TowerRallyParticipant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CharacterName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GuildName).HasMaxLength(64);
        builder.HasIndex(x => new { x.TowerRallyId, x.CharacterId }).IsUnique();
        builder.HasIndex(x => new { x.TowerRallyId, x.AccountId }).IsUnique();
        builder.HasIndex(x => x.CharacterId);
        builder.HasOne(x => x.CharacterSnapshot)
            .WithMany()
            .HasForeignKey(x => x.CharacterSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TowerAttemptConfiguration : IEntityTypeConfiguration<TowerAttempt>
{
    public void Configure(EntityTypeBuilder<TowerAttempt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServerId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SimulationLeaseOwner).HasMaxLength(128);
        builder.HasIndex(x => x.TowerRallyId).IsUnique();
        builder.HasIndex(x => new { x.ServerId, x.FloorNumber, x.Mode, x.Succeeded });
        builder.HasIndex(x => new { x.Status, x.SimulationLeaseUntil });
        builder.HasOne(x => x.Playback)
            .WithOne(x => x.TowerAttempt)
            .HasForeignKey<TowerCombatPlayback>(x => x.TowerAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TowerCombatPlaybackConfiguration : IEntityTypeConfiguration<TowerCombatPlayback>
{
    public void Configure(EntityTypeBuilder<TowerCombatPlayback> builder)
    {
        builder.HasKey(x => x.TowerAttemptId);
        builder.Property(x => x.TimelineJson).HasColumnType("jsonb");
        builder.Property(x => x.BundleHash).HasMaxLength(64);
        builder.Property(x => x.BundleContentType).HasMaxLength(128);
        builder.Property(x => x.BundleContentEncoding).HasMaxLength(32);
        builder.Property(x => x.DispatchLeaseOwner).HasMaxLength(128);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.NextFrameDueAt, x.LastPublishedSequence });
        builder.HasIndex(x => x.PlaybackEndsAt);
        builder.HasIndex(x => x.DispatchLeaseUntil);
        builder.HasOne(x => x.Artifact)
            .WithOne(x => x.Playback)
            .HasForeignKey<TowerCombatPlaybackArtifact>(x => x.TowerAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TowerCombatPlaybackArtifactConfiguration
    : IEntityTypeConfiguration<TowerCombatPlaybackArtifact>
{
    public void Configure(EntityTypeBuilder<TowerCombatPlaybackArtifact> builder)
    {
        builder.HasKey(x => x.TowerAttemptId);
        builder.Property(x => x.BundleBytes).HasColumnType("bytea").IsRequired();
    }
}

public sealed class TowerContributionConfiguration : IEntityTypeConfiguration<TowerContribution>
{
    public void Configure(EntityTypeBuilder<TowerContribution> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServerId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ServerId, x.FloorNumber, x.WeekKey });
        builder.HasIndex(x => new { x.CharacterId, x.FloorNumber, x.WeekKey });
    }
}

public sealed class TowerEchoClearConfiguration : IEntityTypeConfiguration<TowerEchoClear>
{
    public void Configure(EntityTypeBuilder<TowerEchoClear> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServerId).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ServerId, x.CharacterId, x.WeekKey }).IsUnique();
    }
}

public sealed class ServerUnlockConfiguration : IEntityTypeConfiguration<ServerUnlock>
{
    public void Configure(EntityTypeBuilder<ServerUnlock> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServerId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UnlockKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SourceSystem).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.ServerId, x.UnlockKey }).IsUnique();
    }
}
