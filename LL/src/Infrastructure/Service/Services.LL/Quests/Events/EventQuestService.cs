using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Domain.Models.Items;
using Domain.Models.Quests;
using Domain.Models.Quests.Events;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Quests.Events;

public sealed class EventQuestService(
    IEventQuestRepository repository,
    IQuestRepository questRepository,
    IEventQuestDefinitionProvider definitions,
    IItemBaseRepository itemBases,
    IInventoryItemFactory inventoryItemFactory,
    ILootRewardWriter lootRewardWriter,
    TimeProvider timeProvider,
    IGameEventPublisher eventPublisher) : IEventQuestService, IEventQuestProgressionService
{
    public async Task<EventQuestJournal> GetJournalAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        if (!await HasCompletedTutorialAsync(characterId, cancellationToken))
        {
            return new EventQuestJournal([]);
        }

        await EnsureInstancesAsync(cancellationToken);
        var instances = await repository.GetAllAsync(characterId, cancellationToken);
        var changed = RefreshStatuses(instances, timeProvider.GetUtcNow());
        if (changed) await repository.SaveChangesAsync(cancellationToken);
        return await MapJournalAsync(characterId, instances, cancellationToken);
    }

    public async Task ProcessAsync(
        Guid characterId,
        QuestTrigger trigger,
        Guid outboxMessageId,
        string eventType,
        CancellationToken cancellationToken)
    {
        if (!await HasCompletedTutorialAsync(characterId, cancellationToken)) return;

        await EnsureInstancesAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var instances = await repository.GetAllAsync(characterId, cancellationToken);
        var changed = RefreshStatuses(instances, now);
        var globallyChangedEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var personallyChangedEventIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enabledIds = definitions.GetAll()
            .Where(x => x.Enabled)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var instance in instances.Where(x =>
                     enabledIds.Contains(x.EventQuestId) &&
                     now >= x.StartsAtUtc &&
                     now <= x.EndsAtUtc))
        {
            var definition = definitions.Get(instance.EventQuestId, instance.DefinitionVersion);
            var contribution = instance.Contributions.SingleOrDefault();
            foreach (var objectiveDefinition in definition.Objectives)
            {
                var objective = instance.Objectives.Single(x => x.ObjectiveKey == objectiveDefinition.Key);
                if (await repository.HasProcessedAsync(
                        instance.EventQuestId,
                        objective.ObjectiveKey,
                        outboxMessageId,
                        cancellationToken))
                {
                    continue;
                }

                var amount = Evaluate(trigger, objectiveDefinition);
                if (amount <= 0) continue;

                var remaining = Math.Max(0, objective.RequiredAmount - objective.CurrentAmount);
                var applied = Math.Min(amount, remaining);
                if (applied > 0)
                {
                    objective.CurrentAmount += applied;
                    objective.UpdatedAt = now;
                    if (objective.CurrentAmount >= objective.RequiredAmount) objective.CompletedAt = now;
                    globallyChangedEventIds.Add(instance.EventQuestId);
                }

                contribution ??= new EventQuestCharacterContribution
                {
                    EventQuestId = instance.EventQuestId,
                    CharacterId = characterId,
                    EventQuest = instance
                };
                if (!instance.Contributions.Contains(contribution)) instance.Contributions.Add(contribution);
                contribution.TotalAmount += amount;
                contribution.LastContributedAt = now;

                repository.AddLedger(new EventQuestEventLedger
                {
                    Id = Guid.NewGuid(),
                    EventQuestId = instance.EventQuestId,
                    ObjectiveKey = objective.ObjectiveKey,
                    OutboxMessageId = outboxMessageId,
                    CharacterId = characterId,
                    EventType = eventType,
                    ContributionAmount = amount,
                    ProcessedAt = now
                });
                instance.UpdatedAt = now;
                instance.RowVersion++;
                changed = true;
                personallyChangedEventIds.Add(instance.EventQuestId);
            }

            if (instance.Objectives.All(x => x.CompletedAt.HasValue) &&
                instance.Status != EventQuestStatus.Completed)
            {
                instance.Status = EventQuestStatus.Completed;
                instance.CompletedAt ??= now;
                instance.UpdatedAt = now;
                globallyChangedEventIds.Add(instance.EventQuestId);
            }
        }

        if (!changed) return;
        await repository.SaveChangesAsync(cancellationToken);
        foreach (var eventQuestId in globallyChangedEventIds)
        {
            await eventPublisher.PublishAsync(
                new Audience.World(),
                new EventQuestChangedMsg(eventQuestId, now));
        }
        foreach (var eventQuestId in personallyChangedEventIds)
        {
            await eventPublisher.PublishAsync(
                new Audience.Character(characterId),
                new EventQuestChangedMsg(eventQuestId, now));
        }
    }

    public async Task<EventQuestJournal> ClaimAsync(
        Guid characterId,
        string eventQuestId,
        CancellationToken cancellationToken)
    {
        await EnsureTutorialCompletedAsync(characterId, cancellationToken);
        await EnsureInstancesAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var instance = await repository.GetAsync(eventQuestId, characterId, cancellationToken)
            ?? throw new InvalidOperationException("The event quest was not found.");
        RefreshStatus(instance, now);
        var definition = definitions.Get(instance.EventQuestId, instance.DefinitionVersion);
        var contribution = instance.Contributions.SingleOrDefault()?.TotalAmount ?? 0;

        if (instance.Status != EventQuestStatus.Completed || now > instance.ClaimEndsAtUtc)
            throw new InvalidOperationException("This event's rewards are not claimable.");
        if (contribution < definition.MinimumContribution)
            throw new InvalidOperationException(
                $"Contribute at least {definition.MinimumContribution} before claiming this reward.");
        if (instance.RewardClaims.Count > 0)
            throw new InvalidOperationException("This event reward has already been claimed.");

        var loot = await CreateRewardLootAsync(
            characterId,
            definition.Rewards,
            cancellationToken);
        await AddCurrencyRewardsAsync(characterId, definition.Rewards, cancellationToken);

        repository.AddClaim(new EventQuestRewardClaim
        {
            EventQuestId = instance.EventQuestId,
            CharacterId = characterId,
            ClaimedAt = now,
            EventQuest = instance
        });
        await lootRewardWriter.AddLootAsync(
            characterId,
            loot,
            "event-quest-reward",
            location: null,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return await GetJournalAsync(characterId, cancellationToken);
    }

    public Task<EventQuestJournal> ClaimMilestoneAsync(
        Guid characterId,
        string eventQuestId,
        string milestoneKey,
        CancellationToken cancellationToken) =>
        ClaimMilestonesAsync(
            characterId,
            eventQuestId,
            milestoneKey,
            claimAll: false,
            cancellationToken);

    public Task<EventQuestJournal> ClaimAllMilestonesAsync(
        Guid characterId,
        string eventQuestId,
        CancellationToken cancellationToken) =>
        ClaimMilestonesAsync(
            characterId,
            eventQuestId,
            milestoneKey: null,
            claimAll: true,
            cancellationToken);

    private async Task<EventQuestJournal> ClaimMilestonesAsync(
        Guid characterId,
        string eventQuestId,
        string? milestoneKey,
        bool claimAll,
        CancellationToken cancellationToken)
    {
        await EnsureTutorialCompletedAsync(characterId, cancellationToken);
        await EnsureInstancesAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var instance = await repository.GetAsync(eventQuestId, characterId, cancellationToken)
            ?? throw new InvalidOperationException("The event quest was not found.");
        var definition = definitions.Get(instance.EventQuestId, instance.DefinitionVersion);
        if (!definition.Enabled || now < instance.StartsAtUtc || now > instance.ClaimEndsAtUtc)
        {
            throw new InvalidOperationException("This event's personal milestone rewards are not claimable.");
        }

        var contribution = instance.Contributions.SingleOrDefault()?.TotalAmount ?? 0;
        var claimedKeys = instance.MilestoneClaims
            .Select(x => x.MilestoneKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = definition.PersonalMilestones
            .Where(milestone =>
                (claimAll || milestone.Key.Equals(milestoneKey, StringComparison.OrdinalIgnoreCase)) &&
                contribution >= milestone.RequiredContribution &&
                !claimedKeys.Contains(milestone.Key))
            .ToList();

        if (!claimAll && !definition.PersonalMilestones.Any(x =>
                x.Key.Equals(milestoneKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The personal milestone was not found.");
        }
        if (selected.Count == 0)
        {
            throw new InvalidOperationException(
                claimAll
                    ? "There are no unlocked personal milestone rewards to claim."
                    : "This personal milestone is locked or has already been claimed.");
        }

        foreach (var milestone in selected)
        {
            repository.AddMilestoneClaim(new EventQuestMilestoneClaim
            {
                EventQuestId = instance.EventQuestId,
                CharacterId = characterId,
                MilestoneKey = milestone.Key,
                ClaimedAt = now,
                EventQuest = instance
            });
        }

        var loot = await CreateRewardLootAsync(
            characterId,
            selected.SelectMany(x => x.Rewards).ToList(),
            cancellationToken);
        await AddCurrencyRewardsAsync(
            characterId,
            selected.SelectMany(x => x.Rewards).ToList(),
            cancellationToken);
        await lootRewardWriter.AddLootAsync(
            characterId,
            loot,
            "event-quest-reward",
            location: null,
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return await GetJournalAsync(characterId, cancellationToken);
    }

    private async Task<IReadOnlyList<Domain.Models.Inventories.InventoryItem>> CreateRewardLootAsync(
        Guid characterId,
        IReadOnlyCollection<QuestRewardDefinition> rewards,
        CancellationToken cancellationToken)
    {
        var rewardsByItem = rewards
            .Where(x => x.Type == "Item")
            .GroupBy(x => x.ItemBaseId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Sum(reward => reward.Quantity), StringComparer.OrdinalIgnoreCase);
        var bases = await itemBases.GetItemBasesByIdsAsync(rewardsByItem.Keys.ToArray(), cancellationToken);
        return rewardsByItem.SelectMany(pair =>
        {
            if (!bases.TryGetValue(pair.Key, out var itemBase))
                throw new InvalidOperationException($"Event reward item '{pair.Key}' does not exist.");
            return inventoryItemFactory.CreateForQuantity(itemBase, pair.Value, characterId);
        }).ToList();
    }

    private Task AddCurrencyRewardsAsync(
        Guid characterId,
        IReadOnlyCollection<QuestRewardDefinition> rewards,
        CancellationToken cancellationToken)
    {
        var sigilFragments = rewards
            .Where(reward => reward.Type == "SigilFragments")
            .Sum(reward => reward.Quantity);
        return sigilFragments > 0
            ? repository.AddSigilFragmentsAsync(characterId, sigilFragments, cancellationToken)
            : Task.CompletedTask;
    }

    private async Task EnsureInstancesAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var existing = await repository.GetAllAsync(Guid.Empty, cancellationToken);
        var byId = existing.ToDictionary(x => x.EventQuestId, StringComparer.OrdinalIgnoreCase);
        var changed = false;
        foreach (var definition in definitions.GetAll().Where(x => x.Enabled))
        {
            if (byId.TryGetValue(definition.Id, out var instance))
            {
                if (instance.Status == EventQuestStatus.Upcoming &&
                    instance.Objectives.All(x => x.CurrentAmount == 0) &&
                    instance.DefinitionVersion != definition.Version)
                {
                    var currentKeys = instance.Objectives.Select(x => x.ObjectiveKey)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var definitionKeys = definition.Objectives.Select(x => x.Key)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (!currentKeys.SetEquals(definitionKeys))
                    {
                        throw new InvalidOperationException(
                            $"Upcoming event quest '{definition.Id}' cannot change objective keys after it has been materialized.");
                    }

                    instance.DefinitionVersion = definition.Version;
                    instance.StartsAtUtc = definition.StartsAtUtc;
                    instance.EndsAtUtc = definition.EndsAtUtc;
                    instance.ClaimEndsAtUtc = definition.ClaimEndsAtUtc;
                    foreach (var objective in instance.Objectives)
                    {
                        objective.RequiredAmount = definition.Objectives
                            .Single(x => x.Key.Equals(objective.ObjectiveKey, StringComparison.OrdinalIgnoreCase))
                            .RequiredAmount;
                        objective.UpdatedAt = now;
                    }
                    instance.UpdatedAt = now;
                    instance.RowVersion++;
                    changed = true;
                }
                continue;
            }

            repository.Add(new EventQuestInstance
            {
                EventQuestId = definition.Id,
                DefinitionVersion = definition.Version,
                Status = now < definition.StartsAtUtc ? EventQuestStatus.Upcoming : EventQuestStatus.Active,
                StartsAtUtc = definition.StartsAtUtc,
                EndsAtUtc = definition.EndsAtUtc,
                ClaimEndsAtUtc = definition.ClaimEndsAtUtc,
                CreatedAt = now,
                UpdatedAt = now,
                Objectives = definition.Objectives.Select(objective => new EventQuestObjectiveProgress
                {
                    EventQuestId = definition.Id,
                    ObjectiveKey = objective.Key,
                    RequiredAmount = objective.RequiredAmount,
                    UpdatedAt = now
                }).ToList()
            });
            changed = true;
        }

        if (changed) await repository.SaveChangesAsync(cancellationToken);
    }

    private static bool RefreshStatuses(IEnumerable<EventQuestInstance> instances, DateTimeOffset now)
    {
        var changed = false;
        foreach (var instance in instances) changed |= RefreshStatus(instance, now);
        return changed;
    }

    private static bool RefreshStatus(EventQuestInstance instance, DateTimeOffset now)
    {
        var status = instance.Objectives.Count > 0 && instance.Objectives.All(x => x.CompletedAt.HasValue)
            ? now > instance.ClaimEndsAtUtc ? EventQuestStatus.Expired : EventQuestStatus.Completed
            : now < instance.StartsAtUtc ? EventQuestStatus.Upcoming
            : now <= instance.EndsAtUtc ? EventQuestStatus.Active
            : now > instance.ClaimEndsAtUtc ? EventQuestStatus.Expired : EventQuestStatus.Ended;
        if (instance.Status == status) return false;
        instance.Status = status;
        instance.UpdatedAt = now;
        instance.RowVersion++;
        return true;
    }

    private async Task<EventQuestJournal> MapJournalAsync(
        Guid characterId,
        IReadOnlyList<EventQuestInstance> instances,
        CancellationToken cancellationToken)
    {
        var visible = instances
            .Where(instance => definitions.GetAll().Any(definition =>
                definition.Enabled && definition.Id.Equals(instance.EventQuestId, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var rewardIds = visible
            .SelectMany(instance =>
            {
                var definition = definitions.Get(instance.EventQuestId, instance.DefinitionVersion);
                return definition.Rewards.Concat(
                    definition.PersonalMilestones.SelectMany(milestone => milestone.Rewards));
            })
            .Where(x => x.Type == "Item")
            .Select(x => x.ItemBaseId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var bases = await itemBases.GetItemBasesByIdsAsync(rewardIds, cancellationToken);
        EventQuestRewardState MapReward(QuestRewardDefinition reward) =>
            new(
                reward.Key,
                reward.Type,
                reward.ItemBaseId,
                reward.Quantity,
                reward.ItemBaseId is not null && bases.TryGetValue(reward.ItemBaseId, out var itemBase)
                    ? itemBase
                    : null);

        var states = new List<EventQuestState>();
        foreach (var instance in visible)
        {
            var definition = definitions.Get(instance.EventQuestId, instance.DefinitionVersion);
            var contribution = instance.Contributions.SingleOrDefault(x => x.CharacterId == characterId)?.TotalAmount ?? 0;
            var standing = await repository.GetContributionStandingAsync(
                instance.EventQuestId,
                characterId,
                3,
                cancellationToken);
            states.Add(new EventQuestState(
                instance.EventQuestId,
                instance.DefinitionVersion,
                definition.Title,
                definition.Summary,
                instance.Status,
                instance.StartsAtUtc,
                instance.EndsAtUtc,
                instance.ClaimEndsAtUtc,
                instance.CompletedAt,
                definition.MinimumContribution,
                contribution,
                contribution >= definition.MinimumContribution,
                instance.RewardClaims.Any(x => x.CharacterId == characterId),
                standing.CharacterRank,
                standing.ContributorCount,
                standing.ContributionToNextRank,
                definition.SortOrder,
                definition.Objectives.Select(objective =>
                {
                    var progress = instance.Objectives.Single(x => x.ObjectiveKey == objective.Key);
                    return new EventQuestObjectiveState(
                        objective.Key,
                        objective.Description,
                        objective.Type,
                        progress.CurrentAmount,
                        progress.RequiredAmount,
                        progress.CompletedAt.HasValue);
                }).ToList(),
                definition.Rewards.Select(MapReward).ToList(),
                definition.PersonalMilestones.Select(milestone =>
                    new EventQuestPersonalMilestoneState(
                        milestone.Key,
                        milestone.RequiredContribution,
                        contribution >= milestone.RequiredContribution,
                        instance.MilestoneClaims.Any(claim =>
                            claim.CharacterId == characterId &&
                            claim.MilestoneKey.Equals(milestone.Key, StringComparison.OrdinalIgnoreCase)),
                        milestone.Rewards.Select(MapReward).ToList())).ToList(),
                standing.TopContributors.Select(contributor =>
                    new EventQuestContributorState(
                        contributor.Rank,
                        contributor.CharacterId,
                        contributor.CharacterName,
                        contributor.Contribution)).ToList()));
        }

        return new EventQuestJournal(states
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartsAtUtc)
            .ToList());
    }

    private async Task EnsureTutorialCompletedAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        if (!await HasCompletedTutorialAsync(characterId, cancellationToken))
        {
            throw new InvalidOperationException(
                "Complete the tutorial before participating in server-wide events.");
        }
    }

    private async Task<bool> HasCompletedTutorialAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var progress = await questRepository.GetProgressAsync(
            characterId,
            QuestConstants.IntoLumoRuins,
            cancellationToken);
        return progress?.Status == QuestStatus.Completed;
    }

    private static long Evaluate(QuestTrigger trigger, QuestObjectiveDefinition objective)
    {
        var filters = objective.Filters;
        return objective.Type switch
        {
            "CombatEncounterCompleted" when trigger.Type == "CombatEncounterCompleted" &&
                Matches(filters.AreaId, trigger.AreaId) => CountCombatEncounters(trigger, filters.RequiresVictory),
            "AreaActionCompletedWithTool" when trigger.Type == "CombatEncounterCompleted" &&
                Matches(filters.AreaId, trigger.AreaId) &&
                Matches(filters.GatheringType, trigger.EquippedGatheringType) => Math.Max(0, trigger.ActionCount),
            "EssenceAbsorbed" when trigger.Type == "EssenceAbsorbed" &&
                Matches(filters.EssenceDefinitionId, trigger.EssenceDefinitionId) => 1,
            "EssenceFocusSet" when trigger.Type == "EssenceFocusSet" => 1,
            "FocusedCreatureEssenceReceived" when trigger.Type == "FocusedCreatureEssenceReceived" => 1,
            "EssenceAscended" when trigger.Type == "EssenceAscended" => 1,
            "CompatibleEssenceLoadout" when trigger.Type == "EssenceLoadoutChanged" && trigger.HasCompatibleEssenceTrio => 1,
            "EquipmentCrafted" when trigger.Type == "EquipmentCrafted" => CountMatchingItems(trigger, filters),
            "EquipmentTempered" when trigger.Type == "EquipmentTempered" => CountMatchingItems(trigger, filters),
            "TemperingActionCompleted" when trigger.Type == "EquipmentTempered" =>
                Math.Max(0, trigger.ActionCount),
            "CharacterLevelReached" when trigger.Type == "CharacterLevelReached" &&
                trigger.CharacterLevel >= objective.RequiredAmount => objective.RequiredAmount,
            "ColosseumBattleStarted" when trigger.Type == "ColosseumBattleStarted" => 1,
            "TournamentBattleCompleted" when trigger.Type == "TournamentBattleCompleted" => 1,
            "DungeonRunStarted" when trigger.Type == "DungeonRunStarted" => 1,
            "DungeonRunCompleted" when trigger.Type == "DungeonRunCompleted" => 1,
            "DailyProphecyCompleted" when trigger.Type == "DailyProphecyCompleted" => 1,
            _ => 0
        };
    }

    private static long CountCombatEncounters(QuestTrigger trigger, bool? requiresVictory)
    {
        if (requiresVictory != true)
        {
            return Math.Max(0, trigger.ActionCount);
        }

        return Math.Max(
            0,
            trigger.WinningEncounterCount ?? (trigger.WonEncounter == true ? 1 : 0));
    }

    private static long CountMatchingItems(QuestTrigger trigger, QuestObjectiveFilterDefinition filters)
    {
        var ids = trigger.CraftedItemBaseIds?.ToList() ?? [];
        var tiers = trigger.CraftedItemTiers?.ToList() ?? [];
        var recipes = trigger.CraftedBaseRecipeIds?.ToList() ?? [];
        var qualities = trigger.CraftedItemQualities?.ToList() ?? [];
        var potentials = trigger.CraftedItemPotentials?.ToList() ?? [];
        return Enumerable.Range(0, Math.Min(ids.Count, tiers.Count)).LongCount(index =>
            (filters.ItemBaseIds.Count == 0 || filters.ItemBaseIds.Contains(ids[index], StringComparer.OrdinalIgnoreCase)) &&
            (filters.BaseRecipeIds.Count == 0 || index < recipes.Count && recipes[index] is not null &&
                filters.BaseRecipeIds.Contains(recipes[index]!, StringComparer.OrdinalIgnoreCase)) &&
            (!filters.MustBeCrafted || index < recipes.Count && !string.IsNullOrWhiteSpace(recipes[index])) &&
            (!filters.Tier.HasValue || tiers[index] == filters.Tier.Value) &&
            (string.IsNullOrWhiteSpace(filters.Quality) || index < qualities.Count &&
                filters.Quality.Equals(qualities[index].ToString(), StringComparison.OrdinalIgnoreCase)) &&
            (!filters.RequiresNoPotential || index < potentials.Count && potentials[index] is <= 0));
    }

    private static bool Matches(string? expected, string? actual) =>
        string.IsNullOrWhiteSpace(expected) || expected.Equals(actual, StringComparison.OrdinalIgnoreCase);
}
