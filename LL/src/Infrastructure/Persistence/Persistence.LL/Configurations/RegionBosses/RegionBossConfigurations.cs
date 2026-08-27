using Domain.Models.RegionBosses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.RegionBosses;

public sealed class RegionBossEventConfiguration : IEntityTypeConfiguration<RegionBossEvent>
{
    public void Configure(EntityTypeBuilder<RegionBossEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RegionBossDefinitionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DefinitionHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DefinitionSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.RegionBossDefinitionId, x.SignupStartsAtUtc }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.SignupStartsAtUtc });
        builder.HasIndex(x => new { x.Status, x.SignupClosesAtUtc });
        builder.HasIndex(x => new { x.Status, x.PlaybackEndsAtUtc });
        builder.HasMany(x => x.Signups).WithOne(x => x.Event).HasForeignKey(x => x.RegionBossEventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Runs).WithOne(x => x.Event).HasForeignKey(x => x.RegionBossEventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.RewardGrants).WithOne(x => x.Event).HasForeignKey(x => x.RegionBossEventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RegionBossSignupConfiguration : IEntityTypeConfiguration<RegionBossSignup>
{
    public void Configure(EntityTypeBuilder<RegionBossSignup> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CharacterName).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.RegionBossEventId, x.CharacterId }).IsUnique();
        builder.HasIndex(x => new { x.RegionBossEventId, x.AccountId }).IsUnique();
        builder.HasIndex(x => new { x.RegionBossRunId, x.PartySlot }).IsUnique();
        builder.HasIndex(x => x.CharacterId);
        builder.HasOne(x => x.CharacterSnapshot).WithMany().HasForeignKey(x => x.CharacterSnapshotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Run).WithMany(x => x.Members).HasForeignKey(x => x.RegionBossRunId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RegionBossRunConfiguration : IEntityTypeConfiguration<RegionBossRun>
{
    public void Configure(EntityTypeBuilder<RegionBossRun> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SimulationLeaseOwner).HasMaxLength(128);
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.RegionBossEventId, x.PartyNumber }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.SimulationLeaseUntil });
        builder.HasIndex(x => new { x.RegionBossEventId, x.HighestLevelDefeated, x.CurrentBossProgressBasisPoints });
        builder.HasMany(x => x.ParticipantResults).WithOne(x => x.Run).HasForeignKey(x => x.RegionBossRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Playback).WithOne(x => x.Run).HasForeignKey<RegionBossPlayback>(x => x.RegionBossRunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RegionBossParticipantResultConfiguration : IEntityTypeConfiguration<RegionBossParticipantResult>
{
    public void Configure(EntityTypeBuilder<RegionBossParticipantResult> builder) =>
        builder.HasKey(x => new { x.RegionBossRunId, x.CharacterId });
}

public sealed class RegionBossPlaybackConfiguration : IEntityTypeConfiguration<RegionBossPlayback>
{
    public void Configure(EntityTypeBuilder<RegionBossPlayback> builder)
    {
        builder.HasKey(x => x.RegionBossRunId);
        builder.Property(x => x.BundleHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BundleContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.BundleContentEncoding).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Artifact).WithOne(x => x.Playback).HasForeignKey<RegionBossPlaybackArtifact>(x => x.RegionBossRunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RegionBossPlaybackArtifactConfiguration : IEntityTypeConfiguration<RegionBossPlaybackArtifact>
{
    public void Configure(EntityTypeBuilder<RegionBossPlaybackArtifact> builder)
    {
        builder.HasKey(x => x.RegionBossRunId);
        builder.Property(x => x.BundleBytes).HasColumnType("bytea").IsRequired();
    }
}

public sealed class RegionBossRewardGrantConfiguration : IEntityTypeConfiguration<RegionBossRewardGrant>
{
    public void Configure(EntityTypeBuilder<RegionBossRewardGrant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RegionBossDefinitionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RewardKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RewardSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.RegionBossEventId, x.CharacterId, x.RewardKey }).IsUnique();
        builder.HasIndex(x => new { x.CharacterId, x.Status });
    }
}
