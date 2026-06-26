using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Prophecies;

public sealed class WeeklyRevelationProgressConfiguration : IEntityTypeConfiguration<WeeklyRevelationProgress>
{
    public void Configure(EntityTypeBuilder<WeeklyRevelationProgress> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.PlayerId, x.CharacterId, x.PeriodStart }).IsUnique();
    }
}
