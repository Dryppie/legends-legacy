using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Snapshots;

public sealed class EquipmentAttributeModifierSnapshot
{
    public Guid Id { get; init; }
    public Guid EquipmentSnapshotId { get; init; }
    public AttributeType AttributeType { get; set; }
    public float Amount { get; set; }
    public ModifierType ModifierType { get; set; }

    private EquipmentAttributeModifierSnapshot() { }

    public static EquipmentAttributeModifierSnapshot From(InstanceAttributeModifier modifier) => new()
    {
        Id = Guid.NewGuid(),
        AttributeType = modifier.AttributeType,
        Amount = modifier.Amount,
        ModifierType = modifier.ModifierType
    };

    public InstanceAttributeModifier ToInstanceModifier(Guid itemInstanceId) =>
        new(AttributeType, Amount, ModifierType)
        {
            Id = Guid.NewGuid(),
            ItemInstanceId = itemInstanceId
        };
}
