using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Common.Exceptions;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed class DungeonCompletionRewardApplier : IDungeonCompletionRewardApplier
{
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly ILootTableRepository _lootTables;
    private readonly IItemBaseRepository _itemBases;
    private readonly ILootService _lootService;
    private readonly IDungeonPendingRewardWriter _pendingRewardWriter;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IDungeonMasteryService _mastery;

    public DungeonCompletionRewardApplier(
        IDungeonDefinitions dungeonDefinitions,
        IDungeonRunRepository dungeonRuns,
        ILootTableRepository lootTables,
        IItemBaseRepository itemBases,
        ILootService lootService,
        IDungeonPendingRewardWriter pendingRewardWriter,
        IInventoryItemFactory inventoryItemFactory,
        IDungeonMasteryService mastery)
    {
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonRuns = dungeonRuns;
        _lootTables = lootTables;
        _itemBases = itemBases;
        _lootService = lootService;
        _pendingRewardWriter = pendingRewardWriter;
        _inventoryItemFactory = inventoryItemFactory;
        _mastery = mastery;
    }

    public async Task ApplyAsync(DungeonRun run, CancellationToken cancellationToken)
    {
        var dungeon = _dungeonDefinitions.GetByKey(run.DungeonDefinitionId);

        if (dungeon.CompletionLootTableId.HasValue)
        {
            await RollAndAddAsync(
                run.Id,
                dungeon.CompletionLootTableId.Value,
                "Dungeon Completion",
                cancellationToken);
        }

        if (dungeon.TierLootTableId.HasValue)
        {
            await RollAndAddAsync(
                run.Id,
                dungeon.TierLootTableId.Value,
                $"Tier {dungeon.Tier} Completion",
                cancellationToken);
        }

        await AddMonsterCoreRewardsAsync(run.Id, dungeon.Grade, cancellationToken);
        await AddFirstCompletionRewardsIfNeededAsync(run, dungeon, cancellationToken);
        await _mastery.AwardCompletionAsync(run, cancellationToken);
        await _dungeonRuns.MarkDungeonCompletedAsync(
            run.CharacterId,
            run.DungeonDefinitionId,
            run.CompletedAt ?? DateTimeOffset.UtcNow,
            cancellationToken);
    }

    private async Task RollAndAddAsync(
        Guid dungeonRunId,
        Guid lootTableId,
        string source,
        CancellationToken cancellationToken)
    {
        var lootTable = await TryGetLootTableAsync(
            lootTableId,
            cancellationToken);
        if (lootTable is null)
        {
            return;
        }

        var loot = _lootService.GenerateDungeonLoot(lootTable);

        await _pendingRewardWriter.AddLootAsync(
            dungeonRunId,
            loot,
            source,
            cancellationToken);
    }

    private async Task<LootTable?> TryGetLootTableAsync(Guid lootTableId, CancellationToken cancellationToken)
    {
        try
        {
            return await _lootTables.GetLootTableByIdAsync(lootTableId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (NotFoundException)
        {
            return null;
        }
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
        CancellationToken cancellationToken)
    {
        if (await _dungeonRuns.HasCompletedDungeonAsync(run.CharacterId, run.DungeonDefinitionId, cancellationToken))
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
