using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Prophecies;
using Services.LL.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Services.LL.Prophecies;

public sealed class ProphecyService : IProphecyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<WeightedProphecyCacheReward>> CacheRewardTables = CreateCacheRewardTables();

    private readonly IReadOnlyList<ProphecyDefinition> _definitions;
    private readonly IProphecyRepository _repository;
    private readonly ICharacterService _characterService;
    private readonly IEntityService _entityService;
    private readonly ILevelingService _levelingService;
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IItemBaseRepository _itemBases;

    public ProphecyService(
        IProphecyDefinitionProvider definitionProvider,
        IProphecyRepository repository,
        ICharacterService characterService,
        IEntityService entityService,
        ILevelingService levelingService,
        IInventoryService inventoryService,
        IInventoryRepository inventoryRepository,
        IItemBaseRepository itemBases)
    {
        _definitions = definitionProvider.GetAll();
        _repository = repository;
        _characterService = characterService;
        _entityService = entityService;
        _levelingService = levelingService;
        _inventoryService = inventoryService;
        _inventoryRepository = inventoryRepository;
        _itemBases = itemBases;
    }

    public async Task<PropheciesOverview> GetOverviewAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var definitions = await _repository.SyncDefinitionsAsync(_definitions, cancellationToken);

        var dailyPeriod = GetDailyPeriod(now);
        var weeklyPeriod = GetWeeklyPeriod(now);

        var daily = await EnsureDailyInstancesAsync(definitions, playerId, characterId, dailyPeriod.Start, dailyPeriod.End, now, cancellationToken);
        var greater = await EnsureGreaterProphecyAsync(definitions, playerId, characterId, weeklyPeriod.Start, weeklyPeriod.End, now, cancellationToken);
        var weekly = await EnsureWeeklyProgressAsync(playerId, characterId, weeklyPeriod.Start, weeklyPeriod.End, now, cancellationToken);
        var recent = await _repository.GetRecentInstancesAsync(
            playerId,
            characterId,
            now.AddDays(-14),
            12,
            cancellationToken);

        ExpireOldUnfinished(daily, now);
        if (greater.Status is ProphecyStatus.Offered)
        {
            greater.Status = ProphecyStatus.Accepted;
            greater.AcceptedAt = greater.GeneratedAt;
        }

        foreach (var prophecy in daily)
        {
            RebalanceTargetIfHigher(prophecy);
            MarkCompletedIfTargetReached(prophecy, now);
        }

        RebalanceTargetIfHigher(greater);
        MarkCompletedIfTargetReached(greater, now);

        var currentIds = daily.Select(x => x.Id).Append(greater.Id).ToHashSet();
        recent = recent.Where(x => !currentIds.Contains(x.Id)).ToList();
        var weeklyMilestones = CreateWeeklyMilestones(weekly);
        var caches = await GetCacheInventoryAsync(characterId, cancellationToken);

        return new PropheciesOverview(
            now,
            daily,
            daily.FirstOrDefault(IsAcceptedOrLater),
            greater,
            weekly,
            recent,
            weeklyMilestones,
            caches);
    }

    public async Task<ProphecyOperationResult<PropheciesOverview>> AcceptAsync(
        Guid playerId,
        Guid characterId,
        Guid prophecyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var overview = await GetOverviewAsync(playerId, characterId, now, cancellationToken);
        var prophecy = overview.DailyProphecies.FirstOrDefault(x => x.Id == prophecyId);

        if (prophecy is null)
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("Prophecy was not found for the current daily period.");
        }

        if (prophecy.Status != ProphecyStatus.Offered)
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("Only offered prophecies can be accepted.");
        }

        if (prophecy.PeriodEnd <= now)
        {
            prophecy.Status = ProphecyStatus.Expired;
            return ProphecyOperationResult<PropheciesOverview>.Fail("This prophecy has expired.");
        }

        if (overview.DailyProphecies.Any(x => x.Id != prophecy.Id && IsAcceptedOrLater(x)))
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("A daily prophecy has already been accepted for this period.");
        }

        prophecy.Status = ProphecyStatus.Accepted;
        prophecy.AcceptedAt = now;

        foreach (var other in overview.DailyProphecies.Where(x => x.Id != prophecy.Id && x.Status == ProphecyStatus.Offered))
        {
            other.Status = ProphecyStatus.Declined;
        }

        return ProphecyOperationResult<PropheciesOverview>.Success(overview with { ActiveDailyProphecy = prophecy });
    }

    public async Task<ProphecyOperationResult<ProphecyClaimResult>> ClaimAsync(
        Guid playerId,
        Guid characterId,
        Guid prophecyId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var prophecy = await _repository.GetInstanceAsync(prophecyId, playerId, characterId, cancellationToken);
        if (prophecy is null)
        {
            return ProphecyOperationResult<ProphecyClaimResult>.Fail("Prophecy was not found.");
        }

        if (prophecy.Status == ProphecyStatus.Claimed)
        {
            return ProphecyOperationResult<ProphecyClaimResult>.Fail("This prophecy has already been claimed.");
        }

        RebalanceTargetIfHigher(prophecy);
        MarkCompletedIfTargetReached(prophecy, now);

        if (prophecy.Status != ProphecyStatus.Completed)
        {
            return ProphecyOperationResult<ProphecyClaimResult>.Fail("Only completed prophecies can be claimed.");
        }

        var weeklyPeriod = GetWeeklyPeriod(now);
        var weekly = await EnsureWeeklyProgressAsync(playerId, characterId, weeklyPeriod.Start, weeklyPeriod.End, now, cancellationToken);
        var reward = ReadReward(prophecy.RewardSnapshotJson);

        await ApplyRewardAsync(characterId, reward, cancellationToken);

        if (reward.PropheticFavor > 0)
        {
            weekly.PropheticFavor = Math.Min(7, weekly.PropheticFavor + reward.PropheticFavor);
            weekly.UpdatedAt = now;
        }

        prophecy.Status = ProphecyStatus.Claimed;
        prophecy.ClaimedAt = now;

        return ProphecyOperationResult<ProphecyClaimResult>.Success(
            new ProphecyClaimResult(prophecy, reward, weekly, CreateWeeklyMilestones(weekly)));
    }

    public async Task<ProphecyOperationResult<WeeklyRevelationClaimResult>> ClaimWeeklyMilestoneAsync(
        Guid playerId,
        Guid characterId,
        int favorRequired,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var weeklyPeriod = GetWeeklyPeriod(now);
        var weekly = await EnsureWeeklyProgressAsync(playerId, characterId, weeklyPeriod.Start, weeklyPeriod.End, now, cancellationToken);

        if (favorRequired is not (3 or 5 or 7))
        {
            return ProphecyOperationResult<WeeklyRevelationClaimResult>.Fail("Unknown weekly revelation milestone.");
        }

        if (weekly.PropheticFavor < favorRequired)
        {
            return ProphecyOperationResult<WeeklyRevelationClaimResult>.Fail("Not enough Prophetic Favor for this milestone.");
        }

        if (IsMilestoneClaimed(weekly, favorRequired))
        {
            return ProphecyOperationResult<WeeklyRevelationClaimResult>.Fail("This weekly milestone has already been claimed.");
        }

        var reward = CreateWeeklyMilestoneReward(favorRequired);
        await ApplyRewardAsync(characterId, reward, cancellationToken);

        SetMilestoneClaimed(weekly, favorRequired);
        weekly.UpdatedAt = now;

        return ProphecyOperationResult<WeeklyRevelationClaimResult>.Success(
            new WeeklyRevelationClaimResult(favorRequired, reward, weekly, CreateWeeklyMilestones(weekly)));
    }

    public async Task<ProphecyOperationResult<ProphecyCacheOpenResult>> OpenCacheAsync(
        Guid characterId,
        string cacheItemId,
        CancellationToken cancellationToken)
    {
        var cache = CreateCacheItemBases()
            .FirstOrDefault(x => x.Id.Equals(cacheItemId, StringComparison.OrdinalIgnoreCase));

        if (cache is null)
        {
            return ProphecyOperationResult<ProphecyCacheOpenResult>.Fail("Unknown prophecy cache.");
        }

        await EnsureCacheItemBasesAsync([cache.Id], cancellationToken);

        var removed = await _inventoryRepository.TryRemoveItemsByBaseIdAsync(
            characterId,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { [cache.Id] = 1 },
            cancellationToken);

        if (!removed)
        {
            return ProphecyOperationResult<ProphecyCacheOpenResult>.Fail("You do not have that prophecy cache.");
        }

        var reward = CreateCacheOpenReward(cache.Id);
        await ApplyRewardAsync(characterId, reward, cancellationToken);

        return ProphecyOperationResult<ProphecyCacheOpenResult>.Success(
            new ProphecyCacheOpenResult(
                cache.Id,
                reward,
                await GetCacheInventoryAsync(characterId, cancellationToken)));
    }

    public async Task<IReadOnlyList<ProphecyProgressUpdate>> TrackProgressAsync(
        ProphecyProgressEvent progressEvent,
        CancellationToken cancellationToken) =>
        await TrackProgressAsync([progressEvent], cancellationToken);

    public async Task<IReadOnlyList<ProphecyProgressUpdate>> TrackProgressAsync(
        IReadOnlyList<ProphecyProgressEvent> progressEvents,
        CancellationToken cancellationToken)
    {
        if (progressEvents.Count == 0)
        {
            return [];
        }

        var characterIds = progressEvents
            .Select(x => x.CharacterId)
            .Distinct()
            .ToArray();

        if (characterIds.Length != 1)
        {
            throw new InvalidOperationException("Prophecy progress batches must target a single character.");
        }

        var from = progressEvents.Min(x => x.OccurredAt);
        var to = progressEvents.Max(x => x.OccurredAt);
        var active = await _repository.GetAcceptedInstancesForProgressWindowAsync(
            characterIds[0],
            from,
            to,
            cancellationToken);

        var updates = new List<ProphecyProgressUpdate>();
        foreach (var progressEvent in progressEvents.OrderBy(x => x.OccurredAt))
        {
            AddProgressUpdates(active, progressEvent, updates);
        }

        return updates;
    }

    private void AddProgressUpdates(
        IEnumerable<PlayerProphecyInstance> active,
        ProphecyProgressEvent progressEvent,
        List<ProphecyProgressUpdate> updates)
    {
        foreach (var prophecy in active.Where(x => IsActiveForProgress(x, progressEvent.OccurredAt)))
        {
            var previousValue = prophecy.CurrentValue;
            if (!TryApplyProgress(prophecy, progressEvent))
            {
                continue;
            }

            var completed = false;
            if (prophecy.CurrentValue >= prophecy.TargetValue)
            {
                prophecy.CurrentValue = prophecy.TargetValue;
                prophecy.Status = ProphecyStatus.Completed;
                prophecy.CompletedAt = progressEvent.OccurredAt;
                completed = true;
            }

            updates.Add(new ProphecyProgressUpdate(
                progressEvent.CharacterId,
                prophecy.Id,
                prophecy.ProphecyDefinition?.Title ?? prophecy.ProphecyDefinitionId,
                prophecy.Scope.ToString(),
                prophecy.SlotType.ToString(),
                prophecy.Status.ToString(),
                prophecy.CurrentValue,
                prophecy.TargetValue,
                Math.Max(0, prophecy.CurrentValue - previousValue),
                completed));
        }
    }

    private static bool IsActiveForProgress(PlayerProphecyInstance prophecy, DateTimeOffset occurredAt) =>
        prophecy.Status == ProphecyStatus.Accepted &&
        prophecy.AcceptedAt <= occurredAt &&
        prophecy.PeriodStart <= occurredAt &&
        prophecy.PeriodEnd > occurredAt;

    private async Task<IReadOnlyList<PlayerProphecyInstance>> EnsureDailyInstancesAsync(
        IReadOnlyList<ProphecyDefinition> definitions,
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetInstancesForPeriodAsync(
            playerId,
            characterId,
            ProphecyScope.Daily,
            periodStart,
            periodEnd,
            cancellationToken);

        if (existing.Count == 3)
        {
            return existing;
        }

        var existingSlots = existing.Select(x => x.SlotType).ToHashSet();
        var generated = new List<PlayerProphecyInstance>();

        foreach (var slot in new[] { ProphecySlotType.Steady, ProphecySlotType.Focused, ProphecySlotType.Ominous })
        {
            if (existingSlots.Contains(slot))
            {
                continue;
            }

            generated.Add(CreateInstance(
                playerId,
                characterId,
                PickDefinition(definitions, ProphecyScope.Daily, slot, characterId, periodStart),
                ProphecyScope.Daily,
                slot,
                ProphecyStatus.Offered,
                periodStart,
                periodEnd,
                now));
        }

        await _repository.AddInstancesAsync(generated, cancellationToken);
        return existing.Concat(generated).OrderBy(x => x.SlotType).ToList();
    }

    private async Task<PlayerProphecyInstance> EnsureGreaterProphecyAsync(
        IReadOnlyList<ProphecyDefinition> definitions,
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetInstancesForPeriodAsync(
            playerId,
            characterId,
            ProphecyScope.Weekly,
            periodStart,
            periodEnd,
            cancellationToken);

        var greater = existing.FirstOrDefault(x => x.SlotType == ProphecySlotType.Greater);
        if (greater is not null)
        {
            return greater;
        }

        greater = CreateInstance(
            playerId,
            characterId,
            PickDefinition(definitions, ProphecyScope.Weekly, ProphecySlotType.Greater, characterId, periodStart),
            ProphecyScope.Weekly,
            ProphecySlotType.Greater,
            ProphecyStatus.Accepted,
            periodStart,
            periodEnd,
            now);
        greater.AcceptedAt = now;

        await _repository.AddInstancesAsync([greater], cancellationToken);
        return greater;
    }

    private async Task<WeeklyRevelationProgress> EnsureWeeklyProgressAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetWeeklyProgressAsync(playerId, characterId, periodStart, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var progress = new WeeklyRevelationProgress
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CharacterId = characterId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            CreatedAt = now
        };

        await _repository.AddWeeklyProgressAsync(progress, cancellationToken);
        return progress;
    }

    private static PlayerProphecyInstance CreateInstance(
        Guid playerId,
        Guid characterId,
        ProphecyDefinition definition,
        ProphecyScope scope,
        ProphecySlotType slot,
        ProphecyStatus status,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset now)
    {
        var target = GetTargetValue(definition);
        return new PlayerProphecyInstance
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CharacterId = characterId,
            ProphecyDefinitionId = definition.Id,
            ProphecyDefinition = definition,
            Scope = scope,
            SlotType = slot,
            Status = status,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            GeneratedAt = now,
            TargetValue = target,
            ObjectiveParameterSnapshotJson = definition.ObjectiveParameterJson,
            ProgressJson = "{}",
            RewardSnapshotJson = JsonSerializer.Serialize(CreateRewardSnapshot(definition), JsonOptions)
        };
    }

    private ProphecyDefinition PickDefinition(
        IReadOnlyList<ProphecyDefinition> definitions,
        ProphecyScope scope,
        ProphecySlotType slot,
        Guid characterId,
        DateTimeOffset periodStart)
    {
        var slotName = slot.ToString();
        var candidates = definitions
            .Where(x => x.IsEnabled && x.Scope == scope && x.AllowedSlots.Contains(slotName))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = definitions
                .Where(x => x.IsEnabled && x.Scope == scope && x.Category == ProphecyCategory.Combat)
                .ToList();
        }

        var totalWeight = candidates.Sum(x => Math.Max(1, x.Weight));
        var roll = Math.Abs(GetStableInt($"{characterId:N}:{periodStart:O}:{slotName}:{scope}")) % totalWeight;

        foreach (var candidate in candidates)
        {
            roll -= Math.Max(1, candidate.Weight);
            if (roll < 0)
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private bool TryApplyProgress(PlayerProphecyInstance prophecy, ProphecyProgressEvent progressEvent)
    {
        var objective = prophecy.ProphecyDefinition?.ObjectiveType ?? string.Empty;
        var progress = ReadProgress(prophecy.ProgressJson);

        switch (objective)
        {
            case ProphecyObjectiveType.KillCreatures when progressEvent.Kind == ProphecyProgressKind.CreatureDefeated:
                prophecy.CurrentValue += Math.Max(1, progressEvent.Amount);
                return true;

            case ProphecyObjectiveType.KillDifferentCreatureTypes when progressEvent.Kind == ProphecyProgressKind.CreatureDefeated && !string.IsNullOrWhiteSpace(progressEvent.CreatureDefinitionId):
                if (progress.UniqueIds.Contains(progressEvent.CreatureDefinitionId, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                progress.UniqueIds.Add(progressEvent.CreatureDefinitionId);
                prophecy.ProgressJson = JsonSerializer.Serialize(progress, JsonOptions);
                prophecy.CurrentValue = progress.UniqueIds.Count;
                return true;

            case ProphecyObjectiveType.WinEncounters when progressEvent.Kind == ProphecyProgressKind.EncounterWon:
                if (!MeetsMinimumEnemyCount(prophecy.ObjectiveParameterSnapshotJson, progressEvent.EnemyCount))
                {
                    return false;
                }

                prophecy.CurrentValue += Math.Max(1, progressEvent.Amount);
                return true;

            case ProphecyObjectiveType.ClearDungeonRooms when progressEvent.Kind == ProphecyProgressKind.DungeonRoomCleared:
            case ProphecyObjectiveType.CompleteDungeons when progressEvent.Kind == ProphecyProgressKind.DungeonCompleted:
            case ProphecyObjectiveType.ResolveDungeonEvents when progressEvent.Kind == ProphecyProgressKind.DungeonEventResolved:
            case ProphecyObjectiveType.GainEssenceXp when progressEvent.Kind == ProphecyProgressKind.EssenceXpGained:
            case ProphecyObjectiveType.EssenceArchivedOrFed when progressEvent.Kind == ProphecyProgressKind.EssenceArchived:
            case ProphecyObjectiveType.GatherResources when progressEvent.Kind == ProphecyProgressKind.ResourceGathered:
            case ProphecyObjectiveType.TemperItems when progressEvent.Kind == ProphecyProgressKind.ItemTempered:
            case ProphecyObjectiveType.TreasureProgress when progressEvent.Kind == ProphecyProgressKind.TreasureProgress:
                prophecy.CurrentValue += Math.Max(1, progressEvent.Amount);
                return true;

            case ProphecyObjectiveType.SpendPotential when progressEvent.Kind == ProphecyProgressKind.PotentialSpent:
                prophecy.CurrentValue += Math.Max(1, progressEvent.PotentialSpent ?? progressEvent.Amount);
                return true;

            case ProphecyObjectiveType.MeaningfulDefeatThenWins when progressEvent.Kind == ProphecyProgressKind.EncounterLost:
                progress.HasMeaningfulDefeat = true;
                prophecy.ProgressJson = JsonSerializer.Serialize(progress, JsonOptions);
                return false;

            case ProphecyObjectiveType.MeaningfulDefeatThenWins when progressEvent.Kind == ProphecyProgressKind.EncounterWon && progress.HasMeaningfulDefeat:
                prophecy.CurrentValue += Math.Max(1, progressEvent.Amount);
                return true;

            default:
                return false;
        }
    }

    private async Task ApplyRewardAsync(Guid characterId, ProphecyRewardSnapshot reward, CancellationToken cancellationToken)
    {
        if (reward.Cinders <= 0 &&
            reward.Soulstones <= 0 &&
            reward.CharacterExperience <= 0 &&
            reward.SigilFragments <= 0 &&
            reward.AscensionStoneFragments <= 0 &&
            reward.FateEcho <= 0 &&
            !HasInventoryReward(reward))
        {
            return;
        }

        if (HasCharacterReward(reward))
        {
            var character = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
            if (character is null)
            {
                throw new InvalidOperationException($"Could not apply prophecy reward. Character '{characterId}' was not found.");
            }

            character.Cinders += reward.Cinders;
            character.Soulstones += reward.Soulstones;
            character.SigilFragments += reward.SigilFragments;
            character.AscensionStoneFragments += reward.AscensionStoneFragments;
            character.FateEcho += reward.FateEcho;

            if (reward.CharacterExperience > 0)
            {
                character.Experience += reward.CharacterExperience;
                await _levelingService.UpdateCharacterLevel(character, cancellationToken);
            }

            _entityService.UpdateEntities([character]);
        }

        if (HasInventoryReward(reward))
        {
            await _inventoryService.AddItemsToInventory(characterId, await CreateRewardInventoryItemsAsync(characterId, reward, cancellationToken), cancellationToken);
        }
    }

    private async Task<List<InventoryItem>> CreateRewardInventoryItemsAsync(
        Guid characterId,
        ProphecyRewardSnapshot reward,
        CancellationToken cancellationToken)
    {
        var grants = CollectInventoryRewardQuantities(reward);
        if (grants.Count == 0)
        {
            return [];
        }

        var cacheItemBases = await EnsureCacheItemBasesAsync(grants.Keys.ToList(), cancellationToken);
        var existingItemBases = await _itemBases.GetItemBasesByIdsAsync(grants.Keys.ToList(), cancellationToken);
        var itemBases = existingItemBases
            .Concat(cacheItemBases.Where(x => !existingItemBases.ContainsKey(x.Key)))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        var missing = grants.Keys
            .Where(x => !itemBases.ContainsKey(x))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Could not grant prophecy reward. Missing item bases: {string.Join(", ", missing)}.");
        }

        return grants.Select(grant =>
        {
            var itemBase = itemBases[grant.Key];
            var itemInstance = new ItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            };

            return new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = itemInstance.Id,
                ItemInstance = itemInstance,
                Quantity = grant.Value
            };
        }).ToList();
    }

    private async Task<IReadOnlyDictionary<string, ItemBase>> EnsureCacheItemBasesAsync(
        IReadOnlyCollection<string> rewardItemIds,
        CancellationToken cancellationToken)
    {
        var cacheDefinitions = CreateCacheItemBases()
            .Where(x => rewardItemIds.Contains(x.Id, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        if (cacheDefinitions.Count == 0)
        {
            return new Dictionary<string, ItemBase>(StringComparer.OrdinalIgnoreCase);
        }

        var existing = await _itemBases.GetItemBasesByIdsAsync(cacheDefinitions.Keys.ToList(), cancellationToken);
        var missing = cacheDefinitions
            .Where(x => !existing.ContainsKey(x.Key))
            .Select(x => x.Value)
            .ToList();

        await _itemBases.AddMissingItemBasesAsync(missing, cancellationToken);

        return existing
            .Concat(missing.Select(x => new KeyValuePair<string, ItemBase>(x.Id, x)))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<ProphecyCacheInventory>> GetCacheInventoryAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var definitions = CreateCacheItemBases();
        await EnsureCacheItemBasesAsync(definitions.Select(x => x.Id).ToList(), cancellationToken);

        var result = new List<ProphecyCacheInventory>();
        foreach (var definition in definitions)
        {
            result.Add(new ProphecyCacheInventory(
                definition.Id,
                definition.Name,
                definition.Description,
                await _inventoryRepository.GetInventoryQuantityAsync(characterId, definition.Id, cancellationToken)));
        }

        return result;
    }

    private static bool HasCharacterReward(ProphecyRewardSnapshot reward) =>
        reward.Cinders > 0 ||
        reward.Soulstones > 0 ||
        reward.CharacterExperience > 0 ||
        reward.SigilFragments > 0 ||
        reward.AscensionStoneFragments > 0 ||
        reward.FateEcho > 0;

    private static bool HasInventoryReward(ProphecyRewardSnapshot reward) =>
        !string.IsNullOrWhiteSpace(reward.CacheItemId) ||
        reward.Items.Any(x => x.Quantity > 0 && !string.IsNullOrWhiteSpace(x.ItemId));

    private static Dictionary<string, int> CollectInventoryRewardQuantities(ProphecyRewardSnapshot reward)
    {
        var grants = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(reward.CacheItemId))
        {
            grants[reward.CacheItemId] = 1;
        }

        foreach (var item in reward.Items.Where(x => x.Quantity > 0 && !string.IsNullOrWhiteSpace(x.ItemId)))
        {
            grants[item.ItemId] = grants.GetValueOrDefault(item.ItemId) + item.Quantity;
        }

        return grants;
    }

    private static IReadOnlyList<ItemBase> CreateCacheItemBases() =>
    [
        Cache("greater_prophecy_cache", "Greater Prophecy Cache", "A sealed cache earned by fulfilling a weekly Greater Prophecy.", Rarity.Rare),
        Cache("revelation_cache_small", "Small Revelation Cache", "A small cache granted by the Weekly Revelation.", Rarity.Uncommon),
        Cache("revelation_cache_greater", "Greater Revelation Cache", "A greater cache granted by the Weekly Revelation.", Rarity.Rare),
        Cache("revelation_cache_perfect_week", "Perfect Week Revelation Cache", "A rare cache for completing the Weekly Revelation.", Rarity.Epic)
    ];

    private static ItemBase Cache(string id, string name, string description, Rarity rarity) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            Stackable = true,
            IsBound = true,
            ItemType = ItemType.Resource,
            Rarity = rarity
        };

    private static ProphecyRewardSnapshot ReadReward(string json) =>
        JsonSerializer.Deserialize<ProphecyRewardSnapshot>(json, JsonOptions) ?? new ProphecyRewardSnapshot();

    private static ProphecyProgressSnapshot ReadProgress(string json) =>
        JsonSerializer.Deserialize<ProphecyProgressSnapshot>(json, JsonOptions) ?? new ProphecyProgressSnapshot();

    private static ProphecyRewardSnapshot CreateRewardSnapshot(ProphecyDefinition definition)
    {
        var difficulty = definition.Difficulty switch
        {
            ProphecyDifficulty.Common => 1,
            ProphecyDifficulty.Uncommon => 2,
            ProphecyDifficulty.Rare => 3,
            ProphecyDifficulty.Epic => 4,
            _ => 1
        };
        var weekly = definition.Scope == ProphecyScope.Weekly;
        var categoryBonus = definition.Category is ProphecyCategory.Dungeon ? 1 : 0;

        return new ProphecyRewardSnapshot
        {
            Cinders = weekly ? 300 + difficulty * 250 : 60 + difficulty * 45,
            CharacterExperience = weekly ? 200 + difficulty * 130 : 30 + difficulty * 25,
            Soulstones = weekly ? difficulty : difficulty >= 3 ? 1 : 0,
            SigilFragments = definition.Category == ProphecyCategory.Dungeon ? (weekly ? 8 + difficulty * 2 : 2 + categoryBonus) : 0,
            AscensionStoneFragments = definition.Category == ProphecyCategory.Dungeon ? (weekly ? 5 + difficulty : categoryBonus) : 0,
            PropheticFavor = weekly ? 0 : 1,
            FateEcho = weekly ? 18 + difficulty * 8 : 4 + difficulty * 3,
            CacheItemId = weekly ? "greater_prophecy_cache" : null
        };
    }

    private static ProphecyRewardSnapshot CreateWeeklyMilestoneReward(int favorRequired) =>
        favorRequired switch
        {
            3 => new ProphecyRewardSnapshot { Cinders = 150, Soulstones = 1, FateEcho = 10, CacheItemId = "revelation_cache_small" },
            5 => new ProphecyRewardSnapshot { Cinders = 350, Soulstones = 2, FateEcho = 20, CacheItemId = "revelation_cache_greater" },
            7 => new ProphecyRewardSnapshot { Cinders = 750, Soulstones = 5, FateEcho = 35, CacheItemId = "revelation_cache_perfect_week" },
            _ => new ProphecyRewardSnapshot()
        };

    private static IReadOnlyList<WeeklyRevelationMilestone> CreateWeeklyMilestones(WeeklyRevelationProgress progress) =>
    [
        CreateWeeklyMilestone(progress, 3, "Small Revelation Cache"),
        CreateWeeklyMilestone(progress, 5, "Greater Revelation Cache"),
        CreateWeeklyMilestone(progress, 7, "Perfect Week Bonus")
    ];

    private static WeeklyRevelationMilestone CreateWeeklyMilestone(
        WeeklyRevelationProgress progress,
        int favorRequired,
        string title) =>
        new(
            favorRequired,
            title,
            progress.PropheticFavor >= favorRequired,
            IsMilestoneClaimed(progress, favorRequired),
            CreateWeeklyMilestoneReward(favorRequired));

    private static ProphecyRewardSnapshot CreateCacheOpenReward(string cacheItemId)
    {
        if (!CacheRewardTables.TryGetValue(cacheItemId, out var table) || table.Count == 0)
        {
            return new ProphecyRewardSnapshot();
        }

        var rolls = cacheItemId switch
        {
            "revelation_cache_small" => 2,
            "revelation_cache_greater" => 3,
            "revelation_cache_perfect_week" => 4,
            "greater_prophecy_cache" => 3,
            _ => 1
        };

        var reward = new ProphecyRewardSnapshot();
        for (var i = 0; i < rolls; i++)
        {
            AddReward(reward, RollCacheReward(table));
        }

        return reward;
    }

    private static ProphecyRewardSnapshot RollCacheReward(IReadOnlyList<WeightedProphecyCacheReward> table)
    {
        var totalWeight = table.Sum(x => Math.Max(1, x.Weight));
        var roll = RandomNumberGenerator.GetInt32(totalWeight);

        foreach (var entry in table)
        {
            roll -= Math.Max(1, entry.Weight);
            if (roll < 0)
            {
                return entry.Reward;
            }
        }

        return table[^1].Reward;
    }

    private static void AddReward(ProphecyRewardSnapshot target, ProphecyRewardSnapshot reward)
    {
        target.Cinders += reward.Cinders;
        target.CharacterExperience += reward.CharacterExperience;
        target.EssenceExperience += reward.EssenceExperience;
        target.Soulstones += reward.Soulstones;
        target.SigilFragments += reward.SigilFragments;
        target.AscensionStoneFragments += reward.AscensionStoneFragments;
        target.PropheticFavor += reward.PropheticFavor;
        target.FateEcho += reward.FateEcho;

        foreach (var item in reward.Items.Where(x => x.Quantity > 0 && !string.IsNullOrWhiteSpace(x.ItemId)))
        {
            target.Items.Add(item);
        }

        if (!string.IsNullOrWhiteSpace(reward.CacheItemId))
        {
            target.CacheItemId = reward.CacheItemId;
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<WeightedProphecyCacheReward>> CreateCacheRewardTables() =>
        new Dictionary<string, IReadOnlyList<WeightedProphecyCacheReward>>(StringComparer.OrdinalIgnoreCase)
        {
            ["revelation_cache_small"] =
            [
                CacheReward(45, cinders: 80),
                CacheReward(30, fateEcho: 6),
                CacheReward(15, cinders: 120, fateEcho: 4),
                CacheReward(10, soulstones: 1)
            ],
            ["revelation_cache_greater"] =
            [
                CacheReward(35, cinders: 150, fateEcho: 8),
                CacheReward(25, soulstones: 1, fateEcho: 10),
                CacheReward(25, sigilFragments: 2, cinders: 100),
                CacheReward(10, ascensionStoneFragments: 1, fateEcho: 12),
                CacheReward(5, soulstones: 2, sigilFragments: 3)
            ],
            ["revelation_cache_perfect_week"] =
            [
                CacheReward(30, cinders: 275, fateEcho: 18),
                CacheReward(25, soulstones: 2, sigilFragments: 3),
                CacheReward(20, ascensionStoneFragments: 2, fateEcho: 16),
                CacheReward(15, cinders: 450, soulstones: 1),
                CacheReward(10, soulstones: 3, sigilFragments: 5, ascensionStoneFragments: 2)
            ],
            ["greater_prophecy_cache"] =
            [
                CacheReward(35, cinders: 225, fateEcho: 14),
                CacheReward(25, soulstones: 1, sigilFragments: 2),
                CacheReward(20, cinders: 325, soulstones: 1),
                CacheReward(15, ascensionStoneFragments: 1, fateEcho: 18),
                CacheReward(5, soulstones: 2, sigilFragments: 4, ascensionStoneFragments: 1)
            ]
        };

    private static WeightedProphecyCacheReward CacheReward(
        int weight,
        long cinders = 0,
        int characterExperience = 0,
        int essenceExperience = 0,
        int soulstones = 0,
        int sigilFragments = 0,
        int ascensionStoneFragments = 0,
        int propheticFavor = 0,
        int fateEcho = 0) =>
        new(
            weight,
            new ProphecyRewardSnapshot
            {
                Cinders = cinders,
                CharacterExperience = characterExperience,
                EssenceExperience = essenceExperience,
                Soulstones = soulstones,
                SigilFragments = sigilFragments,
                AscensionStoneFragments = ascensionStoneFragments,
                PropheticFavor = propheticFavor,
                FateEcho = fateEcho
            });

    private static int GetTargetValue(ProphecyDefinition definition)
    {
        var weekly = definition.Scope == ProphecyScope.Weekly;
        var scale = definition.Difficulty switch
        {
            ProphecyDifficulty.Common => 1,
            ProphecyDifficulty.Uncommon => 2,
            ProphecyDifficulty.Rare => 3,
            ProphecyDifficulty.Epic => 4,
            _ => 1
        };

        return definition.ObjectiveType switch
        {
            ProphecyObjectiveType.KillCreatures => weekly ? 300 + scale * 100 : 35 + scale * 15,
            ProphecyObjectiveType.KillDifferentCreatureTypes => weekly ? 18 + scale * 8 : 4 + scale * 2,
            ProphecyObjectiveType.WinEncounters => weekly ? 140 + scale * 50 : 18 + scale * 8,
            ProphecyObjectiveType.ClearDungeonRooms => weekly ? 80 + scale * 30 : 8 + scale * 4,
            ProphecyObjectiveType.CompleteDungeons => weekly ? 8 + scale * 4 : 1 + scale,
            ProphecyObjectiveType.ResolveDungeonEvents => weekly ? 25 + scale * 10 : 4 + scale * 3,
            ProphecyObjectiveType.GainEssenceXp => weekly ? 2500 + scale * 1000 : 350 + scale * 150,
            ProphecyObjectiveType.EssenceArchivedOrFed => weekly ? 5 + scale * 2 : 1 + scale,
            ProphecyObjectiveType.GatherResources => weekly ? 280 + scale * 90 : 35 + scale * 15,
            ProphecyObjectiveType.TemperItems => weekly ? 45 + scale * 15 : 6 + scale * 3,
            ProphecyObjectiveType.SpendPotential => weekly ? 260 + scale * 80 : 35 + scale * 15,
            ProphecyObjectiveType.TreasureProgress => 100,
            ProphecyObjectiveType.MeaningfulDefeatThenWins => weekly ? 30 + scale * 12 : 6 + scale * 3,
            _ => weekly ? 120 : 25
        };
    }

    private static bool MeetsMinimumEnemyCount(string parameterJson, int? enemyCount)
    {
        if (enemyCount is null)
        {
            return true;
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(parameterJson) ? "{}" : parameterJson);
        if (!doc.RootElement.TryGetProperty("minimumEnemyCount", out var minimumElement) || minimumElement.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        return enemyCount.Value >= minimumElement.GetInt32();
    }

    private static void ExpireOldUnfinished(IReadOnlyList<PlayerProphecyInstance> instances, DateTimeOffset now)
    {
        foreach (var instance in instances.Where(x => x.PeriodEnd <= now && x.Status is ProphecyStatus.Offered or ProphecyStatus.Accepted or ProphecyStatus.Declined))
        {
            instance.Status = ProphecyStatus.Expired;
        }
    }

    private static bool IsAcceptedOrLater(PlayerProphecyInstance instance) =>
        instance.Status is ProphecyStatus.Accepted or ProphecyStatus.Completed or ProphecyStatus.Claimed;

    private void RebalanceTargetIfHigher(PlayerProphecyInstance instance)
    {
        if (instance.Status is not (ProphecyStatus.Offered or ProphecyStatus.Accepted))
        {
            return;
        }

        var definition = instance.ProphecyDefinition ??
            _definitions.FirstOrDefault(x => x.Id.Equals(instance.ProphecyDefinitionId, StringComparison.OrdinalIgnoreCase));

        if (definition is null)
        {
            return;
        }

        instance.TargetValue = Math.Max(instance.TargetValue, GetTargetValue(definition));
    }

    private static void MarkCompletedIfTargetReached(PlayerProphecyInstance instance, DateTimeOffset now)
    {
        if (instance.Status != ProphecyStatus.Accepted || instance.CurrentValue < instance.TargetValue)
        {
            return;
        }

        instance.CurrentValue = instance.TargetValue;
        instance.Status = ProphecyStatus.Completed;
        instance.CompletedAt ??= now;
    }

    private static bool IsMilestoneClaimed(WeeklyRevelationProgress progress, int favorRequired) =>
        favorRequired switch
        {
            3 => progress.Milestone3Claimed,
            5 => progress.Milestone5Claimed,
            7 => progress.Milestone7Claimed,
            _ => false
        };

    private static void SetMilestoneClaimed(WeeklyRevelationProgress progress, int favorRequired)
    {
        if (favorRequired == 3) progress.Milestone3Claimed = true;
        if (favorRequired == 5) progress.Milestone5Claimed = true;
        if (favorRequired == 7) progress.Milestone7Claimed = true;
    }

    private static (DateTimeOffset Start, DateTimeOffset End) GetDailyPeriod(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        var start = new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero);
        return (start, start.AddDays(1));
    }

    private static (DateTimeOffset Start, DateTimeOffset End) GetWeeklyPeriod(DateTimeOffset now)
    {
        var utc = now.ToUniversalTime();
        var date = new DateTimeOffset(utc.Year, utc.Month, utc.Day, 0, 0, 0, TimeSpan.Zero);
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        var start = date.AddDays(-daysSinceMonday);
        return (start, start.AddDays(7));
    }

    private static int GetStableInt(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return BitConverter.ToInt32(hash, 0);
    }

    private sealed record WeightedProphecyCacheReward(int Weight, ProphecyRewardSnapshot Reward);
}
