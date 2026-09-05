using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.Equipments.Progression;

namespace Domain.Models.Snapshots;

public sealed class EquipmentSnapshot
{
    public Guid Id { get; init; }
    public EquipmentData? ProgressionData { get; init; }
    public EquipmentSlotType Slot { get; init; }
    public Guid EquipmentInstanceId { get; init; }
    public string ItemBaseId { get; init; } = default!;
    public Rarity Rarity { get; init; }
    public ItemQuality Quality { get; init; }
    public int Tier { get; init; } = 1;

    public ICollection<EquipmentAttributeModifierSnapshot> InstanceModifiers { get; set; }
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
            ProgressionData = inst.ProgressionData,
            Rarity = inst.Rarity,
            Quality = inst.Quality,
            Tier = inst.Tier,
            InstanceModifiers = inst.InstanceModifiers?
                .Select(EquipmentAttributeModifierSnapshot.From)
                .ToList() ?? []
        };
    }
}
