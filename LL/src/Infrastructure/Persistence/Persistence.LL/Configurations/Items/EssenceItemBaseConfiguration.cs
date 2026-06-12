using Domain.Models.Items.EssenceItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Items;

public sealed class EssenceItemBaseConfiguration : IEntityTypeConfiguration<EssenceItemBase>
{
    public void Configure(EntityTypeBuilder<EssenceItemBase> builder)
    {
        builder.Property(x => x.EssenceDefinitionId).HasMaxLength(128);
        builder.Property(x => x.DismantleDustAmount).HasDefaultValue(1);
        builder.HasIndex(x => x.EssenceDefinitionId);
    }
}
