using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Administration;

public sealed class SupportCaseConfiguration : IEntityTypeConfiguration<SupportCase>
{
    public void Configure(EntityTypeBuilder<SupportCase> b)
    {
        b.ToTable("SupportCases"); b.HasKey(x => x.Id);
        b.Property(x => x.CharacterName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Title).HasMaxLength(160).IsRequired();
        b.Property(x => x.Category).HasMaxLength(40).IsRequired();
        b.Property(x => x.ExternalReference).HasMaxLength(300);
        b.Property(x => x.Resolution).HasMaxLength(4000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.CharacterId, x.UpdatedAt });
        b.HasIndex(x => new { x.Status, x.UpdatedAt });
    }
}

public sealed class SupportCaseEntryConfiguration : IEntityTypeConfiguration<SupportCaseEntry>
{
    public void Configure(EntityTypeBuilder<SupportCaseEntry> b)
    {
        b.ToTable("SupportCaseEntries"); b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(24);
        b.Property(x => x.ActorSubject).HasMaxLength(320).IsRequired();
        b.Property(x => x.ActorDisplayName).HasMaxLength(320).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.EvidenceReference).HasMaxLength(500);
        b.Property(x => x.LinkedSource).HasMaxLength(16);
        b.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.CaseId, x.Sequence }).IsUnique();
        b.HasIndex(x => x.LinkedOperationId);
        b.HasOne<SupportCase>().WithMany().HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
