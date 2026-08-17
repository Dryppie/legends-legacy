using Domain.Models.Administration;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Administration;

public sealed class AccountRestrictionConfiguration : IEntityTypeConfiguration<AccountRestriction>
{
    public void Configure(EntityTypeBuilder<AccountRestriction> builder)
    {
        builder.ToTable("AccountRestrictions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RestrictionType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1_000).IsRequired();
        builder.Property(x => x.InternalNotes).HasMaxLength(4_000);
        builder.Property(x => x.CreatedBySubject).HasMaxLength(320).IsRequired();
        builder.Property(x => x.RevokedBySubject).HasMaxLength(320);
        builder.Property(x => x.RevocationReason).HasMaxLength(1_000);
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AccountId, x.RestrictionType, x.RevokedAt, x.ExpiresAt });
        builder.HasIndex(x => x.CreatedAt);
    }
}
