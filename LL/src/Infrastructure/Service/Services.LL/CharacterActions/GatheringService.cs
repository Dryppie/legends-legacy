using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Inventories.Events;
using Application.UseCases.Soulstones.Events;
using Domain.Helpers.Constants;
using Domain.Models.Bonuses;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Inventories;
using MediatR;
using Services.LL.Extensions;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class GatheringService : IGatheringService
{
    private readonly ILootService _lootService;
    private readonly ILootTableService _lootTableService;
    private readonly IPublisher _publisher;
    private readonly IProfessionService _professionService;
    private readonly ILevelingService _levelingService;
    private readonly ISoulstoneUpgradeService _soulstoneUpgradeService;
    private readonly IBonusService _bonusService;

    public GatheringService(ILootService ls, ILootTableService lts, IPublisher p, IProfessionService ps, ILevelingService lvlS,  IBonusService bs)
    {
        _lootService = ls;
        _lootTableService = lts;
        _publisher = p;
        _professionService = ps;
        _levelingService = lvlS;
        _bonusService = bs;
    }

    public async Task<GatheringSession> PerformGatheringAsync(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var rng = Random.Shared;
        var startedAt = characterAction.UpdatedAt;

        characterAction.UpdatedAt += TimeSpan.FromSeconds(6 * actionsToPerform);
        var actionDetails = (characterAction.ActionDetails as GatheringActionDetails)!;

        // Find the kind of gathering the player does, check their levels, proceed to generate loot
        var totalLoot = new List<InventoryItem>();
        var lootTable = await _lootTableService.GetLootTableByIdAsync(actionDetails.LootTableId, cancellationToken);
        var gatheringSummary = new GatheringSummary()
        {
            ProfessionType = actionDetails.ProfessionType,
        };

        var factors = await _bonusService.GetAggregatedAsync(characterAction.CharacterId, now, cancellationToken);

        // Find other necessary data to generate loot
        // World buffs, personal buffs, and so on
        double soulstoneDropRate = factors.Get(BonusKind.SoulstoneDropRate);
        double soulstoneDoubleDropChance = factors.Get(BonusKind.SoulstoneDoubleDropChance);
        double gatheringDoubleDropChance = factors.Get(BonusKind.GatheringDoubleDropChance);
        double gatheringDoubleExpChance = factors.Get(BonusKind.GatheringDoubleExpChance);

        for (var i = actionsToPerform; i > 0; i--)
        {
            gatheringSummary.TotalActions++;
            // Generate loot for every action, and add it to the total loot
            var loot = _lootService.GenerateGatheringLootAsync(lootTable, cancellationToken);
            if (loot.Count > 0)
            {
                if (rng.NextDouble() < (gatheringDoubleDropChance / 100))
                    foreach (var drop in loot)
                        drop.Quantity *= 2; // Double loot based on rng roll

                totalLoot.AddRange(loot);
            }

            gatheringSummary.TotalExperience++;
            if (rng.NextDouble() < (gatheringDoubleExpChance / 100))
                gatheringSummary.TotalExperience++;
        }

        // This is made purely to display the loot in the frontend
        gatheringSummary.Loot = totalLoot
            .GroupBy(ii => ii.ItemInstance.ItemBaseId)
            .Select(g => new InventoryItem
            {
                ItemInstance = g.First().ItemInstance,
                Quantity = g.Sum(ii => ii.Quantity)
            })
            .ToList();


        gatheringSummary.TotalSoulstones = await ProcessSoulstoneDrops(characterAction.CharacterId, actionsToPerform, soulstoneDropRate, soulstoneDoubleDropChance, cancellationToken);
        await ProcessLootAsync(characterAction.CharacterId, totalLoot, cancellationToken);
        await UpdateCharacterProfessionsAsync(characterAction.CharacterId, gatheringSummary, cancellationToken);

        var gatheringSession = new GatheringSession()
        {
            From = startedAt,
            To = now,
            GatheringSummary = gatheringSummary,
        };
        return gatheringSession;
    }

    private async Task<int> ProcessSoulstoneDrops(Guid characterId, int actionsToPerform, double dropRate, double doubleDropChance, CancellationToken cancellationToken)
    {
        var durationInSeconds = 6 * actionsToPerform;
        var soulstonesEarned = _lootService.GenerateSoulstoneLoot(durationInSeconds, dropRate, doubleDropChance);
        if (soulstonesEarned < 1) return 0;

        await _publisher.Publish(new SoulstoneDropEvent(characterId, soulstonesEarned), cancellationToken);
        return soulstonesEarned;
    }

    private async Task UpdateCharacterProfessionsAsync(Guid characterId, GatheringSummary gatheringSummary, CancellationToken cancellationToken)
    {
        if (gatheringSummary.TotalExperience == 0) return;

        var professions = await _professionService.GetProfessionsAsync(characterId, cancellationToken);
        var profession = professions.FirstOrDefault(p => p.ProfessionType.Equals(gatheringSummary.ProfessionType));

        if (profession == null) return;
        profession.Experience += gatheringSummary.TotalExperience;

        await _levelingService.UpdateProfessionLevel(profession, cancellationToken);

        await _professionService.UpdateProfessionLevelAsync([profession], cancellationToken);
    }

    private async Task ProcessLootAsync(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        if (loot.Count == 0) return;
        await _publisher.Publish(new LootGeneratedEvent(characterId, loot), cancellationToken);
    }
}