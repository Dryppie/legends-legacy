using Domain.Models.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Attributes;
public class EntityAttributeConfiguration : IEntityTypeConfiguration<EntityAttribute>
{
    public void Configure(EntityTypeBuilder<EntityAttribute> builder)
    {
        builder.HasKey(ea => new { ea.EntityId, ea.AttributeType });
    }
}