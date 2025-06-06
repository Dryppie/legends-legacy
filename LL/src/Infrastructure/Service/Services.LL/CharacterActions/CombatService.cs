using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.CharacterActions;
using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Inventories.Events;
using Application.UseCases.Soulstones.Events;
using Domain.Helpers.Constants;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Slots;
using MediatR;
using Services.LL.Combat;
using Services.LL.Extensions;
using Services.LL.Interfaces;

namespace Services.LL.CharacterActions;
public class CombatService : ICombatService
{
    private readonly IEntityService _entityService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly ILevelingService _levelingService;
    private readonly ILootService _lootService;
    private readonly ISpawningService _spawningService;
    private readonly IPublisher _publisher;
    private readonly ISoulstoneUpgradeService _soulstoneUpgradeService;

    
    // Create a static SoulstoneUpgradeClass that expands on this, containing all bonuses

    public CombatService(IEntityService es, ICombatSetupService cps, ILevelingService lvlS, ILootService ls, ISpawningService ss, IPublisher p, ISoulstoneUpgradeService sus)
    {
        _entityService = es;
        _combatSetupService = cps;
        _levelingService = lvlS;
        _lootService = ls;
        _spawningService = ss;
        _publisher = p;
        _soulstoneUpgradeService = sus;
    }

    public async Task<CombatSession> PerformIdleCombatAsync(CharacterAction characterAction, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Setup
        var rng = Random.Shared;
        var totalLoot = new List<InventoryItem>();
        var lastCombatResult = new CombatResult();
        var combatSummary = new CombatSummary();
        var selectedCombatEnemyEntities = new List<CombatEntity>();
        var sessionStartedAt = characterAction.UpdatedAt;
        var combatAction = characterAction.ActionDetails as CombatActionDetails;

        string[] wantedBonuses = [SoulstoneUpgradeContants.SoulstoneDropRate,
                                  SoulstoneUpgradeContants.SoulstoneDoubleDropChance,
                                  SoulstoneUpgradeContants.CombatEssenceDropRate,
                                  SoulstoneUpgradeContants.CombatDoubleExpChance];

        var soulstoneBonuses = await _soulstoneUpgradeService.GetSoulstoneBonusesByCharacterIdAsync(characterAction.CharacterId, wantedBonuses, cancellationToken);

        // Initialize combatants
        var playerCharacters = await GetEntitiesAsync([.. combatAction.CharacterTeam], cancellationToken);
        var allEnemyCharacters = await GetEntitiesAsync([.. combatAction.Area.Creatures.Select(c => c.CreatureId)], cancellationToken);

        var combatPlayerEntities = _combatSetupService.CreateCombatEntities(playerCharacters);
        var allCombatEnemyEntities = _combatSetupService.CreateCombatEntities(allEnemyCharacters);

        // Prepare entities for combat
        await _combatSetupService.PrepareEntitiesForCombat([.. combatPlayerEntities, .. allCombatEnemyEntities]);

        while (characterAction.UpdatedAt < now)
        {
            // Initialize both teams
            var monsterCount = _spawningService.HowManyMonstersToSpawn(combatAction!.Area.SpawnProbabilities);
            var selectedAreaCreatures = _spawningService.WhatAreaCreaturesToSpawn([.. combatAction.Area.Creatures], monsterCount);
            var selectedEnemyIds = selectedAreaCreatures.Select(c => c.CreatureId).ToList();

            selectedCombatEnemyEntities = [.. selectedEnemyIds.Select(id => allCombatEnemyEntities.First(ee => ee.OriginalId.Equals(id)).Copy())];
            _combatSetupService.AppendPrefixToId(selectedCombatEnemyEntities);
            var combatSimulation = new CombatSimulation(combatPlayerEntities, selectedCombatEnemyEntities);
            lastCombatResult = combatSimulation.RunSimulation();

            // StartedAt is 1 second after the action is initialized, so as to have a 'combat starting' screen
            lastCombatResult.StartedAt = characterAction.UpdatedAt.AddSeconds(1);

            // Update the UpdatedAt timestamp based on combat duration
            // And add 2 seconds to have a delay of one second before and after the fight
            // To display the victory/defeat screen before a new fight is initialized
            characterAction.UpdatedAt += TimeSpan.FromSeconds(lastCombatResult.Duration * 0.1 + 2);

            // Reset entities when combat is over
            // Also process loot, since it's fight that should have already happened
            ResetEntitiesForCombat([.. combatPlayerEntities, .. selectedCombatEnemyEntities]);

            if (lastCombatResult.Outcome.Equals(BattleOutcome.Victory))
            {
                var selectedEnemyEntities = new List<Entity>();
                selectedEnemyIds.ForEach(id => selectedEnemyEntities.Add(allEnemyCharacters.First(ee => ee.Id.Equals(id))));

                var combatEssenceDropRate = soulstoneBonuses.Get(SoulstoneUpgradeContants.CombatEssenceDropRate);
                var lootThisBattle = _lootService.GenerateIdleCombatLootAsync(selectedEnemyEntities, new Dictionary<ItemType, double>() { { ItemType.Essence, combatEssenceDropRate } });
                lastCombatResult.Loot = lootThisBattle;

                // Accumulate total loot
                totalLoot.AddRange(lootThisBattle);

                lastCombatResult.ExperienceGained = selectedEnemyEntities.OfType<Creature>().Sum(e => e.ExperienceReward);
                if (rng.NextDouble() < (soulstoneBonuses.Get(SoulstoneUpgradeContants.CombatDoubleExpChance) / 100))
                    lastCombatResult.ExperienceGained *= 2;
            }
            AddToCombatSummary(combatSummary, lastCombatResult);
        }


        // Create CombatEntities to keep track of simple data over each entity, such as id, health, mana
        lastCombatResult.PlayerTeam = _combatSetupService.CreateSimpleCombatEntities(combatPlayerEntities);
        lastCombatResult.EnemyTeam = _combatSetupService.CreateSimpleCombatEntities(selectedCombatEnemyEntities);

        var combatSession = new CombatSession()
        {
            From = sessionStartedAt,
            To = now,
            CombatResult = lastCombatResult,
            CombatSummary = combatSummary,
        };

        var durationInSeconds = (int)Math.Abs((characterAction.UpdatedAt - sessionStartedAt).TotalSeconds);
        var soulstoneDropRate = soulstoneBonuses.Get(SoulstoneUpgradeContants.SoulstoneDropRate);
        var soulstoneDoubleDropChance = soulstoneBonuses.Get(SoulstoneUpgradeContants.SoulstoneDoubleDropChance);
        await ProcessSoulstoneDrops(characterAction.CharacterId, durationInSeconds, soulstoneDropRate, soulstoneDoubleDropChance, cancellationToken);

        await UpdateCharacterStatsAsync(playerCharacters, combatSession.CombatSummary.TotalExperience, cancellationToken);
        await ProcessLootAsync(characterAction.CharacterId, totalLoot, cancellationToken);

        return combatSession;
    }

