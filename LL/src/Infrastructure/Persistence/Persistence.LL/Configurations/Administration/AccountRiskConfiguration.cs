using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Administration;

public sealed class AccountRiskSnapshotConfiguration : IEntityTypeConfiguration<AccountRiskSnapshot>
{
    public void Configure(EntityTypeBuilder<AccountRiskSnapshot> builder)
    {
        builder.ToTable("AccountRiskSnapshots");
        builder.HasKey(x => x.AccountId);
        builder.Property(x => x.AccountLabel).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CharacterName).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.PrimarySignalType).HasConversion<string>().HasMaxLength(48);
        builder.Property(x => x.PrimaryReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SignalsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RelationshipsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.Severity, x.Score });
        builder.HasIndex(x => x.LastTriggeredAt);
        builder.HasIndex(x => x.EvaluatedAt);
        builder.HasIndex(x => x.PrimarySignalType);
    }
}

public sealed class AccountRiskHistoryConfiguration : IEntityTypeConfiguration<AccountRiskHistory>
{
    public void Configure(EntityTypeBuilder<AccountRiskHistory> builder)
    {
        builder.ToTable("AccountRiskHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.SignalsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.AccountId, x.EvaluatedAt });
    }
}

public sealed class AccountRiskInvestigationConfiguration : IEntityTypeConfiguration<AccountRiskInvestigation>
{
    public void Configure(EntityTypeBuilder<AccountRiskInvestigation> builder)
    {
        builder.ToTable("AccountRiskInvestigations");
        builder.HasKey(x => x.AccountId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.UpdatedBySubject).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.Status, x.UpdatedAt });
    }
}

public sealed class AccountRiskNoteConfiguration : IEntityTypeConfiguration<AccountRiskNote>
{
    public void Configure(EntityTypeBuilder<AccountRiskNote> builder)
    {
        builder.ToTable("AccountRiskNotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorSubject).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ActorDisplayName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4_000).IsRequired();
        builder.HasIndex(x => new { x.AccountId, x.CreatedAt });
    }
}
