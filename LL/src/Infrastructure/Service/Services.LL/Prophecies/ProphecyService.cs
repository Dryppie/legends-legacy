using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Prophecies;
using Domain.Models.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Prophecies;
using Services.LL.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;

namespace Services.LL.Prophecies;

public sealed class ProphecyService : IProphecyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IReadOnlyList<ProphecyDefinition> _definitions;
    private readonly ProphecyBalanceCatalog _balance;
    private readonly IProphecyRewardResolver _rewardResolver;
    private readonly ICharacterExperienceProgressionProvider _experienceProgression;
    private readonly IProphecyRepository _repository;
    private readonly ICharacterService _characterService;
    private readonly IEntityService _entityService;
    private readonly ILevelingService _levelingService;
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IItemBaseRepository _itemBases;

    public ProphecyService(
        IProphecyDefinitionProvider definitionProvider,
        IProphecyBalanceProvider balanceProvider,
        IProphecyRewardResolver rewardResolver,
        ICharacterExperienceProgressionProvider experienceProgression,
        IProphecyRepository repository,
        ICharacterService characterService,
        IEntityService entityService,
        ILevelingService levelingService,
        IInventoryService inventoryService,
        IInventoryRepository inventoryRepository,
        IItemBaseRepository itemBases)
    {
        _definitions = definitionProvider.GetAll();
        _balance = balanceProvider.GetCatalog();
        _rewardResolver = rewardResolver;
        _experienceProgression = experienceProgression;
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
        var character = _characterService is null
            ? null
            : await GetOwnedCharacterAsync(playerId, characterId, cancellationToken);
        var definitions = await _repository.SyncDefinitionsAsync(_definitions, cancellationToken);

        var dailyPeriod = GetDailyPeriod(now);
        var weeklyPeriod = GetWeeklyPeriod(now);
        var rewardContext = CreateRewardContext(character?.Level ?? 1);

        var daily = await EnsureDailyInstancesAsync(definitions, playerId, characterId, dailyPeriod.Start, dailyPeriod.End, now, rewardContext, cancellationToken);
        var greater = await EnsureGreaterProphecyAsync(definitions, playerId, characterId, weeklyPeriod.Start, weeklyPeriod.End, now, rewardContext, cancellationToken);
        var weekly = await EnsureWeeklyProgressAsync(playerId, characterId, weeklyPeriod.Start, weeklyPeriod.End, now, cancellationToken);

        ExpireOldUnfinished(daily, now);
        if (greater.Status is ProphecyStatus.Offered)
        {
            greater.Status = ProphecyStatus.Accepted;
            greater.AcceptedAt = greater.GeneratedAt;
        }

        foreach (var prophecy in daily)
        {
            MarkCompletedIfTargetReached(prophecy, now);
        }

        MarkCompletedIfTargetReached(greater, now);

        var weeklyMilestones = CreateWeeklyMilestones(weekly);
        var caches = await GetCacheInventoryAsync(characterId, cancellationToken);
        var rerollState = await EnsureDailyRerollStateAsync(
            playerId,
            characterId,
            dailyPeriod.Start,
            dailyPeriod.End,
            daily,
            now,
            cancellationToken);
        var nextRerollCost = GetNextRerollCost(rerollState.RerollsUsed);

        return new PropheciesOverview(
            now,
            rerollState.RerollsUsed == 0 ? 1 : 0,
            rerollState.RerollsUsed,
            _balance.Economy.DailyRerollLimit,
            nextRerollCost,
            character?.FateEcho ?? 0,
            daily,
            daily.FirstOrDefault(IsAcceptedOrLater),
            greater,
            weekly,
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

    public async Task<ProphecyOperationResult<PropheciesOverview>> RerollAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var overview = await GetOverviewAsync(playerId, characterId, now, cancellationToken);
        if (overview.DailyProphecies.Count != 3)
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("The complete daily prophecy set is not available.");
        }

        if (overview.ActiveDailyProphecy is not null ||
            overview.DailyProphecies.Any(x => x.Status != ProphecyStatus.Offered))
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("Daily prophecies can only be rerolled before making the daily choice.");
        }

        if (overview.DailyRerollsUsed >= overview.DailyRerollLimit)
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("The daily prophecy reroll limit has been reached.");
        }

        var definitions = await _repository.SyncDefinitionsAsync(_definitions, cancellationToken);
        var character = await GetOwnedCharacterAsync(playerId, characterId, cancellationToken);
        if (character is null)
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("Character was not found.");
        }
        var rewardContext = CreateRewardContext(character.Level);
        var periodStart = overview.DailyProphecies[0].PeriodStart;
        var periodEnd = overview.DailyProphecies[0].PeriodEnd;
        var rerollState = await EnsureDailyRerollStateAsync(
            playerId,
            characterId,
            periodStart,
            periodEnd,
            overview.DailyProphecies,
            now,
            cancellationToken);
        var shownDefinitionIds = ReadShownDefinitionIds(rerollState.ShownDefinitionIdsJson);
        var currentDefinitionIds = overview.DailyProphecies
            .Select(x => x.ProphecyDefinitionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hardExcludedDefinitionIds = new HashSet<string>(currentDefinitionIds, StringComparer.OrdinalIgnoreCase);
        var excludedCategories = new HashSet<ProphecyCategory>();
        var replacements = new List<(PlayerProphecyInstance Prophecy, ProphecyDefinition Definition)>();

        foreach (var prophecy in overview.DailyProphecies.OrderBy(x => x.SlotType))
        {
            var eligibleDefinitions = definitions
                .Where(x => !hardExcludedDefinitionIds.Contains(x.Id))
                .ToList();
            var replacement = ProphecyOfferSelector.Pick(
                eligibleDefinitions,
                ProphecyScope.Daily,
                prophecy.SlotType,
                characterId,
                periodStart,
                $"reroll-set:{rerollState.RerollsUsed + 1}:{prophecy.SlotType}",
                shownDefinitionIds,
                excludedCategories,
                character.Level);

            if (replacement is null)
            {
                return ProphecyOperationResult<PropheciesOverview>.Fail(
                    "No complete alternative set of daily prophecies is available.");
            }

            replacements.Add((prophecy, replacement));
            hardExcludedDefinitionIds.Add(replacement.Id);
            excludedCategories.Add(replacement.Category);
        }

        if (replacements.Count != overview.DailyProphecies.Count)
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail(
                "No complete alternative set of daily prophecies is available.");
        }

        var rerollCost = GetNextRerollCost(rerollState.RerollsUsed);
        if (rerollCost is null)
        {
            return ProphecyOperationResult<PropheciesOverview>.Fail("The daily prophecy reroll limit has been reached.");
        }

        if (rerollCost == 0)
        {
            var consumed = await _repository.TryConsumeDailyRerollAsync(
                playerId,
                characterId,
                periodStart,
                now,
                cancellationToken);
            if (!consumed)
            {
                return ProphecyOperationResult<PropheciesOverview>.Fail("The free daily prophecy reroll has already been used.");
            }
        }
        else
        {
            if (!_balance.Economy.PaidRerollsEnabled)
            {
                return ProphecyOperationResult<PropheciesOverview>.Fail("Paid prophecy rerolls are currently disabled.");
            }

            if (character.FateEcho < rerollCost.Value)
            {
                return ProphecyOperationResult<PropheciesOverview>.Fail($"This reroll requires {rerollCost.Value} Fate Echo.");
            }

            var spent = await _repository.TrySpendFateEchoAsync(
                characterId,
                rerollCost.Value,
                cancellationToken);
            if (!spent)
            {
                return ProphecyOperationResult<PropheciesOverview>.Fail($"This reroll requires {rerollCost.Value} Fate Echo.");
            }

            character.FateEcho -= rerollCost.Value;
            _entityService.UpdateEntities([character]);
            rerollState.FateEchoSpent += rerollCost.Value;
        }

        var rerollAnchor = overview.DailyProphecies.First(x => x.SlotType == ProphecySlotType.Steady);
        rerollAnchor.DailyRerollUsedAt = now;
        rerollState.RerollsUsed++;
        rerollState.UpdatedAt = now;
        rerollState.RowVersion++;
        foreach (var (prophecy, replacement) in replacements)
        {
            shownDefinitionIds.Add(replacement.Id);
            ReplaceOfferDefinition(prophecy, replacement, now, rewardContext);
        }
        rerollState.ShownDefinitionIdsJson = JsonSerializer.Serialize(shownDefinitionIds, JsonOptions);

        var remainingFateEcho = overview.FateEcho - rerollCost.Value;
        var nextCost = GetNextRerollCost(rerollState.RerollsUsed);

        return ProphecyOperationResult<PropheciesOverview>.Success(
            overview with
            {
                DailyRerollsRemaining = 0,
                DailyRerollsUsed = rerollState.RerollsUsed,
                NextDailyRerollCost = nextCost,
                FateEcho = remainingFateEcho
            });
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

        MarkCompletedIfTargetReached(prophecy, now);

        if (prophecy.Status != ProphecyStatus.Completed)
        {
            return ProphecyOperationResult<ProphecyClaimResult>.Fail("Only completed prophecies can be claimed.");
        }

        var reward = ReadReward(prophecy.RewardSnapshotJson);
        reward.PropheticFavor = GetPropheticFavorReward(prophecy.Scope);
        var weeklyPeriod = reward.PropheticFavor > 0
            ? GetWeeklyPeriod(prophecy.PeriodStart)
            : GetWeeklyPeriod(now);
        var weekly = await EnsureWeeklyProgressAsync(playerId, characterId, weeklyPeriod.Start, weeklyPeriod.End, now, cancellationToken);

        await ApplyRewardAsync(characterId, reward, cancellationToken);

        if (reward.PropheticFavor > 0)
        {
            weekly.PropheticFavor = Math.Min(
                _balance.WeeklyMilestones.Max(x => x.FavorRequired),
                weekly.PropheticFavor + reward.PropheticFavor);
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

        var milestone = _balance.WeeklyMilestones.FirstOrDefault(x => x.FavorRequired == favorRequired);
        if (milestone is null)
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

        var reward = CloneReward(milestone.Reward);
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
        var cacheDefinition = _balance.Caches
            .FirstOrDefault(x => x.ItemId.Equals(cacheItemId, StringComparison.OrdinalIgnoreCase));

        if (cacheDefinition is null)
        {
            return ProphecyOperationResult<ProphecyCacheOpenResult>.Fail("Unknown prophecy cache.");
        }

        var cache = CreateCacheItemBase(cacheDefinition);

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

        var initialValues = new Dictionary<Guid, int>();
        var changedProphecies = new List<PlayerProphecyInstance>();
        foreach (var progressEvent in progressEvents.OrderBy(x => x.OccurredAt))
        {
            ApplyProgress(active, progressEvent, initialValues, changedProphecies);
        }

        return changedProphecies
            .Select(prophecy => new ProphecyProgressUpdate(
                characterIds[0],
                prophecy.Id,
                prophecy.ProphecyDefinition?.Title ?? prophecy.ProphecyDefinitionId,
                prophecy.Scope.ToString(),
                prophecy.SlotType.ToString(),
                prophecy.Status.ToString(),
                prophecy.CurrentValue,
                prophecy.TargetValue,
                Math.Max(0, prophecy.CurrentValue - initialValues[prophecy.Id]),
                prophecy.Status == ProphecyStatus.Completed))
            .ToList();
    }

    private void ApplyProgress(
        IEnumerable<PlayerProphecyInstance> active,
        ProphecyProgressEvent progressEvent,
        IDictionary<Guid, int> initialValues,
        ICollection<PlayerProphecyInstance> changedProphecies)
    {
        foreach (var prophecy in active.Where(x => IsActiveForProgress(x, progressEvent.OccurredAt)))
        {
            var previousValue = prophecy.CurrentValue;
            if (!TryApplyProgress(prophecy, progressEvent))
            {
                continue;
            }

            if (prophecy.CurrentValue >= prophecy.TargetValue)
            {
                prophecy.CurrentValue = prophecy.TargetValue;
                prophecy.Status = ProphecyStatus.Completed;
                prophecy.CompletedAt = progressEvent.OccurredAt;
            }

            if (initialValues.TryAdd(prophecy.Id, previousValue))
            {
                changedProphecies.Add(prophecy);
            }
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
        ProphecyRewardContext rewardContext,
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
        var excludedDefinitionIds = existing
            .Select(x => x.ProphecyDefinitionId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var excludedCategories = existing
            .Select(GetDefinition)
            .Where(x => x is not null)
            .Select(x => x!.Category)
            .ToHashSet();
        var generated = new List<PlayerProphecyInstance>();

        foreach (var slot in new[] { ProphecySlotType.Steady, ProphecySlotType.Focused, ProphecySlotType.Ominous })
        {
            if (existingSlots.Contains(slot))
            {
                continue;
            }

            var definition = ProphecyOfferSelector.Pick(
                definitions,
                ProphecyScope.Daily,
                slot,
                characterId,
                periodStart,
                "initial",
                excludedDefinitionIds,
                excludedCategories,
                rewardContext.CharacterLevel) ??
                throw new InvalidOperationException($"No enabled daily prophecy definition is available for slot {slot}.");

            generated.Add(CreateInstance(
                playerId,
                characterId,
                definition,
                ProphecyScope.Daily,
                slot,
                ProphecyStatus.Offered,
                periodStart,
                periodEnd,
                now,
                rewardContext));
            excludedDefinitionIds.Add(definition.Id);
            excludedCategories.Add(definition.Category);
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
        ProphecyRewardContext rewardContext,
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

        var definition = ProphecyOfferSelector.Pick(
            definitions,
            ProphecyScope.Weekly,
            ProphecySlotType.Greater,
            characterId,
            periodStart,
            "initial",
            characterLevel: rewardContext.CharacterLevel) ??
            throw new InvalidOperationException("No enabled weekly prophecy definition is available for the Greater slot.");

        greater = CreateInstance(
            playerId,
            characterId,
            definition,
            ProphecyScope.Weekly,
            ProphecySlotType.Greater,
            ProphecyStatus.Accepted,
            periodStart,
            periodEnd,
            now,
            rewardContext);
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

    private PlayerProphecyInstance CreateInstance(
        Guid playerId,
        Guid characterId,
        ProphecyDefinition definition,
        ProphecyScope scope,
        ProphecySlotType slot,
        ProphecyStatus status,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset now,
        ProphecyRewardContext rewardContext)
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
            RewardSnapshotJson = JsonSerializer.Serialize(_rewardResolver.Resolve(definition, rewardContext), JsonOptions)
        };
    }

    private void ReplaceOfferDefinition(
        PlayerProphecyInstance instance,
        ProphecyDefinition definition,
        DateTimeOffset now,
        ProphecyRewardContext rewardContext)
    {
        instance.ProphecyDefinitionId = definition.Id;
        instance.ProphecyDefinition = definition;
        instance.GeneratedAt = now;
        instance.TargetValue = GetTargetValue(definition);
        instance.CurrentValue = 0;
        instance.ObjectiveParameterSnapshotJson = definition.ObjectiveParameterJson;
        instance.ProgressJson = "{}";
        instance.RewardSnapshotJson = JsonSerializer.Serialize(_rewardResolver.Resolve(definition, rewardContext), JsonOptions);
    }

    private ProphecyDefinition? GetDefinition(PlayerProphecyInstance instance) =>
        instance.ProphecyDefinition ??
        _definitions.FirstOrDefault(x =>
            x.Id.Equals(instance.ProphecyDefinitionId, StringComparison.OrdinalIgnoreCase));

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
            case ProphecyObjectiveType.AbsorbEssence when progressEvent.Kind == ProphecyProgressKind.EssenceAbsorbed:
            case ProphecyObjectiveType.TemperItems when progressEvent.Kind == ProphecyProgressKind.ItemTempered:
            case ProphecyObjectiveType.TreasureProgress when progressEvent.Kind == ProphecyProgressKind.TreasureProgress:
                prophecy.CurrentValue += Math.Max(1, progressEvent.Amount);
                return true;

            case ProphecyObjectiveType.GatherResources when progressEvent.Kind == ProphecyProgressKind.ResourceGathered:
                if (!MeetsGatheringRequirements(prophecy.ObjectiveParameterSnapshotJson, progressEvent.Profession))
                {
                    return false;
                }

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
            var cacheDefinition = _balance.Caches.First(x =>
                x.ItemId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase));
            result.Add(new ProphecyCacheInventory(
                definition.Id,
                definition.Name,
                definition.Description,
                await _inventoryRepository.GetInventoryQuantityAsync(characterId, definition.Id, cancellationToken),
                cacheDefinition.PreviewRewards));
        }

        return result;
    }

    private static bool HasCharacterReward(ProphecyRewardSnapshot reward) =>
        reward.Cinders > 0 ||
        reward.Soulstones > 0 ||
        reward.CharacterExperience > 0 ||
        reward.SigilFragments > 0 ||
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

    private IReadOnlyList<ItemBase> CreateCacheItemBases() =>
        _balance.Caches.Select(CreateCacheItemBase).ToList();

    private async Task<Domain.Models.Entities.Characters.Character?> GetOwnedCharacterAsync(
        Guid playerId,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        if (_characterService is null)
        {
            return null;
        }

        var character = await _characterService.GetCharacterByCharacterIdAsync(characterId, cancellationToken);
        return character?.UserId == playerId ? character : null;
    }

    private async Task<DailyProphecyRerollState> EnsureDailyRerollStateAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        IReadOnlyList<PlayerProphecyInstance> daily,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var state = await _repository.GetDailyRerollStateAsync(
            playerId,
            characterId,
            periodStart,
            cancellationToken);
        if (state is not null)
        {
            return state;
        }

        state = new DailyProphecyRerollState
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CharacterId = characterId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RerollsUsed = daily.Any(x => x.DailyRerollUsedAt.HasValue) ? 1 : 0,
            ShownDefinitionIdsJson = JsonSerializer.Serialize(
                daily.Select(x => x.ProphecyDefinitionId).Distinct(StringComparer.OrdinalIgnoreCase),
                JsonOptions),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _repository.AddDailyRerollStateAsync(state, cancellationToken);
        return state;
    }

    private int? GetNextRerollCost(int rerollsUsed)
    {
        if (rerollsUsed >= _balance.Economy.DailyRerollLimit)
        {
            return null;
        }

        return rerollsUsed == 0
            ? 0
            : _balance.Economy.PaidRerollCosts[rerollsUsed - 1];
    }

    private static HashSet<string> ReadShownDefinitionIds(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static ItemBase CreateCacheItemBase(ProphecyCacheDefinition definition) =>
        new()
        {
            Id = definition.ItemId,
            Name = definition.Title,
            Description = definition.Description,
            Stackable = true,
            IsBound = true,
            ItemType = ItemType.Resource,
            Rarity = definition.Rarity
        };

    private static ProphecyRewardSnapshot ReadReward(string json) =>
        JsonSerializer.Deserialize<ProphecyRewardSnapshot>(json, JsonOptions) ?? new ProphecyRewardSnapshot();

    private static ProphecyProgressSnapshot ReadProgress(string json) =>
        JsonSerializer.Deserialize<ProphecyProgressSnapshot>(json, JsonOptions) ?? new ProphecyProgressSnapshot();

    private ProphecyRewardContext CreateRewardContext(int characterLevel)
    {
        var level = Math.Max(1, characterLevel);
        return new ProphecyRewardContext(level, _experienceProgression.GetRequiredExperience(level));
    }

    private IReadOnlyList<WeeklyRevelationMilestone> CreateWeeklyMilestones(WeeklyRevelationProgress progress) =>
        _balance.WeeklyMilestones
            .OrderBy(x => x.FavorRequired)
            .Select(x => CreateWeeklyMilestone(progress, x))
            .ToList();

    private int GetPropheticFavorReward(ProphecyScope scope) =>
        _balance.FavorRewards.First(x => x.Scope == scope).Amount;

    private static WeeklyRevelationMilestone CreateWeeklyMilestone(
        WeeklyRevelationProgress progress,
        ProphecyWeeklyMilestoneDefinition definition) =>
        new(
            definition.FavorRequired,
            definition.Title,
            progress.PropheticFavor >= definition.FavorRequired,
            IsMilestoneClaimed(progress, definition.FavorRequired),
            CloneReward(definition.Reward));

    private ProphecyRewardSnapshot CreateCacheOpenReward(string cacheItemId)
    {
        var cache = _balance.Caches.FirstOrDefault(x =>
            x.ItemId.Equals(cacheItemId, StringComparison.OrdinalIgnoreCase));
        if (cache is null || cache.Rewards.Count == 0)
        {
            return new ProphecyRewardSnapshot();
        }

        var reward = new ProphecyRewardSnapshot();
        for (var i = 0; i < cache.Rolls; i++)
        {
            AddReward(reward, RollCacheReward(cache.Rewards));
        }

        return reward;
    }

    private static ProphecyRewardSnapshot RollCacheReward(IReadOnlyList<ProphecyCacheRewardEntry> table)
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

    private int GetTargetValue(ProphecyDefinition definition)
    {
        var profile = _balance.Targets.First(x =>
            x.Scope == definition.Scope &&
            x.ObjectiveType.Equals(definition.ObjectiveType, StringComparison.Ordinal));
        return profile.GetValue(definition.Difficulty);
    }

    private static ProphecyRewardSnapshot CloneReward(ProphecyRewardSnapshot reward) =>
        new()
        {
            Cinders = reward.Cinders,
            CharacterExperience = reward.CharacterExperience,
            EssenceExperience = reward.EssenceExperience,
            Soulstones = reward.Soulstones,
            SigilFragments = reward.SigilFragments,
            PropheticFavor = reward.PropheticFavor,
            FateEcho = reward.FateEcho,
            CacheItemId = reward.CacheItemId,
            Items = reward.Items.Select(x => new RewardItemSnapshot
            {
                ItemId = x.ItemId,
                Quantity = x.Quantity
            }).ToList()
        };

    private static bool MeetsMinimumEnemyCount(string parameterJson, int? enemyCount)
    {
        if (enemyCount is null)
        {
            return true;
        }

        if (!ProphecyObjectiveParameters.TryParse(parameterJson, out var parameters))
        {
            return false;
        }

        return parameters.MinimumEnemyCount is null || enemyCount.Value >= parameters.MinimumEnemyCount.Value;
    }

    private static bool MeetsGatheringRequirements(string parameterJson, string? profession)
    {
        if (!ProphecyObjectiveParameters.TryParse(parameterJson, out var parameters))
        {
            return false;
        }

        var requiredProfession = parameters.RequiredProfession?.Trim();
        return string.IsNullOrWhiteSpace(requiredProfession) ||
            string.Equals(requiredProfession, profession?.Trim(), StringComparison.OrdinalIgnoreCase);
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

}
