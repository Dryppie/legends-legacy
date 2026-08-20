using Domain.Models.Raids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Raids;

public sealed class RaidRunConfiguration : IEntityTypeConfiguration<RaidRun>
{
    public void Configure(EntityTypeBuilder<RaidRun> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RaidBossId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DefinitionHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DefinitionSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SimulationLeaseOwner).HasMaxLength(128);
        builder.Property(x => x.ReinforcementPenalty).HasPrecision(8, 6);
        builder.Property(x => x.WardBreak).HasPrecision(8, 6);
        builder.Property(x => x.BossHealthRemainingPercent).HasPrecision(8, 4);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.RaidBossId, x.Status, x.SignupClosesAt });
        builder.HasIndex(x => new { x.Status, x.SimulationLeaseUntil });
        builder.HasIndex(x => new { x.LeaderCharacterId, x.Status });
        builder.HasMany(x => x.Signups).WithOne(x => x.RaidRun).HasForeignKey(x => x.RaidRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.LaneResults).WithOne(x => x.RaidRun).HasForeignKey(x => x.RaidRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Playbacks).WithOne(x => x.RaidRun).HasForeignKey(x => x.RaidRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ParticipantResults).WithOne(x => x.RaidRun).HasForeignKey(x => x.RaidRunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.RewardClaims).WithOne(x => x.RaidRun).HasForeignKey(x => x.RaidRunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RaidSignupConfiguration : IEntityTypeConfiguration<RaidSignup>
{
    public void Configure(EntityTypeBuilder<RaidSignup> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CharacterName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LoadoutHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.RaidRunId, x.CharacterId }).IsUnique();
        builder.HasIndex(x => new { x.RaidRunId, x.AccountId }).IsUnique();
        builder.HasIndex(x => new { x.RaidRunId, x.Lane, x.WingSlotIndex }).IsUnique();
        builder.HasIndex(x => x.CharacterId);
        builder.HasOne(x => x.CharacterSnapshot).WithMany().HasForeignKey(x => x.CharacterSnapshotId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RaidLaneResultConfiguration : IEntityTypeConfiguration<RaidLaneResult>
{
    public void Configure(EntityTypeBuilder<RaidLaneResult> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SurvivingHostileHealthFraction).HasPrecision(8, 6);
        builder.Property(x => x.DerivedModifier).HasPrecision(8, 6);
        builder.HasIndex(x => new { x.RaidRunId, x.Lane }).IsUnique();
        builder.HasOne(x => x.Playback)
            .WithOne()
            .HasForeignKey<RaidLaneResult>(x => x.PlaybackId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class RaidPlaybackConfiguration : IEntityTypeConfiguration<RaidPlayback>
{
    public void Configure(EntityTypeBuilder<RaidPlayback> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BundleHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BundleContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.BundleContentEncoding).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.RaidRunId, x.Lane }).IsUnique();
        builder.HasOne(x => x.Artifact)
            .WithOne(x => x.Playback)
            .HasForeignKey<RaidPlaybackArtifact>(x => x.RaidPlaybackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RaidPlaybackArtifactConfiguration : IEntityTypeConfiguration<RaidPlaybackArtifact>
{
    public void Configure(EntityTypeBuilder<RaidPlaybackArtifact> builder)
    {
        builder.HasKey(x => x.RaidPlaybackId);
        builder.Property(x => x.BundleBytes).HasColumnType("bytea").IsRequired();
    }
}

public sealed class RaidParticipantResultConfiguration : IEntityTypeConfiguration<RaidParticipantResult>
{
    public void Configure(EntityTypeBuilder<RaidParticipantResult> builder)
    {
        builder.HasKey(x => new { x.RaidRunId, x.CharacterId });
        builder.Property(x => x.ContributionScore).HasPrecision(12, 8);
        builder.Property(x => x.PayoutMultiplier).HasPrecision(8, 6);
    }
}

public sealed class RaidRewardClaimConfiguration : IEntityTypeConfiguration<RaidRewardClaim>
{
    public void Configure(EntityTypeBuilder<RaidRewardClaim> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RaidBossId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PendingItemsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.RaidRunId, x.CharacterId }).IsUnique();
        builder.HasIndex(x => new { x.RaidBossId, x.CharacterId, x.WeekKey })
            .IsUnique()
            .HasFilter("\"WasReduced\" = false");
    }
}

public sealed class RaidTrophyPurchaseConfiguration : IEntityTypeConfiguration<RaidTrophyPurchase>
{
    public void Configure(EntityTypeBuilder<RaidTrophyPurchase> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RaidBossId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.VendorItemId).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.CharacterId, x.VendorItemId, x.WeekKey });
        builder.HasIndex(x => new { x.CharacterId, x.VendorItemId });
    }
}

public sealed class RaidPowerRecommendationCacheEntryConfiguration
    : IEntityTypeConfiguration<RaidPowerRecommendationCacheEntry>
{
    public void Configure(EntityTypeBuilder<RaidPowerRecommendationCacheEntry> builder)
    {
        builder.HasKey(x => new { x.RaidBossId, x.Tier });
        builder.Property(x => x.RaidBossId).HasMaxLength(128);
        builder.Property(x => x.DefinitionHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecommendationJson).HasColumnType("jsonb").IsRequired();
    }
}
