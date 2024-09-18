using Application.Interfaces.Services.LL;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class GatheringService : IGatheringService
{
    private readonly ILootService _lootService;
    private readonly ILootTableService _lootTableService;

    public GatheringService(ILootService lootService, ILootTableService lootTableService)
    {
        _lootService = lootService;
        _lootTableService = lootTableService;
    }

    public async Task<List<InventoryItem>> PerformGathering(Guid lootTableId, int actionsToPerform, CancellationToken cancellationToken)
    {
        // Find the kind of gathering the player does, check their levels, proceed to generate loot
        var totalLoot = new List<InventoryItem>();
        var lootTable = await _lootTableService.GetLootTableByIdAsync(lootTableId, cancellationToken);

        // Find other necessary data to generate loot
        // World buffs, personal buffs, and so on

        for (var i = actionsToPerform; i > 0; i--)
        {
            // Generate loot for every action, and add it to the total loot
            var loot = _lootService.GenerateGatheringLoot(lootTable, cancellationToken);
            if (loot.Count > 0)
            {
                totalLoot.AddRange(loot);
            }
        }
        return totalLoot;
    }
}