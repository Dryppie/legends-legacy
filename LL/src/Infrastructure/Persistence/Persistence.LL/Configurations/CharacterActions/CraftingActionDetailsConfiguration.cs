using Domain.Models.CharacterActions.CharacterActionDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.CharacterActions;
public class CraftingActionDetailsConfiguration : IEntityTypeConfiguration<CraftingActionDetails>
{
    public void Configure(EntityTypeBuilder<CraftingActionDetails> builder)
    {
        builder.HasMany(c => c.CraftingQueueItems)
            .WithOne()
            .HasForeignKey(cqi => cqi.CraftingActionDetailsId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
