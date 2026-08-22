using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Rewards;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonCompletionRewardApplier : IDungeonCompletionRewardApplier
{
    private static readonly IReadOnlySet<string> FirstCompletionExcludedRollIds =
        new HashSet<string>(["blueprint_drop"], StringComparer.OrdinalIgnoreCase);

    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly IItemBaseRepository _itemBases;
    private readonly IRewardRoller _rewardRoller;
    private readonly IDungeonPendingRewardWriter _pendingRewardWriter;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IDungeonMasteryService _mastery;
    private readonly IAchievementService? _achievementService;

    public DungeonCompletionRewardApplier(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonRunRepository dungeonRuns,
        IItemBaseRepository itemBases,
        IRewardRoller rewardRoller,
        IDungeonPendingRewardWriter pendingRewardWriter,
        IInventoryItemFactory inventoryItemFactory,
        IDungeonMasteryService mastery,
        IAchievementService? achievementService = null)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonRuns = dungeonRuns;
        _itemBases = itemBases;
        _rewardRoller = rewardRoller;
        _pendingRewardWriter = pendingRewardWriter;
        _inventoryItemFactory = inventoryItemFactory;
        _mastery = mastery;
        _achievementService = achievementService;
    }

    public async Task ApplyAsync(DungeonRun run, CancellationToken cancellationToken)
    {
        var dungeon = _dungeonDefinitions.GetByKey(run.DungeonDefinitionId);
        var masteryBenefits = DungeonMasteryBenefits.Resolve(run.State?.MasteryLevelAtStart ?? 0);
        var isFirstCompletion = !await _dungeonRuns.HasCompletedDungeonAsync(
            run.CharacterId,
            run.DungeonDefinitionId,
            cancellationToken);

        if (dungeon.CompletionRewardTableIds.Count > 0)
        {
            foreach (var rewardTableId in dungeon.CompletionRewardTableIds)
            {
                await RollRewardTableAndAddAsync(
                    run.Id,
                    rewardTableId,
                    "Dungeon Completion",
                    masteryBenefits.CompletionCurrencyBonusPercent,
                    isFirstCompletion ? FirstCompletionExcludedRollIds : null,
                    cancellationToken);
            }
        }

        if (dungeon.TierRewardTableIds.Count > 0)
        {
            foreach (var rewardTableId in dungeon.TierRewardTableIds)
            {
                await RollRewardTableAndAddAsync(
                    run.Id,
                    rewardTableId,
                    $"Tier {dungeon.Tier} Completion",
                    masteryBenefits.CompletionCurrencyBonusPercent,
                    excludedRollIds: null,
                    cancellationToken);
            }
        }

        await AddItemGrantsAsync(
            run.Id,
            dungeon.RewardTable.CompletionRewards,
            "Dungeon Completion Rewards",
            cancellationToken);

        await AddMonsterCoreRewardsAsync(run.Id, dungeon.Grade, cancellationToken);
        await AddFirstCompletionRewardsIfNeededAsync(run, dungeon, isFirstCompletion, cancellationToken);
        var masteryAward = await _mastery.AwardCompletionAsync(run, cancellationToken);
        if (_achievementService is not null)
        {
            await _achievementService.RecordDungeonMasteryLevelReachedAsync(run.CharacterId, masteryAward.Level, cancellationToken);
        }

        if (!masteryAward.AlreadyAwarded &&
            masteryAward.PreviousLevel < DungeonMasteryBenefits.MaxLevel &&
            masteryAward.Level >= DungeonMasteryBenefits.MaxLevel)
        {
            await AddRewardRollResultAsync(
                run.Id,
                new RewardRollResult(
                    [],
                    Cinders: 0,
                    Soulstones: GetMasterySoulstoneReward(dungeon.Tier),
                    Experience: 0,
                    Trace: []),
                "Mastery 10 Soulstones",
                cancellationToken);
        }

        await _dungeonRuns.MarkDungeonCompletedAsync(
            run.CharacterId,
            run.DungeonDefinitionId,
            run.CompletedAt ?? DateTimeOffset.UtcNow,
            cancellationToken);
    }

    private async Task RollRewardTableAndAddAsync(
        Guid dungeonRunId,
        string rewardTableId,
        string source,
        int completionCurrencyBonusPercent,
        IReadOnlySet<string>? excludedRollIds,
        CancellationToken cancellationToken)
    {
        var result = _rewardRoller.Roll(
            rewardTableId,
            new RewardRollContext(source, ExcludedRollIds: excludedRollIds));

        result = ApplyCompletionCurrencyBonus(result, completionCurrencyBonusPercent);

        await AddRewardRollResultAsync(
            dungeonRunId,
            result,
            source,
            cancellationToken);
    }

    private static int GetMasterySoulstoneReward(int dungeonTier) => dungeonTier switch
    {
        1 => 50,
        2 => 100,
        _ => 200
    };

    private static RewardRollResult ApplyCompletionCurrencyBonus(
        RewardRollResult result,
        int bonusPercent)
    {
        if (bonusPercent <= 0)
        {
            return result;
        }

        return result with
        {
            Cinders = AddPercentageBonus(result.Cinders, bonusPercent),
            Soulstones = AddPercentageBonus(result.Soulstones, bonusPercent)
        };
    }

    private static int AddPercentageBonus(int value, int bonusPercent)
    {
        if (value <= 0)
        {
            return value;
        }

        var bonus = Math.Max(1, (int)Math.Round(
            value * bonusPercent / 100d,
            MidpointRounding.AwayFromZero));
        return checked(value + bonus);
    }

    private async Task AddRewardRollResultAsync(
        Guid dungeonRunId,
        RewardRollResult result,
        string source,
        CancellationToken cancellationToken)
    {
        if (result.Cinders > 0 || result.Soulstones > 0 || result.Experience > 0)
        {
            var run = await _dungeonRuns.GetDungeonRunByDungeonIdAsync(dungeonRunId, cancellationToken);
            if (run is not null)
            {
                run.PendingCinders += result.Cinders;
                run.PendingSoulstones += result.Soulstones;
                run.PendingExperience += result.Experience;
                await _dungeonRuns.UpdateDungeonRunAsync(run, cancellationToken);
            }
        }

        if (result.Items.Count == 0)
        {
            return;
        }

        var itemIds = result.Items
            .Select(x => x.ItemId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);

        var loot = result.Items
            .Where(item => itemBases.ContainsKey(item.ItemId))
            .GroupBy(item => item.ItemId, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => _inventoryItemFactory.CreateForQuantity(
                itemBases[group.Key],
                group.Sum(item => item.Quantity)))
            .ToList();

        if (loot.Count == 0)
        {
            return;
        }

        await _pendingRewardWriter.AddLootAsync(
            dungeonRunId,
            loot,
            source,
            cancellationToken);
    }

    private async Task AddMonsterCoreRewardsAsync(
        Guid dungeonRunId,
        DungeonGrade dungeonGrade,
        CancellationToken cancellationToken)
    {
        var grants = RollMonsterCoreGrants(dungeonGrade);
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(grants.Keys.ToList(), cancellationToken);
        var loot = grants
            .Where(grant => itemBases.ContainsKey(grant.Key))
            .SelectMany(grant => _inventoryItemFactory.CreateForQuantity(itemBases[grant.Key], grant.Value))
            .ToList();

        if (loot.Count == 0)
        {
            return;
        }

        await _pendingRewardWriter.AddLootAsync(
            dungeonRunId,
            loot,
            $"{FormatGrade(dungeonGrade)} Monster Cores",
            cancellationToken);
    }

    private async Task AddFirstCompletionRewardsIfNeededAsync(
        DungeonRun run,
        Domain.Models.Dungeons.DungeonDefinition dungeon,
        bool isFirstCompletion,
        CancellationToken cancellationToken)
    {
        if (!isFirstCompletion)
        {
            return;
        }

        var grants = DungeonRewardCatalog.GetFirstCompletionGrants(dungeon);
        await AddItemGrantsAsync(
            run.Id,
            grants,
            $"{FormatGrade(dungeon.Grade)} First Completion",
            cancellationToken);
    }

    private async Task AddItemGrantsAsync(
        Guid dungeonRunId,
        IReadOnlyList<DungeonRewardGrant> grants,
        string source,
        CancellationToken cancellationToken)
    {
        var rolled = RollItemGrants(grants);
        if (rolled.Count == 0)
        {
            return;
        }

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(rolled.Keys.ToList(), cancellationToken);
        var loot = rolled
            .Where(grant => itemBases.ContainsKey(grant.Key))
            .SelectMany(grant => _inventoryItemFactory.CreateForQuantity(itemBases[grant.Key], grant.Value))
            .ToList();

        if (loot.Count == 0)
        {
            return;
        }

        await _pendingRewardWriter.AddLootAsync(
            dungeonRunId,
            loot,
            source,
            cancellationToken);
    }

    private static Dictionary<string, int> RollItemGrants(IReadOnlyList<DungeonRewardGrant> grants)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var grant in grants)
        {
            if (string.IsNullOrWhiteSpace(grant.ItemId)) continue;
            if (Random.Shared.NextDouble() > grant.Chance) continue;

            var min = Math.Max(0, grant.MinAmount);
            var max = Math.Max(min, grant.MaxAmount);
            var amount = max == min ? min : Random.Shared.Next(min, max + 1);
            if (amount <= 0) continue;

            result[grant.ItemId] = result.GetValueOrDefault(grant.ItemId) + amount;
        }

        return result;
    }

    private static Dictionary<string, int> RollMonsterCoreGrants(DungeonGrade dungeonGrade)
    {
        var grants = dungeonGrade switch
        {
            DungeonGrade.GradeII => new Dictionary<string, int>
            {
                [EssenceProgressionConstants.GreaterMonsterCoreItemId] = Random.Shared.Next(2, 5),
                [EssenceProgressionConstants.LesserMonsterCoreItemId] = Random.Shared.Next(2, 5)
            },
            DungeonGrade.GradeIII => new Dictionary<string, int>
            {
                [EssenceProgressionConstants.PrimalMonsterCoreItemId] = Random.Shared.Next(1, 4),
                [EssenceProgressionConstants.GreaterMonsterCoreItemId] = Random.Shared.Next(2, 5),
                [EssenceProgressionConstants.LesserMonsterCoreItemId] = Random.Shared.Next(4, 9)
            },
            _ => new Dictionary<string, int>
            {
                [EssenceProgressionConstants.LesserMonsterCoreItemId] = Random.Shared.Next(3, 6)
            }
        };

        AddBonusStoneChance(grants, dungeonGrade);
        return grants;
    }

    private static void AddBonusStoneChance(Dictionary<string, int> grants, DungeonGrade dungeonGrade)
    {
        const double bonusChance = 0.25;
        if (Random.Shared.NextDouble() >= bonusChance)
        {
            return;
        }

        var itemId = dungeonGrade switch
        {
            DungeonGrade.GradeII => EssenceProgressionConstants.GreaterMonsterCoreItemId,
            DungeonGrade.GradeIII => Random.Shared.NextDouble() < 0.5
                ? EssenceProgressionConstants.PrimalMonsterCoreItemId
                : EssenceProgressionConstants.GreaterMonsterCoreItemId,
            _ => EssenceProgressionConstants.LesserMonsterCoreItemId
        };

        grants[itemId] = grants.GetValueOrDefault(itemId) + 1;
    }

    private static string FormatGrade(DungeonGrade grade) =>
        grade switch
        {
            DungeonGrade.GradeII => "Grade II",
            DungeonGrade.GradeIII => "Grade III",
            _ => "Grade I"
        };
}
