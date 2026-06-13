using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Domain.Models.Bonuses;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
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

    private const double SigilDropChancePerCreature = 0.035d;
    private const string GoblinMinesSigilId = "sigil_goblin_mines";
    private const string ForgottenCatacombsSigilId = "sigil_forgotten_catacombs";
    private const string HivesAbyssSigilId = "sigil_hives_abyss";

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
        var sigilItemBases = await _itemBases.GetItemBasesByIdsAsync(
            [GoblinMinesSigilId, ForgottenCatacombsSigilId, HivesAbyssSigilId],
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

                var sigilDrops = RollSigilDrops(
                    facts.Area.Id,
                    encounter.HostileCreatures,
                    sigilItemBases);

                if (sigilDrops.Count > 0)
                {
                    loot = loot.Concat(sigilDrops).ToList();
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
        string areaId,
        IReadOnlyList<Creature> defeatedCreatures,
        IReadOnlyDictionary<string, ItemBase> sigilItemBases)
    {
        var sigilId = ResolveSigilId(areaId, defeatedCreatures);
        if (sigilId is null || !sigilItemBases.TryGetValue(sigilId, out var itemBase))
        {
            return [];
        }

        var quantity = 0;
        foreach (var _ in defeatedCreatures)
        {
            if (_randomSource.NextDouble() < SigilDropChancePerCreature)
            {
                quantity++;
            }
        }

        if (quantity <= 0)
        {
            return [];
        }

        var itemInstanceId = Guid.NewGuid();

        return
        [
            new InventoryItem
            {
                ItemInstanceId = itemInstanceId,
                Quantity = quantity,
                ItemInstance = new ItemInstance
                {
                    Id = itemInstanceId,
                    ItemBaseId = itemBase.Id,
                    ItemBase = itemBase
                }
            }
        ];
    }

    private static string? ResolveSigilId(
        string areaId,
        IReadOnlyList<Creature> defeatedCreatures)
    {
        return areaId switch
        {
            "region_01_area_01" or "region_01_area_05" => GoblinMinesSigilId,
            "region_01_area_02" or "region_01_area_07" => ForgottenCatacombsSigilId,
            "region_01_area_06" => HivesAbyssSigilId,
            _ => ResolveSigilIdFromCreatures(defeatedCreatures)
        };
    }

    private static string? ResolveSigilIdFromCreatures(IReadOnlyList<Creature> creatures)
    {
        var names = creatures
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        if (names.Any(x => x.Contains("goblin") || x.Contains("rat")))
        {
            return GoblinMinesSigilId;
        }

        if (names.Any(x => x.Contains("skeleton") || x.Contains("ghoul") || x.Contains("bat") || x.Contains("wraith")))
        {
            return ForgottenCatacombsSigilId;
        }

        if (names.Any(x => x.Contains("ant") || x.Contains("spider") || x.Contains("snake") || x.Contains("viper") || x.Contains("lizard")))
        {
            return HivesAbyssSigilId;
        }

        return null;
    }
}
