using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Extensions;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatRewardCalculator : IIdleCombatRewardCalculator
{
    private readonly IBonusService _bonusService;
    private readonly ILootService _lootService;
    private readonly ISoulstoneRewardCalculator _soulstoneRewardCalculator;
    private readonly IEssenceResonanceService _essenceResonanceService;
    private readonly IIdleDungeonSigilDropCalculator _sigilDropCalculator;
    private readonly ICombatGatheringRewardProcessor _gatheringRewardProcessor;
    private readonly IAreaExperienceBalanceProvider _areaExperienceBalance;

    public IdleCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        ISoulstoneRewardCalculator soulstoneRewardCalculator,
        IEssenceResonanceService essenceResonanceService,
        IIdleDungeonSigilDropCalculator sigilDropCalculator,
        ICombatGatheringRewardProcessor gatheringRewardProcessor,
        IAreaExperienceBalanceProvider areaExperienceBalance)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _soulstoneRewardCalculator = soulstoneRewardCalculator;
        _essenceResonanceService = essenceResonanceService;
        _sigilDropCalculator = sigilDropCalculator;
        _gatheringRewardProcessor = gatheringRewardProcessor;
        _areaExperienceBalance = areaExperienceBalance;
    }

    public async Task<IdleCombatCalculatedOutcome> CalculateAsync(
        IdleCombatRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var factors = await _bonusService.GetAggregatedAsync(
            facts.CharacterId,
            facts.RequestedTo,
            cancellationToken);

        var combatExperienceGainBps = factors.Get(BonusKind.CombatExperienceGainBps);
        var defeatExperienceRetentionBps = factors.Get(BonusKind.IdleCombatDefeatExperienceRetentionBps);

        var encounterOutcomes = new List<IdleEncounterCalculatedOutcome>(facts.Encounters.Count);
        var totalLoot = new List<InventoryItem>();
        var powerRewards = new List<InventoryItem>();
        var craftingRewards = new List<InventoryItem>();
        var essenceRewards = new List<InventoryItem>();
        var dungeonAccessRewards = new List<InventoryItem>();
        var totalExperience = 0;
        var totalCinders = 0;
        var sigilEligibleVictories = 0;
        var orderedEncounters = facts.Encounters.OrderBy(x => x.Sequence).ToArray();
        var victoriousEncounters = orderedEncounters.Where(x => x.IsVictory).ToArray();
        var combatLootByEncounterId = new Dictionary<Guid, IReadOnlyList<InventoryItem>>();
        var essenceDropsByEncounterId = new Dictionary<Guid, IReadOnlyList<InventoryItem>>();

        if (victoriousEncounters.Length > 0)
        {
            await _essenceResonanceService.PrepareEssenceDropsAsync(
                facts.CharacterId,
                victoriousEncounters.SelectMany(x => x.HostileCreatures).ToArray(),
                factors.Get(BonusKind.FocusedMonsterEssenceDropRateRelativeBps) > 0,
                cancellationToken);

            var lootGroups = await _lootService.GenerateIdleCombatLootBatchAsync(
                victoriousEncounters
                    .Select(encounter => (IReadOnlyList<Entity>)encounter.HostileCreatures.Cast<Entity>().ToArray())
                    .ToArray(),
                [],
                cancellationToken);

            var essenceDropGroups = await _essenceResonanceService.RollEssenceDropGroupsAsync(
                facts.CharacterId,
                victoriousEncounters
                    .Select(encounter => (IReadOnlyList<Creature>)encounter.HostileCreatures)
                    .ToArray(),
                eligible: true,
                cancellationToken,
                factors);

            for (var index = 0; index < victoriousEncounters.Length; index++)
            {
                combatLootByEncounterId[victoriousEncounters[index].EncounterId] = lootGroups[index];
                essenceDropsByEncounterId[victoriousEncounters[index].EncounterId] = essenceDropGroups[index];
            }
        }

        foreach (var encounter in orderedEncounters)
        {
            var creatureCount = encounter.HostileCreatures.Count;
            var areaBaseExperience = _areaExperienceBalance.CalculateEncounterExperience(
                facts.Area.Id,
                creatureCount);
            var areaBaseCinders = _areaExperienceBalance.CalculateEncounterCinders(
                facts.Area.Id,
                creatureCount);
            var bonusAdjustedExperience = areaBaseExperience.ApplyPositiveBps(combatExperienceGainBps);
            var experience = 0;
            var cinders = 0;
            IReadOnlyList<InventoryItem> loot = Array.Empty<InventoryItem>();

            if (encounter.IsVictory)
            {
                loot = combatLootByEncounterId[encounter.EncounterId];
                ClassifyCombatLoot(loot, powerRewards, craftingRewards, essenceRewards);

                var essenceDrops = essenceDropsByEncounterId[encounter.EncounterId];

                if (essenceDrops.Count > 0)
                {
                    loot = loot.Concat(essenceDrops).ToList();
                    essenceRewards.AddRange(essenceDrops);
                }

                sigilEligibleVictories++;

                experience = bonusAdjustedExperience;

                cinders = areaBaseCinders;

                totalLoot.AddRange(loot);
                totalCinders += cinders;
            }
            else if (defeatExperienceRetentionBps > 0)
            {
                experience = bonusAdjustedExperience.TakeBpsPortion(defeatExperienceRetentionBps);
            }

            totalExperience += experience;

            encounterOutcomes.Add(new IdleEncounterCalculatedOutcome(
                EncounterId: encounter.EncounterId,
                Sequence: encounter.Sequence,
                CreatureCount: creatureCount,
                AreaBaseExperience: areaBaseExperience,
                BonusAdjustedExperience: bonusAdjustedExperience,
                ExperienceGained: experience,
                CindersGained: cinders,
                Loot: loot));
        }

        var sigilDrops = await _sigilDropCalculator.RollAsync(
            facts.CharacterId,
            facts.Area,
            sigilEligibleVictories,
            cancellationToken,
            factors);

        if (sigilDrops.Count > 0)
        {
            totalLoot.AddRange(sigilDrops);
            dungeonAccessRewards.AddRange(sigilDrops);
        }

        var gatheringRewards = await _gatheringRewardProcessor.ProcessAsync(
            new CombatGatheringRewardFacts(
                facts.CharacterId,
                facts.Encounters.Count(x => x.IsVictory),
                facts.EquippedTool,
                facts.Area.GatheringNodes
                    .Select(node => new CombatGatheringNode(
                        node.Id,
                        node.Name,
                        node.Type,
                        node.LevelRequirement,
                        node.ProcChance,
                        node.RewardTableId,
                        YieldMultiplier: AreaGatheringYieldBalance.ResolveMultiplier(node.YieldBonusPercent),
                        AreaYieldBonusPercent: node.YieldBonusPercent))
                    .ToArray()),
            cancellationToken,
            factors);

        var gatheringLoot = gatheringRewards
            .SelectMany(x => x.ItemsGained)
            .ToList();

        if (gatheringLoot.Count > 0)
        {
            totalLoot.AddRange(gatheringLoot);
        }

        var totalSoulstones = _soulstoneRewardCalculator.Calculate(
            durationInSeconds: (int)Math.Abs(facts.ProcessedDuration.TotalSeconds),
            dropRatePercent: 0,
            doubleDropChancePercent: 0);

        return new IdleCombatCalculatedOutcome(
            CharacterId: facts.CharacterId,
            From: facts.From,
            ProcessedUntil: facts.ProcessedUntil,
            TotalExperience: totalExperience,
            TotalCinders: totalCinders,
            TotalSoulstones: totalSoulstones,
            TotalLoot: totalLoot,
            PowerRewards: powerRewards,
            CraftingRewards: craftingRewards,
            EssenceRewards: essenceRewards,
            DungeonAccessRewards: dungeonAccessRewards,
            GatheringRewards: gatheringRewards,
            EncounterOutcomes: encounterOutcomes);
    }

    private static void ClassifyCombatLoot(
        IReadOnlyList<InventoryItem> loot,
        ICollection<InventoryItem> powerRewards,
        ICollection<InventoryItem> craftingRewards,
        ICollection<InventoryItem> essenceRewards)
    {
        foreach (var item in loot)
        {
            switch (item.ItemInstance.ItemBase.ItemType)
            {
                case ItemType.Essence:
                    essenceRewards.Add(item);
                    break;
                case ItemType.Equipment:
                    powerRewards.Add(item);
                    break;
                default:
                    craftingRewards.Add(item);
                    break;
            }
        }
    }
}
