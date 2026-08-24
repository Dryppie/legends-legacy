using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions.Crafting.V2;

namespace Domain.Models.Snapshots;

public sealed class EquipmentSnapshot
{
    public Guid Id { get; init; }
    public EquipmentSlotType Slot { get; init; }
    public Guid EquipmentInstanceId { get; init; }
    public string ItemBaseId { get; init; } = default!;
    public string? BaseRecipeId { get; init; }
    public string? BlueprintId { get; init; }
    public string? EquipmentSetId { get; init; }
    public Rarity Rarity { get; init; }
    public ItemQuality Quality { get; init; }
    public int Tier { get; init; } = 1;
    public int StatModelVersion { get; set; } = EquipmentStatBudgetCatalog.LegacyBalanceVersion;
    public int? Potential { get; init; }
    public int ItemXp { get; init; }
    public bool IsMasterpiece { get; init; }
    public bool IsLevelingItem { get; init; }

    public ICollection<EquipmentAttributeModifierSnapshot> InstanceModifiers { get; set; }
        = new List<EquipmentAttributeModifierSnapshot>();

    // Required by EF Core
    private EquipmentSnapshot() { }

    public static EquipmentSnapshot From(
        EquipmentSlotType slot,
        EquipmentInstance inst)
    {
        EquipmentStatModelMigrator.MigrateToCurrent(inst);
        return new EquipmentSnapshot
        {
            Slot = slot,
            EquipmentInstanceId = inst.Id,
            ItemBaseId = inst.ItemBaseId,
            BaseRecipeId = inst.BaseRecipeId,
            BlueprintId = inst.BlueprintId,
            EquipmentSetId = inst.EquipmentSetId,
            Rarity = inst.Rarity,
            Quality = inst.Quality,
            Tier = inst.Tier,
            StatModelVersion = inst.StatModelVersion,
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
