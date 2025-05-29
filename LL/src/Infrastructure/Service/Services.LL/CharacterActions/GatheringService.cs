using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Inventories.Events;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Professions;
using MediatR;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class GatheringService : IGatheringService
{
    private readonly ILootService _lootService;
    private readonly ILootTableService _lootTableService;
    private readonly IPublisher _publisher;
    private readonly IProfessionService _professionService;
    private readonly ILevelingService _levelingService;

    public GatheringService(ILootService ls, ILootTableService lts, IPublisher p, IProfessionService ps, ILevelingService lvlS)
    {
        _lootService = ls;
        _lootTableService = lts;
        _publisher = p;
        _professionService = ps;
        _levelingService = lvlS;
    }

    public async Task<GatheringSession> PerformGatheringAsync(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken)
    {
        var startedAt = characterAction.UpdatedAt;
        var now = DateTimeOffset.UtcNow;

        characterAction.UpdatedAt += TimeSpan.FromSeconds(6 * actionsToPerform);
        var actionDetails = (characterAction.ActionDetails as GatheringActionDetails)!;

        // Find the kind of gathering the player does, check their levels, proceed to generate loot
        var totalLoot = new List<InventoryItem>();
        var lootTable = await _lootTableService.GetLootTableByIdAsync(actionDetails.LootTableId, cancellationToken);
        var gatheringSummary = new GatheringSummary()
        {
            ProfessionType = actionDetails.ProfessionType,
        };
        // Find other necessary data to generate loot
        // World buffs, personal buffs, and so on

        for (var i = actionsToPerform; i > 0; i--)
        {
            gatheringSummary.TotalActions++;
            // Generate loot for every action, and add it to the total loot
            var loot = _lootService.GenerateGatheringLootAsync(lootTable, cancellationToken);
            if (loot.Count > 0)
            {
                totalLoot.AddRange(loot);
            }
            gatheringSummary.TotalExperience++;
        }

        gatheringSummary.Loot = totalLoot
            .GroupBy(ii => ii.ItemInstance.ItemBaseId)
            .Select(g => new InventoryItem
            {
                ItemInstance = g.First().ItemInstance,
                Quantity = g.Sum(ii => ii.Quantity)
            })
            .ToList();

        var gatheringSession = new GatheringSession()
        {
            From = startedAt,
            To = now,
            GatheringSummary = gatheringSummary,
        };

        var durationInSeconds = 6 * actionsToPerform;
        var soulstonesEarned = _lootService.GenerateSoulstoneLoot(durationInSeconds, 0, 0);
        // TODO: Publish event to handle earning soulstones
        // TODO: Perhaps publish event with nothing but a durationInSeconds, and a CharacterGuid. The event can then handle checking whether SS drops

        await ProcessLootAsync(characterAction.CharacterId, totalLoot, cancellationToken);
        await UpdateCharacterProfessionsAsync(characterAction.CharacterId, gatheringSummary, cancellationToken);

        return gatheringSession;
    }

    private async Task UpdateCharacterProfessionsAsync(Guid characterId, GatheringSummary gatheringSummary, CancellationToken cancellationToken)
    {
        if (gatheringSummary.TotalExperience == 0) return;

        var professions = await _professionService.GetProfessionsAsync(characterId, cancellationToken);
        var profession = professions.FirstOrDefault(p => p.ProfessionType.Equals(gatheringSummary.ProfessionType));

        if (profession == null) return;
        profession.Experience += gatheringSummary.TotalExperience;

        await _levelingService.UpdateProfessionLevel(profession);

        await _professionService.UpdateProfessionLevelAsync(professions, cancellationToken);
    }

    private async Task ProcessLootAsync(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        // Implement how to update the character or game state with the loot
        // For example, updating the character inventory
        //await _InventoryService.AddLootAsync(loot, cancellationToken);
        await _publisher.Publish(new LootGeneratedEvent(characterId, loot), cancellationToken);
    }
}