using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.Extensions.Options;

namespace Services.LL.Professions.Craftings;

public class ItemPotentialService : IItemPotentialService
{
    private readonly CraftingBalanceOptions _options;

    public ItemPotentialService(IOptions<CraftingBalanceOptions>? options = null)
    {
        _options = options?.Value ?? new CraftingBalanceOptions();
    }

    public int CalculateStartingPotential(EquipmentBase equipment, int targetTier, ItemQuality quality, int masteryLevel, int craftingLevel)
    {
        var basePotential = 100 + (Math.Max(targetTier, 1) * 100);
        var slotWeight = _options.GetPotentialSlotWeight(equipment.EquipmentType);
        var qualityMultiplier = _options.GetPotentialQualityMultiplier(quality);
        var masteryBonus = Math.Clamp(masteryLevel, 0, 100) * 10;
        var craftingLevelMultiplier = 10 * craftingLevel;

        return (int)Math.Round((basePotential * slotWeight * qualityMultiplier) + masteryBonus + craftingLevelMultiplier);
    }
}
