using Domain.Models.Dungeons.Runs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Dungeons;

public class RunRewardConfiguration : IEntityTypeConfiguration<RunReward>
{
    public void Configure(EntityTypeBuilder<RunReward> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ItemId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired();

        builder.Property(x => x.Source)
            .IsRequired();
    }
}
