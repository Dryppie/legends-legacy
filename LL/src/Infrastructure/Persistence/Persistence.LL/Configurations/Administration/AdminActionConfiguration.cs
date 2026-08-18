using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Administration;

public sealed class AdminActionConfiguration : IEntityTypeConfiguration<AdminAction>
{
    public void Configure(EntityTypeBuilder<AdminAction> builder)
    {
        builder.ToTable("AdminActions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.Permission).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ActorSubject).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1_000).IsRequired();
        builder.Property(x => x.InternalNotes).HasMaxLength(4_000);
        builder.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RiskLevel)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.RiskLevel, x.OccurredAt });
        builder.HasIndex(x => new { x.TargetAccountId, x.OccurredAt });
        builder.HasIndex(x => new { x.TargetCharacterId, x.OccurredAt });
        builder.HasIndex(x => x.TargetResourceId);
    }
}
