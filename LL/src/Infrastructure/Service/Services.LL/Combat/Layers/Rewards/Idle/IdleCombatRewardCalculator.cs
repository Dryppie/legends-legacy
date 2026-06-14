using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;
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
    private readonly IItemBaseRepository _itemBases;

    private const int IdleActionsPerDay = 24 * 60 * 60 / 10;
    private const double TargetSigilDropsPerDay = 2d;
    private const double SigilDropChancePerIdleAction = TargetSigilDropsPerDay / IdleActionsPerDay;
    private const string GoblinMinesSigilId = "sigil_goblin_mines";
    private const string ForgottenCatacombsSigilId = "sigil_forgotten_catacombs";
    private const string HivesAbyssSigilId = "sigil_hives_abyss";
    private const string ShenicRegionId = "region_01";

    private static readonly IReadOnlyDictionary<string, string[]> RegionSigilIds = new Dictionary<string, string[]>
    {
        [ShenicRegionId] =
        [
            GoblinMinesSigilId,
            ForgottenCatacombsSigilId,
            HivesAbyssSigilId
        ]
    };

    public IdleCombatRewardCalculator(
        IBonusService bonusService,
        ILootService lootService,
        ICinderRewardCalculator cinderRewardCalculator,
        ISoulstoneRewardCalculator soulstoneRewardCalculator,
        IRandomSource randomSource,
        IEssenceResonanceService essenceResonanceService,
        IItemBaseRepository itemBases)
    {
        _bonusService = bonusService;
        _lootService = lootService;
        _cinderRewardCalculator = cinderRewardCalculator;
        _soulstoneRewardCalculator = soulstoneRewardCalculator;
        _randomSource = randomSource;
        _essenceResonanceService = essenceResonanceService;
        _itemBases = itemBases;
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
        var sigilRegionId = ResolveSigilRegionId(facts.Area.Id);
        var sigilEligibleVictories = 0;
        var sigilItemBases = await _itemBases.GetItemBasesByIdsAsync(
            RegionSigilIds.Values.SelectMany(x => x).Distinct().ToArray(),
            cancellationToken);

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

                if (sigilRegionId is not null)
                {
                    sigilEligibleVictories++;
                }

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

        var sigilDrops = RollSigilDrops(sigilRegionId, sigilEligibleVictories, sigilItemBases);
        if (sigilDrops.Count > 0)
        {
            totalLoot.AddRange(sigilDrops);
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
            EncounterOutcomes: encounterOutcomes);
    }

    private IReadOnlyList<InventoryItem> RollSigilDrops(
        string? regionId,
        int eligibleVictories,
        IReadOnlyDictionary<string, ItemBase> sigilItemBases)
    {
        if (regionId is null || eligibleVictories <= 0)
        {
            return [];
        }

        if (!RegionSigilIds.TryGetValue(regionId, out var regionSigilIds) || regionSigilIds.Length == 0)
        {
            return [];
        }

        var dropCount = SamplePoisson(eligibleVictories * SigilDropChancePerIdleAction);
        if (dropCount <= 0)
        {
            return [];
        }

        var quantitiesBySigilId = new Dictionary<string, int>();
        for (var i = 0; i < dropCount; i++)
        {
            var sigilId = PickRandomSigilId(regionSigilIds);
            quantitiesBySigilId[sigilId] = quantitiesBySigilId.GetValueOrDefault(sigilId) + 1;
        }

        var sigilDrops = new List<InventoryItem>();

        foreach (var (sigilId, quantity) in quantitiesBySigilId)
        {
            if (!sigilItemBases.TryGetValue(sigilId, out var itemBase))
            {
                continue;
            }

            var itemInstanceId = Guid.NewGuid();
            sigilDrops.Add(new InventoryItem
            {
                ItemInstanceId = itemInstanceId,
                Quantity = quantity,
                ItemInstance = new ItemInstance
                {
                    Id = itemInstanceId,
                    ItemBaseId = itemBase.Id,
                    ItemBase = itemBase
                }
            });
        }

        return sigilDrops;
    }

    private int SamplePoisson(double lambda)
    {
        if (lambda <= 0)
        {
            return 0;
        }

        var drops = 0;
        var probability = 1.0;
        var threshold = Math.Exp(-lambda);

        while (probability > threshold)
        {
            drops++;
            probability *= _randomSource.NextDouble();
        }

        return drops - 1;
    }

    private string PickRandomSigilId(IReadOnlyList<string> sigilIds)
    {
        var index = Math.Min((int)(_randomSource.NextDouble() * sigilIds.Count), sigilIds.Count - 1);
        return sigilIds[index];
    }

    private static string? ResolveSigilRegionId(string areaId)
    {
        const string areaMarker = "_area_";
        var markerIndex = areaId.IndexOf(areaMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0)
        {
            return null;
        }

        var regionId = areaId[..markerIndex];
        if (!RegionSigilIds.ContainsKey(regionId))
        {
            return null;
        }

        return regionId;
    }
}
