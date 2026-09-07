using Domain.Models.Items.Equipments.Progression;

namespace Application.UseCases.Inventories.SelectionCrates;

public sealed record RandomEquipmentBoxReward(EquipmentRarity Rarity, int Quantity, int Tier = 1, int Rank = 0);

public static class RandomEquipmentBoxCatalog
{
    public const string UncommonItemBaseId = "item.uncommon_equipment_box";
    public const string OpenOptionId = "random";

    public static SelectionContainerDefinition Uncommon { get; } = new(
        UncommonItemBaseId,
        "Uncommon Equipment Box",
        "Equipment",
        [],
        new RandomEquipmentBoxReward(EquipmentRarity.Uncommon, 2));
}
