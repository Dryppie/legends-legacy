using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Professions.Crafting;
public class CraftingQueueItemConfiguration : IEntityTypeConfiguration<CraftingQueueItem>
{
    public void Configure(EntityTypeBuilder<CraftingQueueItem> builder)
    {
        builder.Property(x => x.Position).IsRequired();
        builder.HasIndex(x => new { x.CraftingActionDetailsId, x.Position });
        builder.HasIndex(x => new { x.PausedForCharacterId, x.Position });

        builder
            .HasOne<Domain.Models.Entities.Characters.Character>()
            .WithMany()
            .HasForeignKey(x => x.PausedForCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_CraftingQueueItems_ActiveOrPaused",
            "(\"CraftingActionDetailsId\" IS NOT NULL AND \"PausedForCharacterId\" IS NULL) OR " +
            "(\"CraftingActionDetailsId\" IS NULL AND \"PausedForCharacterId\" IS NOT NULL)"));

        builder
            .HasOne(x => x.EquipmentInstance)
            .WithMany()
            .HasForeignKey(x => x.EquipmentInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
