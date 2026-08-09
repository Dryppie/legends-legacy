using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Regions;
public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(x => x.RequiredActiveQuestId).HasMaxLength(160);
        builder.Property(x => x.RequiredCompletedQuestId).HasMaxLength(160);
    }
}
