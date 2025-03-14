using Domain.Models.Essences.EssenceSlots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Essences;
public class EssenceSlotConfiguration : IEntityTypeConfiguration<EssenceSlot>
{
    public void Configure(EntityTypeBuilder<EssenceSlot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.OccupiedEssence)
               .WithMany(e => e.EssenceSlots)
               .HasForeignKey(x => x.EssenceId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
