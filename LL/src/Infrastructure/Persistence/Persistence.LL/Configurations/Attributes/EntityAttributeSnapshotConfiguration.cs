using Domain.Models.Snapshots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Attributes;

public class EntityAttributeSnapshotConfiguration : IEntityTypeConfiguration<EntityAttributeSnapshot>
{
    public void Configure(EntityTypeBuilder<EntityAttributeSnapshot> builder)
    {
        builder.HasKey(ea => new { ea.CharacterSnapshotId, ea.AttributeType });
    }
}