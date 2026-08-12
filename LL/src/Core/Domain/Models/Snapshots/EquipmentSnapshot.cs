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
    public string? BaseRecipeId { get; init; }
    public string? BlueprintId { get; init; }
    public Rarity Rarity { get; init; }
    public int? Potential { get; init; }
    public int ItemXp { get; init; }
    public bool IsMasterpiece { get; init; }
    public bool IsLevelingItem { get; init; }

    public ICollection<EquipmentAttributeModifierSnapshot> InstanceModifiers { get; init; }
        = new List<EquipmentAttributeModifierSnapshot>();

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
            BaseRecipeId = inst.BaseRecipeId,
            BlueprintId = inst.BlueprintId,
            Rarity = inst.Rarity,
            Potential = inst.Potential,
            ItemXp = inst.ItemXp,
            IsMasterpiece = inst.IsMasterpiece,
            IsLevelingItem = inst.IsLevelingItem,
            InstanceModifiers = inst.InstanceModifiers?
                .Select(EquipmentAttributeModifierSnapshot.From)
                .ToList() ?? []
        };
    }
}
