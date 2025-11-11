using Domain.Models.Attributes.Modifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.AttributeModifiers;
public class InstanceAttributeModifierConfiguration : IEntityTypeConfiguration<InstanceAttributeModifier>
{
    public void Configure(EntityTypeBuilder<InstanceAttributeModifier> b)
    {
        b.HasOne(m => m.ItemInstance)
         .WithMany()
         .HasForeignKey(m => m.ItemInstanceId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
