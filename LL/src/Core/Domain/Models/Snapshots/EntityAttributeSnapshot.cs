using Domain.Models.Attributes;

namespace Domain.Models.Snapshots;

public class EntityAttributeSnapshot
{
    public Guid CharacterSnapshotId { get; set; }
    public AttributeType AttributeType { get; set; }
    public float Value { get; set; }
}
