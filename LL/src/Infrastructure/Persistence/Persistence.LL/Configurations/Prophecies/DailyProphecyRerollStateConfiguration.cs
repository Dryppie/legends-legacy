using Domain.Models.Prophecies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Prophecies;

public sealed class DailyProphecyRerollStateConfiguration : IEntityTypeConfiguration<DailyProphecyRerollState>
{
    public void Configure(EntityTypeBuilder<DailyProphecyRerollState> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ShownDefinitionIdsJson).HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.PlayerId, x.CharacterId, x.PeriodStart }).IsUnique();
    }
}
