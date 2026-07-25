using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Snapshots;

public sealed class EquipmentAttributeModifierSnapshot
{
    public Guid Id { get; init; }
    public Guid EquipmentSnapshotId { get; init; }
    public AttributeType AttributeType { get; init; }
    public float Amount { get; init; }
    public ModifierType ModifierType { get; init; }

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
