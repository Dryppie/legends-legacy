using Domain.Models.Entities.Characters;
using Domain.Models.Nobility;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Nobility;

public sealed class NobilityConfiguration : IEntityTypeConfiguration<NobilityMembership>,
    IEntityTypeConfiguration<NobilityCoverage>, IEntityTypeConfiguration<SignetIssuance>,
    IEntityTypeConfiguration<SignetUnit>, IEntityTypeConfiguration<SignetMovement>,
    IEntityTypeConfiguration<SignetRedemption>, IEntityTypeConfiguration<NobilityDailyGrant>
{
    public void Configure(EntityTypeBuilder<NobilityMembership> b)
    {
        b.ToTable("NobilityMemberships");
        b.HasKey(x => x.AccountId);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => x.NextDailyRewardAt);
        b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Character>().WithMany().HasForeignKey(x => x.RewardCharacterId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Coverage).WithOne().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<NobilityCoverage> b)
    {
        b.ToTable("NobilityCoverage", t => t.HasCheckConstraint("CK_NobilityCoverage_Duration",
            "\"EndsAt\" > \"StartsAt\" AND \"CalendarMonths\" > 0"));
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.AccountId, x.StartsAt }).IsUnique();
        b.HasIndex(x => x.EndsAt);
    }

    public void Configure(EntityTypeBuilder<SignetIssuance> b)
    {
        b.ToTable("SignetIssuances", t => t.HasCheckConstraint("CK_SignetIssuance_Quantity", "\"Quantity\" > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ActorSubject).HasMaxLength(200);
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.HasIndex(x => new { x.AccountId, x.Origin });
        b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<SignetUnit> b)
    {
        b.ToTable("SignetUnits", t => t.HasCheckConstraint("CK_SignetUnit_Location",
            "(\"State\" = 1 AND \"ListingId\" IS NOT NULL AND \"RedemptionId\" IS NULL) OR " +
            "(\"State\" = 2 AND \"ListingId\" IS NULL AND \"RedemptionId\" IS NOT NULL) OR " +
            "(\"State\" IN (0, 3) AND \"ListingId\" IS NULL AND \"RedemptionId\" IS NULL)"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.IssuanceId, x.Ordinal }).IsUnique();
        b.HasIndex(x => new { x.OwnerCharacterId, x.State });
        b.HasIndex(x => x.ListingId);
        b.HasOne<SignetIssuance>().WithMany().HasForeignKey(x => x.IssuanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Character>().WithMany().HasForeignKey(x => x.OwnerCharacterId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<SignetMovement> b)
    {
        b.ToTable("SignetMovements");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UnitId, x.OperationId, x.Kind }).IsUnique();
        b.HasOne<SignetUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<SignetRedemption> b)
    {
        b.ToTable("SignetRedemptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.AccountId, x.RedeemedAt });
        b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<NobilityDailyGrant> b)
    {
        b.ToTable("NobilityDailyGrants");
        b.HasKey(x => new { x.AccountId, x.GameDate });
        b.HasOne<NobilityMembership>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
