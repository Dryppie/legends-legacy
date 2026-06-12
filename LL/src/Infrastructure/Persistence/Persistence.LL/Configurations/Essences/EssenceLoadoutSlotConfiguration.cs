using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Essences;

public sealed class EssenceLoadoutSlotConfiguration : IEntityTypeConfiguration<EssenceLoadoutSlot>
{
    public void Configure(EntityTypeBuilder<EssenceLoadoutSlot> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.EssenceLoadoutId, x.SlotIndex }).IsUnique();
        builder.HasIndex(x => new { x.EssenceLoadoutId, x.PlayerEssenceId }).IsUnique();
        builder.HasOne(x => x.PlayerEssence)
            .WithMany()
            .HasForeignKey(x => x.PlayerEssenceId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.ToTable(t => t.HasCheckConstraint("CK_EssenceLoadoutSlots_SlotIndex", "\"SlotIndex\" >= 0 AND \"SlotIndex\" < 10"));
    }
}
