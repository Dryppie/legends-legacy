using Application.Interfaces.Services.LL.Items;
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
    private readonly IAreaExperienceBalanceProvider _areaExperienceBalance;
    private readonly ICombatAcquisitionRewardProcessor? _progression;

    public IdleCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        ISoulstoneRewardCalculator soulstoneRewardCalculator,
        IEssenceResonanceService essenceResonanceService,
        IAreaExperienceBalanceProvider areaExperienceBalance,
        ICombatAcquisitionRewardProcessor? progression = null)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _soulstoneRewardCalculator = soulstoneRewardCalculator;
        _essenceResonanceService = essenceResonanceService;
        _areaExperienceBalance = areaExperienceBalance;
        _progression = progression;
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

        if (_progression != null)
        {
            var ordinary = await _progression.ProcessAsync(facts, cancellationToken);
            totalLoot.AddRange(ordinary.Equipment);
            totalLoot.AddRange(ordinary.Scrap);
            totalLoot.AddRange(ordinary.Sigils);
            powerRewards.AddRange(ordinary.Equipment);
            craftingRewards.AddRange(ordinary.Scrap);
            dungeonAccessRewards.AddRange(ordinary.Sigils);
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
