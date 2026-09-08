using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Inventories.SelectionCrates;

public sealed record RandomEquipmentBoxReward(
    EquipmentRarity Rarity,
    int Quantity,
    int Tier = 1,
    int Rank = 0,
    IReadOnlyList<EquipmentType>? EquipmentTypes = null);

public static class RandomEquipmentBoxCatalog
{
    public const string UncommonItemBaseId = "item.uncommon_equipment_box";
    public const string ArmorChestItemBaseId = "item.armor_chest";
    public const string JewelryChestItemBaseId = "item.jewelry_chest";
    public const string OpenOptionId = "random";

    public static SelectionContainerDefinition Uncommon { get; } = new(
        UncommonItemBaseId,
        "Uncommon Equipment Box",
        "Equipment",
        [],
        new RandomEquipmentBoxReward(EquipmentRarity.Uncommon, 2));

    public static SelectionContainerDefinition ArmorChest { get; } = new(
        ArmorChestItemBaseId,
        "Armor Chest",
        "Armor",
        [],
        new RandomEquipmentBoxReward(EquipmentRarity.Common, 1,
            EquipmentTypes: [EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs]));

    public static SelectionContainerDefinition JewelryChest { get; } = new(
        JewelryChestItemBaseId,
        "Jewelry Chest",
        "Jewelry",
        [],
        new RandomEquipmentBoxReward(EquipmentRarity.Common, 1,
            EquipmentTypes: [EquipmentType.Ring, EquipmentType.Necklace, EquipmentType.Relic]));
}