    private async Task ProcessSoulstoneDrops(Guid characterId, int durationInSeconds, double dropRate, double doubleDropChance, CancellationToken cancellationToken)
    {
        var soulstonesEarned = _lootService.GenerateSoulstoneLoot(durationInSeconds, dropRate, doubleDropChance);
        if (soulstonesEarned < 1) return;

        await _publisher.Publish(new SoulstoneDropEvent(characterId, soulstonesEarned), cancellationToken);
    }

    private static void AddToCombatSummary(CombatSummary combatSummary, CombatResult lastCombatResult)
    {
        combatSummary.TotalBattles++;

        if (lastCombatResult.Outcome.Equals(BattleOutcome.Victory)) combatSummary.Wins++;
        else if (lastCombatResult.Outcome.Equals(BattleOutcome.Defeat)) combatSummary.Losses++;
        else combatSummary.Draws++;

        combatSummary.TotalExperience += lastCombatResult.ExperienceGained;
    }

    private async Task<List<Entity>> GetEntitiesAsync(List<Guid> entityIds, CancellationToken cancellationToken)
    {
        return await _entityService.GetEntitiesByIdsForCombatAsync(entityIds, cancellationToken);
    }

    private static void ResetEntitiesForCombat(List<CombatEntity> allEntities)
    {
        foreach (var entity in allEntities)
        {
            entity.Reset();
        }
    }


    private async Task ProcessLootAsync(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
    {
        if (loot.Count == 0) return;
        // Implement how to update the character or game state with the loot
        // For example, updating the character inventory
        //await _InventoryService.AddLootAsync(loot, cancellationToken);
        await _publisher.Publish(new LootGeneratedEvent(characterId, loot), cancellationToken);
    }

    private async Task UpdateCharacterStatsAsync(List<Entity> playerCharacters, int totalExp, CancellationToken cancellationToken)
    {
        if (totalExp == 0) return;

        var characters = playerCharacters.OfType<Character>();
        foreach (var character in characters)
        {
            character.Experience += totalExp / characters.Count();
            var wepAndShield = character.EquipmentSlots.Where(eq => (eq.EquipmentType == EquipmentType.MainHand || eq.EquipmentType == EquipmentType.OffHand) && eq.EquipmentInstance != null).ToList();

            await _levelingService.UpdateCharacterLevel(character);
        }
        await _entityService.UpdateEntities(playerCharacters, cancellationToken);
    }
}