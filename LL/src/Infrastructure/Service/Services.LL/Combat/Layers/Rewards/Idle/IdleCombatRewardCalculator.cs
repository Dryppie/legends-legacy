using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Inventories;
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
    private readonly ICinderRewardCalculator _cinderRewardCalculator;
    private readonly ISoulstoneRewardCalculator _soulstoneRewardCalculator;
    private readonly IRandomSource _randomSource;
    private readonly IEssenceResonanceService _essenceResonanceService;
    private readonly IIdleDungeonSigilDropCalculator _sigilDropCalculator;
    private readonly ICombatGatheringRewardProcessor _gatheringRewardProcessor;

    public IdleCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        ICinderRewardCalculator cinderRewardCalculator,
        ISoulstoneRewardCalculator soulstoneRewardCalculator,
        IRandomSource randomSource,
        IEssenceResonanceService essenceResonanceService,
        IIdleDungeonSigilDropCalculator sigilDropCalculator,
        ICombatGatheringRewardProcessor gatheringRewardProcessor)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _cinderRewardCalculator = cinderRewardCalculator;
        _soulstoneRewardCalculator = soulstoneRewardCalculator;
        _randomSource = randomSource;
        _essenceResonanceService = essenceResonanceService;
        _sigilDropCalculator = sigilDropCalculator;
        _gatheringRewardProcessor = gatheringRewardProcessor;
    }

    public async Task<IdleCombatCalculatedOutcome> CalculateAsync(
        IdleCombatRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var factors = await _bonusService.GetAggregatedAsync(
            facts.CharacterId,
            facts.RequestedTo,
            cancellationToken);

        var doubleExpChance = factors.Get(BonusKind.CombatDoubleExpChance);
        var soulstoneDropRate = factors.Get(BonusKind.SoulstoneDropRate);
        var soulstoneDoubleDropChance = factors.Get(BonusKind.SoulstoneDoubleDropChance);

        var encounterOutcomes = new List<IdleEncounterCalculatedOutcome>(facts.Encounters.Count);
        var totalLoot = new List<InventoryItem>();
        var totalExperience = 0;
        var totalCinders = 0;
        var sigilEligibleVictories = 0;

        foreach (var encounter in facts.Encounters.OrderBy(x => x.Sequence))
        {
            var experience = 0;
            var cinders = 0;
            IReadOnlyList<InventoryItem> loot = Array.Empty<InventoryItem>();

            if (encounter.IsVictory)
            {
                loot = _lootService.GenerateIdleCombatLootAsync(
                    encounter.HostileCreatures.Cast<Entity>().ToList(),
                    []);

                var essenceDrops = await _essenceResonanceService.RollEssenceDropsAsync(
                    facts.CharacterId,
                    encounter.HostileCreatures,
                    eligible: true,
                    cancellationToken);

                if (essenceDrops.Count > 0)
                {
                    loot = loot.Concat(essenceDrops).ToList();
                }

                sigilEligibleVictories++;

                experience = encounter.HostileCreatures.Sum(x => x.ExperienceReward);

                if (_randomSource.NextDouble() < (doubleExpChance / 100d))
                {
                    experience *= 2;
                }

                cinders = _cinderRewardCalculator.Calculate(encounter.HostileCreatures);

                totalLoot.AddRange(loot);
                totalExperience += experience;
                totalCinders += cinders;
            }

            encounterOutcomes.Add(new IdleEncounterCalculatedOutcome(
                EncounterId: encounter.EncounterId,
                Sequence: encounter.Sequence,
                ExperienceGained: experience,
                CindersGained: cinders,
                Loot: loot));
        }

        var sigilDrops = await _sigilDropCalculator.RollAsync(
            facts.Area,
            sigilEligibleVictories,
            cancellationToken);

        if (sigilDrops.Count > 0)
        {
            totalLoot.AddRange(sigilDrops);
        }

        var gatheringRewards = await _gatheringRewardProcessor.ProcessAsync(
            facts,
            cancellationToken);

        var gatheringLoot = gatheringRewards
            .SelectMany(x => x.ItemsGained)
            .ToList();

        if (gatheringLoot.Count > 0)
        {
            totalLoot.AddRange(gatheringLoot);
        }

        var totalSoulstones = _soulstoneRewardCalculator.Calculate(
            durationInSeconds: (int)Math.Abs(facts.ProcessedDuration.TotalSeconds),
            dropRatePercent: soulstoneDropRate,
            doubleDropChancePercent: soulstoneDoubleDropChance);

        return new IdleCombatCalculatedOutcome(
            CharacterId: facts.CharacterId,
            From: facts.From,
            ProcessedUntil: facts.ProcessedUntil,
            TotalExperience: totalExperience,
            TotalCinders: totalCinders,
            TotalSoulstones: totalSoulstones,
            TotalLoot: totalLoot,
            GatheringRewards: gatheringRewards,
            EncounterOutcomes: encounterOutcomes);
    }
}
