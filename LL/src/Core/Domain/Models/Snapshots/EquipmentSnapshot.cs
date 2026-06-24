using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;

namespace Domain.Models.Snapshots;

public sealed class EquipmentSnapshot
{
    public Guid Id { get; init; }
    public EquipmentSlotType Slot { get; init; }
    public Guid EquipmentInstanceId { get; init; }
    public string ItemBaseId { get; init; } = default!;
    public Rarity Rarity { get; init; }
    public int? Potential { get; init; }
    public int ItemXp { get; init; }
    public bool IsMasterpiece { get; init; }
    public bool IsLevelingItem { get; init; }

    public ICollection<InstanceAttributeModifier> InstanceModifiers { get; init; }
        = new List<InstanceAttributeModifier>();

    // Required by EF Core
    private EquipmentSnapshot() { }

    public static EquipmentSnapshot From(
        EquipmentSlotType slot,
        EquipmentInstance inst)
    {
        return new EquipmentSnapshot
        {
            Slot = slot,
            EquipmentInstanceId = inst.Id,
            ItemBaseId = inst.ItemBaseId,
            Rarity = inst.Rarity,
            Potential = inst.Potential,
            ItemXp = inst.ItemXp,
            IsMasterpiece = inst.IsMasterpiece,
            IsLevelingItem = inst.IsLevelingItem,
            InstanceModifiers = inst.InstanceModifiers?
                .Select(CloneInstanceModifier)
                .ToList() ?? []
        };
    }

    private static InstanceAttributeModifier CloneInstanceModifier(
        InstanceAttributeModifier modifier) =>
        new(modifier.AttributeType, modifier.Amount, modifier.ModifierType)
        {
            Id = Guid.NewGuid(),
            ItemInstanceId = modifier.ItemInstanceId
        };
}
