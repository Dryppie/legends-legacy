using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Rewards;

namespace Services.LL.Dungeons;

public sealed class DungeonPreviewRewardService : IDungeonPreviewRewardService
{
    private readonly IItemBaseRepository _itemBases;
    private readonly IRewardTableDefinitionProvider _rewardTables;
    private readonly EquipmentBlueprintCatalog? _blueprints;
    private readonly IEquipmentBlueprintRepository? _blueprintProgress;

    public DungeonPreviewRewardService(
        IItemBaseRepository itemBases,
        IRewardTableDefinitionProvider rewardTables,
        EquipmentBlueprintCatalog? blueprints = null,
        IEquipmentBlueprintRepository? blueprintProgress = null)
    {
        _itemBases = itemBases;
        _rewardTables = rewardTables;
        _blueprints = blueprints;
        _blueprintProgress = blueprintProgress;
    }

    public async Task<IReadOnlyList<DungeonPreviewReward>> GetPossibleCompletionRewardsAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        var rewardsByDungeon = await GetPossibleCompletionRewardsAsync(
            new[] { dungeon },
            cancellationToken);
        return rewardsByDungeon[dungeon.Id];
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<DungeonPreviewReward>>> GetPossibleCompletionRewardsAsync(
        IReadOnlyCollection<DungeonDefinition> dungeons,
        CancellationToken cancellationToken,
        Guid? characterId = null)
    {
        if (dungeons.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<DungeonPreviewReward>>(
                StringComparer.OrdinalIgnoreCase);
        }

        var progress = characterId.HasValue && _blueprints is not null
            ? await (_blueprintProgress ?? throw new InvalidOperationException("Blueprint persistence is required."))
                .GetProgressAsync(characterId.Value, cancellationToken)
            : [];
        var missesByFamily = progress.ToDictionary(x => x.FamilyId, x => x.Misses, StringComparer.Ordinal);
        var entriesByDungeon = dungeons.ToDictionary(
            dungeon => dungeon.Id,
            dungeon => BuildPreviewEntries(dungeon, missesByFamily),
            StringComparer.OrdinalIgnoreCase);
        var itemIds = entriesByDungeon.Values
            .SelectMany(entries => entries)
            .Select(entry => entry.ItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);

        return entriesByDungeon.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<DungeonPreviewReward>)MapRewards(pair.Value, itemBases),
            StringComparer.OrdinalIgnoreCase);
    }

    private List<DungeonPreviewRewardEntry> BuildPreviewEntries(
        DungeonDefinition dungeon, IReadOnlyDictionary<string, int> missesByFamily)
    {
        var rewards = new List<DungeonPreviewRewardEntry>();

        if (dungeon.CompletionRewardTableIds.Count > 0)
        {
            rewards.AddRange(MapRewardTables(
                dungeon.CompletionRewardTableIds,
                "Completion Loot",
                "Every Completion"));
        }

        if (dungeon.TierRewardTableIds.Count > 0)
        {
            rewards.AddRange(MapRewardTables(
                dungeon.TierRewardTableIds,
                "Tier Loot",
                $"Tier {dungeon.Tier} Completion"));
        }

        rewards.AddRange(MapMonsterCoreRewards(dungeon));
        rewards.AddRange(MapFirstCompletionRewards(dungeon));

        var source = _blueprints?.FindSource(dungeon.SigilItemId);
        if (source is not null)
        {
            var misses = missesByFamily.GetValueOrDefault(source.FamilyId);
            var chance = _blueprints!.DropChanceAfterMisses(misses);
            var drops = _blueprints.DropsFor(source);
            var clearsUntilGuaranteed = Math.Max(1, _blueprints.GuaranteeCompletions - misses);
            var notice = clearsUntilGuaranteed == 1
                ? "A blueprint is guaranteed on this clear."
                : $"Blueprint chance: {chance * 100:0.##}%. Guaranteed within {clearsUntilGuaranteed} clears.";
            if (drops.Count > 1) notice += " One of the listed blueprints drops, chosen at random.";
            foreach (var blueprint in drops)
                rewards.Add(new DungeonPreviewRewardEntry(blueprint.ItemId, "Blueprints", notice,
                    1, 1, chance * 100d / drops.Count,
                    chance < 1 || drops.Count > 1, (1d - chance / drops.Count) * 100d));
        }

        return rewards;
    }

    private static List<DungeonPreviewReward> MapRewards(
        IEnumerable<DungeonPreviewRewardEntry> rewards,
        IReadOnlyDictionary<string, ItemBase> itemBases) =>
        rewards
            .Where(reward => itemBases.ContainsKey(reward.ItemId))
            .GroupBy(x => new { x.ItemId, x.Category })
            .Select(x =>
            {
                var firstReward = x.First();
                var source = string.Join(", ", x.Select(reward => reward.Source).Distinct());
                var chance = CombineChances(x.Select(reward => reward.DropChancePercent));
                var noDropChance = CombineChances(x.Select(reward => reward.NoDropChancePercent));

                return new DungeonPreviewReward(
                    itemBases[firstReward.ItemId],
                    firstReward.Category,
                    source,
                    x.Min(reward => reward.MinQuantity),
                    x.Max(reward => reward.MaxQuantity),
                    chance,
                    x.Any(reward => reward.CanDropNothing),
                    noDropChance);
            })
            .ToList();

    private IEnumerable<DungeonPreviewRewardEntry> MapRewardTables(
        IReadOnlyCollection<string> rewardTableIds,
        string category,
        string source)
    {
        var entries = rewardTableIds
            .Select(_rewardTables.GetById)
            .SelectMany(table => AnalyzeTable(table, 1d, false, null))
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .ToList();

        return entries
            .Select(entry => new DungeonPreviewRewardEntry(
                entry.ItemId,
                category,
                source,
                entry.MinQuantity,
                entry.MaxQuantity,
                entry.DropChancePercent,
                entry.CanDropNothing,
                entry.NoDropChancePercent))
            .ToList();
    }

    private static IEnumerable<DungeonPreviewRewardEntry> MapMonsterCoreRewards(
        DungeonDefinition dungeon)
    {
        var grants = DungeonRewardCatalog.GetMonsterCoreRewardGrants(dungeon.Grade);

        return grants
            .Select(grant => new DungeonPreviewRewardEntry(
                grant.ItemId,
                "Monster Cores",
                "Every Completion",
                grant.MinAmount,
                grant.MaxAmount,
                DropChancePercent: 100))
            .ToList();
    }

    private static IEnumerable<DungeonPreviewRewardEntry> MapFirstCompletionRewards(
        DungeonDefinition dungeon)
    {
        var grants = DungeonRewardCatalog.GetFirstCompletionGrants(dungeon);
        if (grants.Count == 0)
        {
            return [];
        }

        return grants
            .Where(x => !string.IsNullOrWhiteSpace(x.ItemId))
            .Select(x => new DungeonPreviewRewardEntry(
                x.ItemId,
                "First Completion",
                "Once Per Character",
                x.MinAmount,
                x.MaxAmount,
                x.Chance * 100,
                x.Chance < 1,
                (1 - x.Chance) * 100))
            .ToList();
    }

    private IEnumerable<RewardPreviewEntry> AnalyzeTable(
        RewardTableDefinition rewardTable,
        double parentChance,
        bool inheritedCanDropNothing,
        double? inheritedNoDropChance)
    {
        foreach (var roll in rewardTable.Rolls)
        {
            var rollChance = parentChance * Math.Clamp(roll.Chance, 0d, 1d);
            var rollCanDropNothing = inheritedCanDropNothing || roll.Chance < 1;
            var rollNoDropChance = CombineNoDropChance(inheritedNoDropChance, roll.Chance < 1 ? 1 - roll.Chance : null);

            var totalEntryWeight = roll.Entries.Sum(entry => Math.Max(0, entry.Weight));
            var totalWeightWithNoDrop = totalEntryWeight + Math.Max(0, roll.NoDropWeight);
            if (roll.Type == RewardRollType.WeightedWithNoDrop && totalWeightWithNoDrop > 0)
            {
                rollCanDropNothing = true;
                rollNoDropChance = CombineNoDropChance(rollNoDropChance, Math.Max(0, roll.NoDropWeight) / totalWeightWithNoDrop);
            }

            foreach (var entry in roll.Entries)
            {
                if (entry.Type == RewardEntryType.Item && !string.IsNullOrWhiteSpace(entry.ItemId))
                {
                    var probability = CalculateEntryProbability(roll, entry, rollChance);
                    yield return new RewardPreviewEntry(
                        entry.ItemId,
                        entry.Quantity.Min,
                        Math.Max(entry.Quantity.Min, entry.Quantity.Max) * Math.Max(1, roll.Rolls),
                        ProbabilityToPercent(probability),
                        rollCanDropNothing,
                        ProbabilityToPercent(rollNoDropChance));
                    continue;
                }

                if (entry.Type == RewardEntryType.RewardTableReference &&
                    !string.IsNullOrWhiteSpace(entry.RewardTableId))
                {
                    var probability = CalculateEntryProbability(roll, entry, rollChance);
                    foreach (var nested in AnalyzeTable(
                        _rewardTables.GetById(entry.RewardTableId),
                        probability,
                        rollCanDropNothing,
                        rollNoDropChance))
                    {
                        yield return nested;
                    }
                }
            }
        }
    }

    private static double CalculateEntryProbability(
        RewardRollDefinition roll,
        RewardEntryDefinition entry,
        double rollChance)
    {
        var entryChance = Math.Clamp(entry.Chance, 0d, 1d);
        var perRollProbability = roll.Type switch
        {
            RewardRollType.Weighted => WeightedProbability(roll, entry, includeNoDrop: false) * rollChance * entryChance,
            RewardRollType.WeightedWithNoDrop => WeightedProbability(roll, entry, includeNoDrop: true) * rollChance * entryChance,
            _ => rollChance * entryChance
        };

        return ProbabilityAtLeastOnce(perRollProbability, Math.Max(1, roll.Rolls));
    }

    private static double WeightedProbability(
        RewardRollDefinition roll,
        RewardEntryDefinition entry,
        bool includeNoDrop)
    {
        var total = roll.Entries.Sum(x => Math.Max(0, x.Weight)) +
            (includeNoDrop ? Math.Max(0, roll.NoDropWeight) : 0);
        return total <= 0 ? 0 : Math.Max(0, entry.Weight) / total;
    }

    private static double ProbabilityAtLeastOnce(double probability, int rolls)
    {
        var clamped = Math.Clamp(probability, 0d, 1d);
        return 1d - Math.Pow(1d - clamped, Math.Max(1, rolls));
    }

    private static double? ProbabilityToPercent(double? probability) =>
        probability.HasValue ? Math.Round(Math.Clamp(probability.Value, 0d, 1d) * 100d, 4) : null;

    private static double? CombineChances(IEnumerable<double?> percentages)
    {
        var probabilities = percentages
            .Where(x => x.HasValue)
            .Select(x => Math.Clamp(x!.Value / 100d, 0d, 1d))
            .ToList();

        if (probabilities.Count == 0)
        {
            return null;
        }

        return ProbabilityToPercent(1d - probabilities.Aggregate(1d, (product, chance) => product * (1d - chance)));
    }

    private static double? CombineNoDropChance(double? existing, double? next)
    {
        if (!existing.HasValue)
        {
            return next;
        }

        if (!next.HasValue)
        {
            return existing;
        }

        return 1d - ((1d - existing.Value) * (1d - next.Value));
    }

    private sealed record RewardPreviewEntry(
        string ItemId,
        int MinQuantity,
        int MaxQuantity,
        double? DropChancePercent,
        bool CanDropNothing,
        double? NoDropChancePercent);

    private sealed record DungeonPreviewRewardEntry(
        string ItemId,
        string Category,
        string Source,
        int MinQuantity = 1,
        int MaxQuantity = 1,
        double? DropChancePercent = null,
        bool CanDropNothing = false,
        double? NoDropChancePercent = null);
}
