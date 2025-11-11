using Domain.Models.Attributes.Modifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.AttributeModifiers;
public class ItemAttributeModifierConfiguration : IEntityTypeConfiguration<ItemAttributeModifier>
{
    public void Configure(EntityTypeBuilder<ItemAttributeModifier> b)
    {
        b.HasOne(m => m.ItemBase)
         .WithMany()
         .HasForeignKey(m => m.ItemBaseId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
