using Domain.Models.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Analytics;

public sealed class ItemizationObservationConfiguration : IEntityTypeConfiguration<ItemizationObservationRow>
{
    public void Configure(EntityTypeBuilder<ItemizationObservationRow> builder)
    {
        builder.ToTable("ItemizationObservations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(64);
        builder.Property(x => x.Kind).HasMaxLength(32);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.CharacterId, x.OccurredAtUtc });
        builder.HasOne<Domain.Models.Entities.Characters.Character>().WithMany()
            .HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ItemizationDailyReportConfiguration : IEntityTypeConfiguration<ItemizationDailyReport>
{
    public void Configure(EntityTypeBuilder<ItemizationDailyReport> builder)
    {
        builder.ToTable("ItemizationDailyReports");
        builder.HasKey(x => x.Day);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb");
    }
}
