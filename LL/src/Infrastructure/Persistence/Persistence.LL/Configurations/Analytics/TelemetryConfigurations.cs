using Domain.Models.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Analytics;

public sealed class AccountActivityDayConfiguration : IEntityTypeConfiguration<AccountActivityDay>
{
    public void Configure(EntityTypeBuilder<AccountActivityDay> builder)
    {
        builder.ToTable("AccountActivityDays");
        builder.HasKey(x => new { x.AccountId, x.ActivityDateUtc });
        builder.HasIndex(x => x.ActivityDateUtc);
        builder.HasOne<Domain.Models.Users.AppUser>().WithMany()
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DungeonAttemptHistoryConfiguration : IEntityTypeConfiguration<DungeonAttemptHistory>
{
    public void Configure(EntityTypeBuilder<DungeonAttemptHistory> builder)
    {
        builder.ToTable("DungeonAttemptHistories");
        builder.HasKey(x => x.RunId);
        builder.Property(x => x.DungeonDefinitionId).HasMaxLength(128);
        builder.Property(x => x.Outcome).HasMaxLength(24);
        builder.HasIndex(x => x.StartedAtUtc);
        builder.HasIndex(x => x.FinishedAtUtc);
        builder.HasOne<Domain.Models.Entities.Characters.Character>().WithMany()
            .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DailyTelemetryReportConfiguration : IEntityTypeConfiguration<DailyTelemetryReport>
{
    public void Configure(EntityTypeBuilder<DailyTelemetryReport> builder)
    {
        builder.ToTable("DailyTelemetryReports");
        builder.HasKey(x => x.ReportDateUtc);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
    }
}
