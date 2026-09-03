using Domain.Models.Items.Equipments.Progression;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL;
using Application.UseCases.Quests.Dtos;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using AutoMapper;
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
    IGameRealtimeBroadcaster? eventPublisher = null,
    IStateSyncService? stateSync = null,
    IMapper? mapper = null,
    IQuestSystemChatPublisher? systemChatPublisher = null,
    IEquipmentQuestSupport? equipmentProgressionEquipment = null,
    IQuestEquipmentRewardRepository? currencyRewards = null,
    Application.Interfaces.Services.LL.Items.IStarterEquipmentService? starterClaims = null) : IQuestService, IQuestProgressionService
{
    public async Task<QuestJournal> GetJournalAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        var completedQuestIds = new List<string>();
        var loot = new List<InventoryItem>();
        var changed = await ReconcileEssenceProgressAsync(
            characterId, progresses, completedQuestIds, loot, cancellationToken);
        changed |= EnsurePinnedQuest(progresses);
        var journal = await MapJournalAsync(progresses, cancellationToken);
        if (changed)
        {
            await PublishChangedAsync(characterId, journal, cancellationToken);
            // Journal reads also repair progress, outside the command/outbox scope invalidation pipeline.
            if (completedQuestIds.Count > 0 && stateSync is not null)
            {
                var scopes = new List<string>
                {
                    StateSyncScopes.Character, StateSyncScopes.AreaAccess, StateSyncScopes.EquipmentForge
                };
                if (loot.Count > 0) scopes.Add(StateSyncScopes.Inventory);
                await stateSync.InvalidateCharacterScopesAsync(
                    characterId, scopes, nameof(GetJournalAsync), cancellationToken);
            }
            await repository.SaveChangesAsync(cancellationToken);
            await PublishCompletionsAsync(characterId, journal, completedQuestIds, cancellationToken);
        }

        return journal;
    }

    public async Task<QuestJournal> SelectChoiceAsync(
        Guid characterId,
        string questId,
        string optionKey,
        CancellationToken cancellationToken)
    {
        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        await EnsureAvailabilityAsync(characterId, progresses, cancellationToken);
        var progress = progresses.FirstOrDefault(x =>
            x.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (progress is null || progress.Status != QuestStatus.Active)
        {
            throw new InvalidOperationException("The quest choice is not active.");
        }

        var definition = definitions.Get(progress.QuestId, progress.DefinitionVersion);
        var choice = definition.Choice
            ?? throw new InvalidOperationException("This quest does not have a choice.");
        var option = choice.Options.FirstOrDefault(x =>
            x.Key.Equals(optionKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("The selected quest option does not exist.");

        if (!string.IsNullOrWhiteSpace(progress.SelectedOptionKey))
        {
            if (!progress.SelectedOptionKey.Equals(option.Key, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("This quest choice has already been confirmed.");
            }

            return await MapJournalAsync(progresses, cancellationToken);
        }

        var automaticObjectiveKeys = definition.Objectives
            .Where(objective => objective.Type == "CharacterLevelReached")
            .Select(objective => objective.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (progress.Objectives.Any(x =>
                !automaticObjectiveKeys.Contains(x.ObjectiveKey) &&
                (x.CurrentAmount > 0 || x.CompletedAt.HasValue)))
        {
            throw new InvalidOperationException("A quest choice cannot be changed after progress has started.");
        }

        var now = timeProvider.GetUtcNow();
        progress.SelectedOptionKey = option.Key;
        progress.UpdatedAt = now;
        progress.RowVersion++;
        await repository.SaveChangesAsync(cancellationToken);
        var journal = await MapJournalAsync(progresses, cancellationToken);
        await PublishChangedAsync(characterId, journal, cancellationToken);
        return journal;
    }

    public async Task<QuestJournal> AcknowledgeWelcomeAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        await EnsureAvailabilityAsync(characterId, progresses, cancellationToken);
        var progress = progresses.FirstOrDefault(x =>
            x.QuestId.Equals(QuestConstants.TrainingDay, StringComparison.OrdinalIgnoreCase) &&
            x.Status == QuestStatus.Active);
        if (progress is null)
        {
            throw new InvalidOperationException("The tutorial welcome is not available.");
        }

        if (!progress.AcceptedAt.HasValue)
        {
            var now = timeProvider.GetUtcNow();
            progress.AcceptedAt = now;
            progress.UpdatedAt = now;
            progress.RowVersion++;
            await repository.SaveChangesAsync(cancellationToken);
        }

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
            return new QuestProgressionResult(current, [], [], false);
        }

        var progresses = (await repository.GetProgressesAsync(characterId, cancellationToken)).ToList();
        var completedQuestIds = new List<string>();
        var loot = new List<InventoryItem>();
        var journalChanged = await ReconcileEssenceProgressAsync(
            characterId, progresses, completedQuestIds, loot, cancellationToken);
        var now = timeProvider.GetUtcNow();

        foreach (var progress in progresses.Where(x => x.Status == QuestStatus.Active).ToList())
        {
            var definition = definitions.Get(progress.QuestId, progress.DefinitionVersion);
            if (definition.Choice is not null &&
                string.IsNullOrWhiteSpace(progress.SelectedOptionKey))
            {
                continue;
            }

            var candidates = GetCandidateObjectives(progress, definition);
            foreach (var (objectiveProgress, objective) in candidates)
            {
                var amount = await EvaluateAsync(characterId, trigger, objective, cancellationToken);
                if (amount <= 0) continue;

                journalChanged = true;

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
                await CompleteQuestAsync(progress, definition, completedQuestIds, loot, cancellationToken);
                journalChanged = true;
            }
        }

        journalChanged |= await ReconcileEssenceProgressAsync(
            characterId, progresses, completedQuestIds, loot, cancellationToken);
        journalChanged |= EnsurePinnedQuest(progresses);

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
        if (journalChanged)
        {
            await PublishChangedAsync(characterId, journal, cancellationToken);
        }
        await PublishCompletionsAsync(characterId, journal, completedQuestIds, cancellationToken);
        return new QuestProgressionResult(journal, completedQuestIds, loot, journalChanged);
    }

    private async Task PublishCompletionsAsync(
        Guid characterId,
        QuestJournal journal,
        IReadOnlyCollection<string> completedQuestIds,
        CancellationToken cancellationToken)
    {
        if (completedQuestIds.Count > 0 && systemChatPublisher is not null)
        {
            var completedQuests = journal.Quests
                .Where(quest => completedQuestIds.Contains(quest.QuestId, StringComparer.OrdinalIgnoreCase))
                .Select(quest => new QuestCompletionChatMessage(quest.QuestId, quest.Title))
                .ToList();
            await systemChatPublisher.PublishAsync(
                characterId,
                completedQuests,
                cancellationToken);
        }
    }

    private async Task CompleteQuestAsync(
        CharacterQuestProgress progress,
        QuestDefinition definition,
        List<string> completedQuestIds,
        List<InventoryItem> loot,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        progress.Status = QuestStatus.Completed;
        progress.CompletedAt ??= now;
        progress.IsPinned = false;
        progress.UpdatedAt = now;
        progress.RowVersion++;
        loot.AddRange(await GrantRewardsAsync(progress, definition, cancellationToken));
        completedQuestIds.Add(progress.QuestId);
    }

    private async Task<bool> ReconcileEssenceProgressAsync(
        Guid characterId,
        List<CharacterQuestProgress> progresses,
        List<string> completedQuestIds,
        List<InventoryItem> loot,
        CancellationToken cancellationToken)
    {
        var changed = false;
        IReadOnlySet<string>? ownedEssences = null;
        bool completedQuest;
        do
        {
            changed |= await EnsureAvailabilityAsync(characterId, progresses, cancellationToken);
            completedQuest = false;
            foreach (var progress in progresses.Where(x => x.Status == QuestStatus.Active).ToList())
            {
                var definition = definitions.Get(progress.QuestId, progress.DefinitionVersion);
                if (definition.Choice is not null && string.IsNullOrWhiteSpace(progress.SelectedOptionKey)) continue;
                if (!definition.Objectives.Any(x => x.Type is "EssenceAbsorbed" or "EssenceOwned" or "EssenceEquipped")) continue;

                var now = timeProvider.GetUtcNow();
                var progressChanged = false;
                // Repair the old one-event requirement on active A Second Soul rows without resetting them.
                foreach (var objective in definition.Objectives.Where(x => x.Type == "EssenceOwned"))
                {
                    var saved = progress.Objectives.Single(x => x.ObjectiveKey == objective.Key);
                    if (saved.RequiredAmount == objective.RequiredAmount) continue;
                    saved.RequiredAmount = objective.RequiredAmount;
                    saved.CurrentAmount = Math.Min(saved.CurrentAmount, saved.RequiredAmount);
                    saved.CompletedAt = saved.CurrentAmount >= saved.RequiredAmount ? saved.CompletedAt ?? now : null;
                    saved.UpdatedAt = now;
                    progressChanged = true;
                }

                bool advanced;
                do
                {
                    advanced = false;
                    foreach (var (saved, objective) in GetCandidateObjectives(progress, definition))
                    {
                        if (objective.Type is not ("EssenceAbsorbed" or "EssenceOwned" or "EssenceEquipped")) continue;
                        var expected = await ResolveExpectedEssenceDefinitionIdAsync(
                            characterId, objective.Filters, cancellationToken);
                        long amount;
                        if (objective.Type == "EssenceEquipped")
                        {
                            amount = string.IsNullOrWhiteSpace(expected)
                                ? await repository.HasAnyEssenceInLoadoutAsync(characterId, cancellationToken) ? 1 : 0
                                : await repository.HasEssenceInAnyLoadoutAsync(characterId, expected, cancellationToken) ? 1 : 0;
                        }
                        else
                        {
                            // Only a specific, one-time absorption can be proven from current ownership.
                            // Unfiltered absorption events keep their authored event-counting behavior.
                            if (objective.Type == "EssenceAbsorbed" &&
                                (string.IsNullOrWhiteSpace(expected) || objective.RequiredAmount != 1)) continue;
                            ownedEssences ??= await repository.GetOwnedEssenceDefinitionIdsAsync(characterId, cancellationToken);
                            amount = ownedEssences.Count(id => Matches(expected, id));
                        }

                        var target = Math.Min(saved.RequiredAmount, amount);
                        if (target <= saved.CurrentAmount) continue;
                        saved.CurrentAmount = target;
                        saved.UpdatedAt = now;
                        if (target >= saved.RequiredAmount)
                        {
                            saved.CompletedAt ??= now;
                            advanced = true;
                        }
                        progressChanged = true;
                    }
                } while (advanced && definition.ObjectiveMode == "Sequential");

                if (progressChanged)
                {
                    progress.UpdatedAt = now;
                    progress.RowVersion++;
                    changed = true;
                }
                if (progress.Objectives.All(x => x.CompletedAt.HasValue))
                {
                    await CompleteQuestAsync(progress, definition, completedQuestIds, loot, cancellationToken);
                    changed = completedQuest = true;
                }
            }
        } while (completedQuest);

        return changed;
    }

    private async Task<bool> EnsureAvailabilityAsync(
        Guid characterId,
        List<CharacterQuestProgress> progresses,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var changed = false;
        var level = await repository.GetCharacterLevelAsync(characterId, cancellationToken)
            ?? throw new InvalidOperationException("Character was not found.");
        changed |= ApplyCharacterLevelProgress(progresses, level, now);

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
                Status = QuestStatus.Active,
                AcceptedAt = !definition.Id.Equals(
                                 QuestConstants.TrainingDay,
                                 StringComparison.OrdinalIgnoreCase)
                    ? now
                    : null,
                CreatedAt = now,
                UpdatedAt = now,
                Objectives = definition.Objectives.Select(objective =>
                {
                    var currentAmount = objective.Type == "CharacterLevelReached"
                        ? Math.Min(objective.RequiredAmount, level)
                        : 0;
                    return new CharacterQuestObjectiveProgress
                    {
                        CharacterId = characterId,
                        QuestId = definition.Id,
                        ObjectiveKey = objective.Key,
                        CurrentAmount = currentAmount,
                        RequiredAmount = objective.RequiredAmount,
                        CompletedAt = currentAmount >= objective.RequiredAmount ? now : null,
                        UpdatedAt = now
                    };
                }).ToList()
            };
            progresses.Add(progress);
            repository.AddProgress(progress);
            changed = true;
        }


        return changed;
    }

    private bool ApplyCharacterLevelProgress(
        IReadOnlyCollection<CharacterQuestProgress> progresses,
        int characterLevel,
        DateTimeOffset now)
    {
        var changed = false;
        foreach (var progress in progresses)
        {
            var definition = definitions.Get(progress.QuestId, progress.DefinitionVersion);
            var progressChanged = false;
            foreach (var objective in definition.Objectives.Where(x =>
                         x.Type == "CharacterLevelReached"))
            {
                var objectiveProgress = progress.Objectives.FirstOrDefault(x =>
                    x.ObjectiveKey.Equals(objective.Key, StringComparison.OrdinalIgnoreCase));
                if (objectiveProgress is null)
                {
                    objectiveProgress = new CharacterQuestObjectiveProgress
                    {
                        CharacterId = progress.CharacterId,
                        QuestId = progress.QuestId,
                        ObjectiveKey = objective.Key,
                        RequiredAmount = objective.RequiredAmount,
                        UpdatedAt = now
                    };
                    progress.Objectives.Add(objectiveProgress);
                    progressChanged = true;
                }

                var targetAmount = progress.Status == QuestStatus.Completed
                    ? objective.RequiredAmount
                    : Math.Min(objective.RequiredAmount, characterLevel);
                if (objectiveProgress.RequiredAmount != objective.RequiredAmount)
                {
                    objectiveProgress.RequiredAmount = objective.RequiredAmount;
                    progressChanged = true;
                }

                if (objectiveProgress.CurrentAmount < targetAmount)
                {
                    objectiveProgress.CurrentAmount = targetAmount;
                    progressChanged = true;
                }

                if (objectiveProgress.CurrentAmount >= objectiveProgress.RequiredAmount &&
                    !objectiveProgress.CompletedAt.HasValue)
                {
                    objectiveProgress.CompletedAt = now;
                    progressChanged = true;
                }

                if (progressChanged)
                {
                    objectiveProgress.UpdatedAt = now;
                }
            }

            if (!progressChanged) continue;
            progress.UpdatedAt = now;
            progress.RowVersion++;
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
        var expectedEssenceDefinitionId = await ResolveExpectedEssenceDefinitionIdAsync(
            characterId,
            filters,
            cancellationToken);
        return objective.Type switch
        {
            EquipmentKeys.StarterLoadoutObjective or EquipmentKeys.PlainTargetObjective =>
                await (equipmentProgressionEquipment ?? throw new InvalidOperationException("Equipment quest support is required."))
                    .IsEquippedAsync(characterId, objective.Type, filters.StarterEquipmentKind, cancellationToken) ? 1 : 0,
            "CombatEncounterCompleted" when
                trigger.Type == "CombatEncounterCompleted" &&
                Matches(filters.AreaId, trigger.AreaId) &&
                (filters.RequiresVictory != true || trigger.WonEncounter == true) => 1,

            "AreaActionCompletedWithTool" when
                trigger.Type == "CombatEncounterCompleted" &&
                Matches(filters.AreaId, trigger.AreaId) &&
                Matches(filters.GatheringType, trigger.EquippedGatheringType) =>
                Math.Max(0, trigger.ActionCount),

            "EssenceAbsorbed" when
                trigger.Type == "EssenceAbsorbed" &&
                Matches(expectedEssenceDefinitionId, trigger.EssenceDefinitionId) => 1,

            "EssenceEquipped" when
                trigger.Type == "EssenceLoadoutChanged" =>
                string.IsNullOrWhiteSpace(expectedEssenceDefinitionId)
                    ? await repository.HasAnyEssenceInLoadoutAsync(characterId, cancellationToken) ? 1 : 0
                    : await repository.HasEssenceInAnyLoadoutAsync(
                        characterId,
                        expectedEssenceDefinitionId,
                        cancellationToken) ? 1 : 0,

            "EssenceFocusSet" when trigger.Type == "EssenceFocusSet" => 1,

            "FocusedCreatureEssenceReceived" when
                trigger.Type == "FocusedCreatureEssenceReceived" => 1,

            "EssenceAscended" when trigger.Type == "EssenceAscended" => 1,

            "CompatibleEssenceLoadout" when
                trigger.Type == "EssenceLoadoutChanged" &&
                trigger.HasCompatibleEssenceTrio => 1,

            "EquipmentCrafted" when trigger.Type == "EquipmentCrafted" =>
                CountMatchingCraftedItems(trigger, filters),

            "EquipmentTempered" when trigger.Type == "EquipmentTempered" =>
                CountMatchingCraftedItems(trigger, filters),

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

            "ColosseumBattleStarted" when trigger.Type == "ColosseumBattleStarted" => 1,

            "TournamentBattleCompleted" when trigger.Type == "TournamentBattleCompleted" => 1,

            "DungeonRunStarted" when trigger.Type == "DungeonRunStarted" => 1,

            "DungeonRunCompleted" when
                trigger.Type == "DungeonRunCompleted" &&
                Matches(filters.DungeonDefinitionId, trigger.DungeonDefinitionId) => 1,

            "DailyProphecyCompleted" when trigger.Type == "DailyProphecyCompleted" => 1,

            _ => 0
        };
    }

    private static long CountMatchingCraftedItems(
        QuestTrigger trigger,
        QuestObjectiveFilterDefinition filters)
    {
        var itemBaseIds = trigger.CraftedItemBaseIds?.ToList() ?? [];
        var tiers = trigger.CraftedItemTiers?.ToList() ?? [];
        var baseRecipeIds = trigger.CraftedBaseRecipeIds?.ToList() ?? [];
        var qualities = trigger.CraftedItemQualities?.ToList() ?? [];
        var potentials = trigger.CraftedItemPotentials?.ToList() ?? [];
        var itemCount = Math.Min(itemBaseIds.Count, tiers.Count);

        return Enumerable.Range(0, itemCount).LongCount(index =>
            (filters.ItemBaseIds.Count == 0 ||
             filters.ItemBaseIds.Contains(itemBaseIds[index], StringComparer.OrdinalIgnoreCase)) &&
            (filters.BaseRecipeIds.Count == 0 ||
             index < baseRecipeIds.Count &&
             baseRecipeIds[index] is not null &&
             filters.BaseRecipeIds.Contains(baseRecipeIds[index]!, StringComparer.OrdinalIgnoreCase)) &&
            (!filters.MustBeCrafted ||
             index < baseRecipeIds.Count &&
             !string.IsNullOrWhiteSpace(baseRecipeIds[index])) &&
            (!filters.Tier.HasValue || tiers[index] == filters.Tier.Value) &&
            (string.IsNullOrWhiteSpace(filters.Quality) ||
             index < qualities.Count &&
             filters.Quality.Equals(qualities[index].ToString(), StringComparison.OrdinalIgnoreCase)) &&
            (!filters.RequiresNoPotential ||
             index < potentials.Count &&
             potentials[index] is <= 0));
    }

    private async Task<string?> ResolveExpectedEssenceDefinitionIdAsync(
        Guid characterId,
        QuestObjectiveFilterDefinition filters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filters.EssenceDefinitionFromChoiceQuestId))
        {
            return filters.EssenceDefinitionId;
        }

        var choiceProgress = await repository.GetProgressAsync(
            characterId,
            filters.EssenceDefinitionFromChoiceQuestId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Choice quest progress '{filters.EssenceDefinitionFromChoiceQuestId}' was not found.");
        var choiceDefinition = definitions.Get(
            choiceProgress.QuestId,
            choiceProgress.DefinitionVersion);
        var option = GetSelectedOption(choiceProgress, choiceDefinition)
            ?? throw new InvalidOperationException(
                $"Choice quest '{choiceProgress.QuestId}' has no valid selected option.");
        return option.EssenceDefinitionId;
    }

    private async Task<IReadOnlyList<InventoryItem>> GrantRewardsAsync(
        CharacterQuestProgress progress,
        QuestDefinition definition,
        CancellationToken cancellationToken)
    {
        var rewards = GetResolvedRewards(progress, definition);
        if (progress.RewardsGrantedAt.HasValue || rewards.Count == 0)
        {
            progress.RewardsGrantedAt ??= timeProvider.GetUtcNow();
            return [];
        }

        var quantities = rewards
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
            await lootRewardWriter.AddLootAsync(
                progress.CharacterId,
                items,
                "quest-reward",
                location: null,
                cancellationToken);
        }

        var cinders = rewards.Where(x => x.Type == "Cinders").Sum(x => (long)x.Quantity);
        if (cinders > 0)
            await (currencyRewards ?? throw new InvalidOperationException("Quest currency rewards are required."))
                .AwardCindersAsync(progress.CharacterId, definition.Id, cinders, cancellationToken);
        if (definition.Id == QuestConstants.FirstWeapon)
        {
            // Award accessories with the completed quest inside the outbox transaction.
            var grant = await (starterClaims ?? throw new InvalidOperationException("Starter equipment grants are required."))
                .ClaimAsync(progress.CharacterId, Domain.Models.Items.Equipments.Progression.StarterEquipmentGrantKind.ReadyForRoad, [], cancellationToken);
            if (grant.Error != null) throw new InvalidOperationException(grant.Error);
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
        var selected = pinned ?? active.FirstOrDefault(progress =>
            definitions
                .Get(progress.QuestId, progress.DefinitionVersion)
                .Category.Equals("Tutorial", StringComparison.OrdinalIgnoreCase));
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

    private async Task<QuestJournal> MapJournalAsync(
        IReadOnlyList<CharacterQuestProgress> progresses,
        CancellationToken cancellationToken)
    {
        var itemBaseIds = progresses
            .SelectMany(progress =>
            {
                var definition = definitions.Get(progress.QuestId, progress.DefinitionVersion);
                return GetResolvedRewards(progress, definition)
                    .Select(reward => reward.ItemBaseId)
                    .Concat(definition.Choice?.Options.Select(x => x.RewardItemBaseId) ?? []);
            })
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
                var selectedOption = GetSelectedOption(progress, definition);
                var rewards = GetResolvedRewards(progress, definition);
                return new QuestState(
                    progress.QuestId,
                    progress.DefinitionVersion,
                    definition.Choice?.ReplaceQuestIdentity == true
                        ? selectedOption?.Title ?? definition.Choice.SelectionTitle
                        : definition.Title,
                    definition.Choice?.ReplaceQuestIdentity == true
                        ? selectedOption?.Summary ?? definition.Choice.SelectionSummary
                        : definition.Summary,
                    definition.Category,
                    definition.ObjectiveMode,
                    definition.Chain is null
                        ? null
                        : new QuestChain(
                            definition.Chain.Id,
                            definition.Chain.Title,
                            definition.Chain.Description,
                            definition.Chain.Goal,
                            definition.Chain.PromisedReward,
                            definition.Chain.Step,
                            definition.Chain.TotalSteps),
                    definition.Choice is null
                        ? null
                        : new QuestChoice(
                            definition.Choice.SelectionTitle,
                            definition.Choice.SelectionSummary,
                            definition.Choice.ConfirmationText,
                            progress.SelectedOptionKey,
                            definition.Choice.Options.Select(option =>
                                new QuestChoiceOption(
                                    option.Key,
                                    option.Title,
                                    option.Summary,
                                    option.CreatureId,
                                    option.CreatureName,
                                    option.EssenceDefinitionId,
                                    option.RewardItemBaseId,
                                    option.EncounterKey,
                                    rewardItemBases.GetValueOrDefault(option.RewardItemBaseId))).ToList()),
                    definition.SortOrder,
                    progress.Status,
                    progress.IsPinned,
                    progress.QuestId.Equals(
                        QuestConstants.TrainingDay,
                        StringComparison.OrdinalIgnoreCase) &&
                    progress.Status == QuestStatus.Active &&
                    !progress.AcceptedAt.HasValue,
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
                    rewards.Select(reward =>
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

    private static QuestChoiceOptionDefinition? GetSelectedOption(
        CharacterQuestProgress progress,
        QuestDefinition definition) =>
        string.IsNullOrWhiteSpace(progress.SelectedOptionKey)
            ? null
            : definition.Choice?.Options.FirstOrDefault(option =>
                option.Key.Equals(progress.SelectedOptionKey, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<QuestRewardDefinition> GetResolvedRewards(
        CharacterQuestProgress progress,
        QuestDefinition definition)
    {
        var selectedOption = GetSelectedOption(progress, definition);
        if (selectedOption is null) return definition.Rewards;

        return definition.Rewards
            .Append(new QuestRewardDefinition
            {
                Key = $"choice_{selectedOption.Key}_essence",
                Type = "Item",
                ItemBaseId = selectedOption.RewardItemBaseId,
                Quantity = 1
            })
            .ToList();
    }

    private async Task PublishChangedAsync(
        Guid characterId,
        QuestJournal journal,
        CancellationToken cancellationToken)
    {
        if (eventPublisher is null) return;
        if (mapper is null)
        {
            throw new InvalidOperationException(
                "Quest realtime publishing requires the application mapper.");
        }

        var stateVersion = 0L;
        if (stateSync is not null)
        {
            await stateSync.AdvanceCharacterScopeAsync(
                characterId,
                StateSyncScopes.Quests,
                nameof(QuestJournalChanged),
                cancellationToken);
            stateVersion = stateSync
                .GetChangedRevisions(characterId)
                .GetValueOrDefault(StateSyncScopes.Quests);
        }

        await eventPublisher.PublishAsync(
            new Audience.Character(characterId),
            new QuestJournalChanged(mapper.Map<QuestJournalDto>(journal), stateVersion),
            nameof(QuestService),
            cancellationToken);
    }

    private static bool Matches(string? expected, string? actual) =>
        string.IsNullOrWhiteSpace(expected) ||
        expected.Equals(actual, StringComparison.OrdinalIgnoreCase);
}
