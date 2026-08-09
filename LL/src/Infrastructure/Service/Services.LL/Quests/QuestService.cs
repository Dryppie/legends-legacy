using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Quests;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Quests;

public sealed class QuestService(
    IQuestRepository repository,
    IQuestDefinitionProvider definitions,
    IItemBaseRepository itemBases,
    IInventoryItemFactory inventoryItemFactory,
    ILootRewardWriter lootRewardWriter,
    TimeProvider timeProvider,
    IGameEventPublisher? eventPublisher = null) : IQuestService, IQuestProgressionService
{
    public async Task<QuestJournal> GetJournalAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        var changed = await EnsureAvailabilityAsync(characterId, progresses, cancellationToken);
        changed |= EnsurePinnedQuest(progresses);
        if (changed)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return await MapJournalAsync(progresses, cancellationToken);
    }

    public async Task<QuestJournal> AcceptAsync(
        Guid characterId,
        string questId,
        CancellationToken cancellationToken)
    {
        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        await EnsureAvailabilityAsync(characterId, progresses, cancellationToken);
        var progress = progresses.FirstOrDefault(x =>
            x.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (progress is null || progress.Status != QuestStatus.Available)
        {
            throw new InvalidOperationException("The quest is not available to accept.");
        }

        Activate(progress);
        EnsurePinnedQuest(progresses);
        await repository.SaveChangesAsync(cancellationToken);
        var journal = await MapJournalAsync(progresses, cancellationToken);
        await PublishChangedAsync(characterId, journal, cancellationToken);
        return journal;
    }

    public async Task<QuestJournal> PinAsync(
        Guid characterId,
        string? questId,
        CancellationToken cancellationToken)
    {
        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        CharacterQuestProgress? selected = null;
        if (!string.IsNullOrWhiteSpace(questId))
        {
            selected = progresses.FirstOrDefault(x =>
                x.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase) &&
                x.Status == QuestStatus.Active);
            if (selected is null)
            {
                throw new InvalidOperationException("Only an active quest can be pinned.");
            }
        }

        var now = timeProvider.GetUtcNow();
        foreach (var progress in progresses)
        {
            var shouldPin = progress == selected;
            if (progress.IsPinned == shouldPin) continue;
            progress.IsPinned = shouldPin;
            progress.UpdatedAt = now;
            progress.RowVersion++;
        }

        await repository.SaveChangesAsync(cancellationToken);
        var journal = await MapJournalAsync(progresses, cancellationToken);
        await PublishChangedAsync(characterId, journal, cancellationToken);
        return journal;
    }

    public async Task<QuestProgressionResult> ProcessAsync(
        Guid characterId,
        QuestTrigger trigger,
        Guid? outboxMessageId,
        string eventType,
        CancellationToken cancellationToken)
    {
        if (outboxMessageId.HasValue &&
            await repository.HasProcessedEventAsync(outboxMessageId.Value, cancellationToken))
        {
            var current = await GetJournalAsync(characterId, cancellationToken);
            await PublishChangedAsync(characterId, current, cancellationToken);
            return new QuestProgressionResult(current, [], []);
        }

        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        await EnsureAvailabilityAsync(characterId, progresses, cancellationToken);
        var completedQuestIds = new List<string>();
        var loot = new List<InventoryItem>();
        var now = timeProvider.GetUtcNow();

        foreach (var progress in progresses.Where(x => x.Status == QuestStatus.Active).ToList())
        {
            var definition = definitions.Get(progress.QuestId, progress.DefinitionVersion);
            var candidates = GetCandidateObjectives(progress, definition);
            foreach (var (objectiveProgress, objective) in candidates)
            {
                var amount = await EvaluateAsync(characterId, trigger, objective, cancellationToken);
                if (amount <= 0) continue;

                objectiveProgress.CurrentAmount = Math.Min(
                    objectiveProgress.RequiredAmount,
                    objectiveProgress.CurrentAmount + amount);
                objectiveProgress.UpdatedAt = now;
                if (objectiveProgress.CurrentAmount >= objectiveProgress.RequiredAmount)
                {
                    objectiveProgress.CompletedAt ??= now;
                }

                progress.UpdatedAt = now;
                progress.RowVersion++;
            }

            if (progress.Objectives.All(x => x.CompletedAt.HasValue))
            {
                progress.Status = QuestStatus.Completed;
                progress.CompletedAt ??= now;
                progress.IsPinned = false;
                progress.UpdatedAt = now;
                progress.RowVersion++;
                loot.AddRange(await GrantRewardsAsync(progress, definition, cancellationToken));
                completedQuestIds.Add(progress.QuestId);
            }
        }

        await EnsureAvailabilityAsync(characterId, progresses, cancellationToken);
        EnsurePinnedQuest(progresses);

        if (outboxMessageId.HasValue)
        {
            repository.AddEventLedger(new QuestEventLedger
            {
                Id = Guid.NewGuid(),
                OutboxMessageId = outboxMessageId.Value,
                CharacterId = characterId,
                EventType = eventType,
                ProcessedAt = now
            });
        }

        await repository.SaveChangesAsync(cancellationToken);
        var journal = await MapJournalAsync(progresses, cancellationToken);
        await PublishChangedAsync(characterId, journal, cancellationToken);
        return new QuestProgressionResult(journal, completedQuestIds, loot);
    }

    private async Task<bool> EnsureAvailabilityAsync(
        Guid characterId,
        List<CharacterQuestProgress> progresses,
        CancellationToken cancellationToken)
    {
        var level = await repository.GetCharacterLevelAsync(characterId, cancellationToken)
            ?? throw new InvalidOperationException("Character was not found.");
        var changed = false;
        var now = timeProvider.GetUtcNow();

        foreach (var definition in definitions.GetAll())
        {
            if (progresses.Any(x => x.QuestId.Equals(definition.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var completedIds = progresses
                .Where(x => x.Status == QuestStatus.Completed)
                .Select(x => x.QuestId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (level < definition.Availability.MinimumLevel ||
                definition.Availability.CompletedQuestIds.Any(x => !completedIds.Contains(x)))
            {
                continue;
            }

            var progress = new CharacterQuestProgress
            {
                CharacterId = characterId,
                QuestId = definition.Id,
                DefinitionVersion = definition.Version,
                Status = definition.AutoAccept ? QuestStatus.Active : QuestStatus.Available,
                AcceptedAt = definition.AutoAccept ? now : null,
                CreatedAt = now,
                UpdatedAt = now,
                Objectives = definition.Objectives.Select(objective =>
                    new CharacterQuestObjectiveProgress
                    {
                        CharacterId = characterId,
                        QuestId = definition.Id,
                        ObjectiveKey = objective.Key,
                        RequiredAmount = objective.RequiredAmount,
                        UpdatedAt = now
                    }).ToList()
            };
            progresses.Add(progress);
            repository.AddProgress(progress);
            changed = true;
        }

        return changed;
    }

    private static IReadOnlyList<(CharacterQuestObjectiveProgress Progress, QuestObjectiveDefinition Definition)>
        GetCandidateObjectives(CharacterQuestProgress progress, QuestDefinition definition)
    {
        var pairs = definition.Objectives
            .Select(objective => (
                Progress: progress.Objectives.Single(x => x.ObjectiveKey == objective.Key),
                Definition: objective))
            .Where(x => !x.Progress.CompletedAt.HasValue)
            .ToList();
        return definition.ObjectiveMode == "Sequential" ? pairs.Take(1).ToList() : pairs;
    }

    private async Task<long> EvaluateAsync(
        Guid characterId,
        QuestTrigger trigger,
        QuestObjectiveDefinition objective,
        CancellationToken cancellationToken)
    {
        var filters = objective.Filters;
        return objective.Type switch
        {
            "CombatEncounterCompleted" when
                trigger.Type == "CombatEncounterCompleted" &&
                Matches(filters.AreaId, trigger.AreaId) &&
                (filters.RequiresVictory != true || trigger.WonEncounter == true) => 1,

            "EssenceAbsorbed" when
                trigger.Type == "EssenceAbsorbed" &&
                Matches(filters.EssenceDefinitionId, trigger.EssenceDefinitionId) => 1,

            "EssenceEquipped" when trigger.Type == "EssenceLoadoutChanged" =>
                await repository.HasEssenceInActiveLoadoutAsync(
                    characterId,
                    filters.EssenceDefinitionId ?? string.Empty,
                    cancellationToken) ? 1 : 0,

            "EquipmentCrafted" when trigger.Type == "EquipmentCrafted" =>
                (trigger.CraftedItemBaseIds ?? [])
                    .Zip(trigger.CraftedItemTiers ?? [])
                    .LongCount(x =>
                        (filters.ItemBaseIds.Count == 0 || filters.ItemBaseIds.Contains(x.First)) &&
                        (!filters.Tier.HasValue || x.Second == filters.Tier.Value)),

            "EquipmentEquipped" when trigger.Type == "EquipmentChanged" =>
                await repository.HasQualifyingEquipmentEquippedAsync(
                    characterId,
                    filters.ItemBaseIds,
                    filters.Tier,
                    filters.MustBeCrafted,
                    false,
                    cancellationToken) ? 1 : 0,

            "GatheringToolEquipped" when trigger.Type == "EquipmentChanged" =>
                await repository.HasQualifyingEquipmentEquippedAsync(
                    characterId,
                    filters.ItemBaseIds,
                    filters.Tier,
                    filters.MustBeCrafted,
                    true,
                    cancellationToken) ? 1 : 0,

            "CharacterLevelReached" when
                trigger.Type == "CharacterLevelReached" &&
                trigger.CharacterLevel >= objective.RequiredAmount => objective.RequiredAmount,

            _ => 0
        };
    }

    private async Task<IReadOnlyList<InventoryItem>> GrantRewardsAsync(
        CharacterQuestProgress progress,
        QuestDefinition definition,
        CancellationToken cancellationToken)
    {
        if (progress.RewardsGrantedAt.HasValue || definition.Rewards.Count == 0)
        {
            progress.RewardsGrantedAt ??= timeProvider.GetUtcNow();
            return [];
        }

        var quantities = definition.Rewards
            .Where(x => x.Type == "Item")
            .GroupBy(x => x.ItemBaseId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Sum(reward => reward.Quantity), StringComparer.OrdinalIgnoreCase);
        var bases = await itemBases.GetItemBasesByIdsAsync(quantities.Keys.ToArray(), cancellationToken);
        var items = new List<InventoryItem>();
        foreach (var (itemBaseId, quantity) in quantities)
        {
            if (!bases.TryGetValue(itemBaseId, out var itemBase))
            {
                throw new InvalidOperationException(
                    $"Quest reward item '{itemBaseId}' does not exist for quest '{definition.Id}'.");
            }

            items.AddRange(inventoryItemFactory.CreateForQuantity(itemBase, quantity, progress.CharacterId));
        }

        if (items.Count > 0)
        {
            await lootRewardWriter.AddLootAsync(progress.CharacterId, items, cancellationToken);
        }

        progress.RewardsGrantedAt = timeProvider.GetUtcNow();
        return items;
    }

    private bool EnsurePinnedQuest(List<CharacterQuestProgress> progresses)
    {
        var active = progresses
            .Where(x => x.Status == QuestStatus.Active)
            .OrderBy(x => definitions.Get(x.QuestId, x.DefinitionVersion).SortOrder)
            .ToList();
        var pinned = active.FirstOrDefault(x => x.IsPinned);
        var selected = pinned ?? active.FirstOrDefault();
        var changed = false;
        var now = timeProvider.GetUtcNow();
        foreach (var progress in progresses)
        {
            var shouldPin = progress == selected;
            if (progress.IsPinned == shouldPin) continue;
            progress.IsPinned = shouldPin;
            progress.UpdatedAt = now;
            progress.RowVersion++;
            changed = true;
        }

        return changed;
    }

    private void Activate(CharacterQuestProgress progress)
    {
        var now = timeProvider.GetUtcNow();
        progress.Status = QuestStatus.Active;
        progress.AcceptedAt ??= now;
        progress.UpdatedAt = now;
        progress.RowVersion++;
    }

    private async Task<QuestJournal> MapJournalAsync(
        IReadOnlyList<CharacterQuestProgress> progresses,
        CancellationToken cancellationToken)
    {
        var itemBaseIds = progresses
            .SelectMany(progress => definitions
                .Get(progress.QuestId, progress.DefinitionVersion)
                .Rewards)
            .Select(reward => reward.ItemBaseId)
            .Where(itemBaseId => !string.IsNullOrWhiteSpace(itemBaseId))
            .Select(itemBaseId => itemBaseId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rewardItemBases = await itemBases.GetItemBasesByIdsAsync(
            itemBaseIds,
            cancellationToken);

        var states = progresses
            .Select(progress =>
            {
                var definition = definitions.Get(progress.QuestId, progress.DefinitionVersion);
                return new QuestState(
                    progress.QuestId,
                    progress.DefinitionVersion,
                    definition.Title,
                    definition.Summary,
                    definition.Category,
                    definition.SortOrder,
                    progress.Status,
                    progress.IsPinned,
                    progress.AcceptedAt,
                    progress.CompletedAt,
                    definition.Objectives.Select(objective =>
                    {
                        var objectiveProgress = progress.Objectives.Single(x => x.ObjectiveKey == objective.Key);
                        return new QuestObjectiveState(
                            objective.Key,
                            objective.Description,
                            objective.Type,
                            objectiveProgress.CurrentAmount,
                            objectiveProgress.RequiredAmount,
                            objectiveProgress.CompletedAt.HasValue,
                            new QuestPresentation(
                                objective.Presentation.ActionLabel,
                                objective.Presentation.DestinationRoute,
                                objective.Presentation.GuidePageId,
                                objective.Presentation.TourPageId));
                    }).ToList(),
                    definition.Rewards.Select(reward =>
                        new QuestRewardState(
                            reward.Key,
                            reward.Type,
                            reward.ItemBaseId,
                            reward.Quantity,
                            reward.ItemBaseId is not null &&
                            rewardItemBases.TryGetValue(reward.ItemBaseId, out var itemBase)
                                ? itemBase
                                : null)).ToList());
            })
            .OrderBy(x => x.SortOrder)
            .ToList();
        return new QuestJournal(states, states.FirstOrDefault(x => x.IsPinned)?.QuestId);
    }

    private Task PublishChangedAsync(
        Guid characterId,
        QuestJournal journal,
        CancellationToken cancellationToken) =>
        eventPublisher is null
            ? Task.CompletedTask
            : eventPublisher.PublishAsync(
                new Audience.Character(characterId),
                new QuestJournalChangedMsg(journal));

    private static bool Matches(string? expected, string? actual) =>
        string.IsNullOrWhiteSpace(expected) ||
        expected.Equals(actual, StringComparison.OrdinalIgnoreCase);
}
