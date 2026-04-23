using Application.Interfaces.Services.LL;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Extensions;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

internal class DungeonCombatRewardCalculator : IDungeonCombatRewardCalculator
{
    private readonly IBonusService _bonusService;
    private readonly ILootService _lootService;
    private readonly ICinderRewardCalculator _cinderRewardCalculator;
    private readonly ISoulstoneRewardCalculator _soulstoneRewardCalculator;
    private readonly IRandomSource _randomSource;

    public DungeonCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        ICinderRewardCalculator cinderRewardCalculator,
        ISoulstoneRewardCalculator soulstoneRewardCalculator,
        IRandomSource randomSource)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _cinderRewardCalculator = cinderRewardCalculator;
        _soulstoneRewardCalculator = soulstoneRewardCalculator;
        _randomSource = randomSource;
    }

    public async Task<DungeonCombatCalculatedOutcome> CalculateAsync(
        DungeonCombatRewardFacts facts,
        CancellationToken cancellationToken)
    {
        var factors = await _bonusService.GetAggregatedAsync(
            facts.CharacterId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        var essenceDropRate = factors.Get(BonusKind.CombatEssenceDropRate);
        var doubleExpChance = factors.Get(BonusKind.CombatDoubleExpChance);
        var soulstoneDropRate = factors.Get(BonusKind.SoulstoneDropRate);
        var soulstoneDoubleDropChance = factors.Get(BonusKind.SoulstoneDoubleDropChance);

        var encounterOutcomes = new List<DungeonEncounterCalculatedOutcome>(facts.Encounters.Count);
        var totalLoot = new List<InventoryItem>();
        var totalExperience = 0;
        var totalCinders = 0;

        foreach (var encounter in facts.Encounters)
        {
            var experience = 0;
            var cinders = 0;
            IReadOnlyList<InventoryItem> loot = Array.Empty<InventoryItem>();

            if (encounter.IsVictory)
            {
                loot = _lootService.GenerateIdleCombatLootAsync(
                    encounter.HostileCreatures.Cast<Entity>().ToList(),
                    new Dictionary<ItemType, double>
                    {
                        { ItemType.Essence, essenceDropRate }
                    });

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

            encounterOutcomes.Add(new DungeonEncounterCalculatedOutcome(
                EncounterId: encounter.EncounterId,
                ExperienceGained: experience,
                CindersGained: cinders,
                Loot: loot));
        }

        var totalSoulstones = 5;
            //_soulstoneRewardCalculator.Calculate(
            //durationInSeconds: (int)Math.Abs(facts.ProcessedDuration.TotalSeconds),
            //dropRatePercent: soulstoneDropRate,
            //doubleDropChancePercent: soulstoneDoubleDropChance);

        return new DungeonCombatCalculatedOutcome(
            CharacterId: facts.CharacterId,
            TotalExperience: totalExperience,
            TotalCinders: totalCinders,
            TotalSoulstones: totalSoulstones,
            TotalLoot: totalLoot,
            EncounterOutcomes: encounterOutcomes);
    }
}