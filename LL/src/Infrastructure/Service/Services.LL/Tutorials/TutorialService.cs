using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Tutorials;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Tutorials;
using Microsoft.EntityFrameworkCore;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Tutorials;

public sealed class TutorialService : ITutorialService, ITutorialProgressionService
{
    private readonly IDbContext _context;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly ILootRewardWriter _lootRewardWriter;
    private readonly IGameEventPublisher? _eventPublisher;
    private readonly ITutorialDefinitionProvider _definitionProvider;
    private readonly ITutorialProgressCache _progressCache;

    public TutorialService(
        IDbContext context,
        IItemBaseRepository itemBases,
        IInventoryRepository inventory,
        IInventoryItemFactory inventoryItemFactory,
        ILootRewardWriter lootRewardWriter,
        ITutorialDefinitionProvider definitionProvider,
        ITutorialProgressCache progressCache,
        IGameEventPublisher? eventPublisher = null)
    {
        _context = context;
        _itemBases = itemBases;
        _inventory = inventory;
        _inventoryItemFactory = inventoryItemFactory;
        _lootRewardWriter = lootRewardWriter;
        _definitionProvider = definitionProvider;
        _progressCache = progressCache;
        _eventPublisher = eventPublisher;
    }

    public async Task<TutorialState?> GetStateAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var progress = await GetOrCreateProgressAsync(characterId, true, cancellationToken);
        if (await SynchronizeCurrentStepAsync(progress, cancellationToken))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        CacheProgress(progress);
        return MapState(progress);
    }

    public async Task<TutorialProgressResult?> TryProgressAsync(
        Guid characterId,
        TutorialTrigger trigger,
        CancellationToken cancellationToken)
    {
        var cached = _progressCache.Get(characterId);
        if (cached is { IsActive: false })
        {
            return null;
        }

        var progress = await GetOrCreateProgressAsync(characterId, false, cancellationToken);
        if (progress.IsCompleted)
        {
            CacheProgress(progress);
            return null;
        }

        var step = _definitionProvider.GetStep(progress.TutorialId, progress.CurrentStep);
        if (step is null || !TriggerMatches(step, trigger))
        {
            CacheProgress(progress);
            return null;
        }

        var (progressed, loot) = await ApplyTriggerAsync(progress, step, trigger, cancellationToken);
        if (!progressed)
        {
            CacheProgress(progress);
            return null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await PublishTutorialStateChangedAsync(progress, cancellationToken);
        return new TutorialProgressResult(MapState(progress), loot, true);
    }

    public async Task<bool> CanStartCombatAreaAsync(Guid characterId, string areaId, CancellationToken cancellationToken)
    {
        if (IsCachedInactive(characterId))
        {
            return !areaId.Equals(TutorialConstants.TrainingGroundsAreaId, StringComparison.OrdinalIgnoreCase);
        }

        var progress = await GetOrCreateProgressAsync(characterId, true, cancellationToken);
        if (await CompleteLegacyLumoStepAsync(progress, cancellationToken))
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        CacheProgress(progress);

        if (areaId.Equals(TutorialConstants.TrainingGroundsAreaId, StringComparison.OrdinalIgnoreCase))
        {
            return progress.CurrentStep == TutorialConstants.StepDefeatTrainingCreature;
        }

        return progress.IsCompleted;
    }

    private async Task<bool> HasTutorialEssenceAsync(
        Guid characterId,
        IReadOnlyCollection<Guid> playerEssenceIds,
        string essenceDefinitionId,
        CancellationToken cancellationToken)
    {
        var ids = playerEssenceIds.ToArray();

        return await _context.PlayerEssences.AnyAsync(essence =>
            essence.CharacterId == characterId &&
            ids.Contains(essence.Id) &&
            essence.EssenceDefinitionId == essenceDefinitionId,
            cancellationToken);
    }

    private async Task<bool> HasTutorialEssenceInActiveLoadoutAsync(
        Guid characterId,
        string essenceDefinitionId,
        CancellationToken cancellationToken) =>
        await _context.EssenceLoadouts
            .Include(loadout => loadout.Slots)
            .ThenInclude(slot => slot.PlayerEssence)
            .Where(loadout => loadout.CharacterId == characterId && loadout.IsActive)
            .SelectMany(loadout => loadout.Slots)
            .AnyAsync(slot =>
                slot.PlayerEssence != null &&
                slot.PlayerEssence.EssenceDefinitionId == essenceDefinitionId,
                cancellationToken);

    private async Task<(bool Progressed, IReadOnlyList<InventoryItem> Loot)> ApplyTriggerAsync(
        CharacterTutorialProgress progress,
        TutorialStepDefinition step,
        TutorialTrigger trigger,
        CancellationToken cancellationToken)
    {
        var triggerType = step.Trigger!.Type;

        if (triggerType.Equals("IdleCombatCompleted", StringComparison.OrdinalIgnoreCase))
        {
            var loot = await GrantTrainingEssenceAsync(progress, cancellationToken);
            progress.TrainingCombatWonAt ??= DateTimeOffset.UtcNow;
            Advance(progress, step);
            return (true, loot);
        }

        if (triggerType.Equals("EssenceAbsorbed", StringComparison.OrdinalIgnoreCase))
        {
            progress.EssenceAbsorbedAt ??= DateTimeOffset.UtcNow;
            Advance(progress, step);
            return (true, []);
        }

        if (triggerType.Equals("EssenceLoadoutChanged", StringComparison.OrdinalIgnoreCase))
        {
            var essenceDefinitionId =
                step.Trigger.EssenceDefinitionId ?? TutorialConstants.TutorialEssenceDefinitionId;
            var hasTutorialEssenceAttuned = trigger.AttunedPlayerEssenceIds is { Count: > 0 }
                ? await HasTutorialEssenceAsync(
                    progress.CharacterId,
                    trigger.AttunedPlayerEssenceIds,
                    essenceDefinitionId,
                    cancellationToken)
                : await HasTutorialEssenceInActiveLoadoutAsync(
                    progress.CharacterId,
                    essenceDefinitionId,
                    cancellationToken);

            if (!hasTutorialEssenceAttuned)
            {
                return (false, []);
            }

            progress.EssenceEquippedAt ??= DateTimeOffset.UtcNow;
            Advance(progress, step);
            return (true, []);
        }

        if (triggerType.Equals("ClientRouteVisited", StringComparison.OrdinalIgnoreCase))
        {
            if (progress.CurrentStep == TutorialConstants.StepCraftEquipment)
            {
                await GrantTutorialEquipmentAsync(progress, cancellationToken);
                progress.CraftedTierOneEquipmentCount = TutorialConstants.RequiredCraftedEquipmentCount;
            }

            Advance(progress, step);
            return (true, []);
        }

        if (triggerType.Equals("CraftedEquipment", StringComparison.OrdinalIgnoreCase))
        {
            var craftedTierOneItems = trigger.CraftedItemTiers?.Count(tier => tier == 1) ?? 0;
            if (craftedTierOneItems <= 0)
            {
                return (false, []);
            }

            progress.CraftedTierOneEquipmentCount = Math.Min(
                TutorialConstants.RequiredCraftedEquipmentCount,
                progress.CraftedTierOneEquipmentCount + craftedTierOneItems);
            Touch(progress);

            if (progress.CraftedTierOneEquipmentCount >= TutorialConstants.RequiredCraftedEquipmentCount)
            {
                await GrantTutorialEquipmentAsync(progress, cancellationToken);
                Advance(progress, step);
            }

            return (true, []);
        }

        if (triggerType.Equals("EquipmentChanged", StringComparison.OrdinalIgnoreCase))
        {
            progress.EquippedTierOneEquipmentCount = await GetEquippedTutorialEquipmentCountAsync(
                progress.CharacterId,
                step.Trigger.ItemBaseIds,
                cancellationToken);
            Touch(progress);

            var requiredCount = step.Trigger.RequiredCount ?? TutorialConstants.RequiredEquippedEquipmentCount;
            if (progress.EquippedTierOneEquipmentCount < requiredCount)
            {
                return (true, []);
            }

            await GrantCompletionRewardAsync(progress, cancellationToken);
            Advance(progress, step);
            return (true, []);
        }

        return (false, []);
    }

    private static bool TriggerMatches(TutorialStepDefinition step, TutorialTrigger trigger)
    {
        var definitionTrigger = step.Trigger;
        if (definitionTrigger is null ||
            !definitionTrigger.Type.Equals(trigger.Type, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(trigger.StepKey) &&
            !step.Key.Equals(trigger.StepKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(definitionTrigger.AreaId) &&
            !definitionTrigger.AreaId.Equals(trigger.AreaId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (definitionTrigger.RequiresVictory == true && trigger.WonEncounter != true)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(definitionTrigger.EssenceDefinitionId) &&
            trigger.Type.Equals("EssenceAbsorbed", StringComparison.OrdinalIgnoreCase) &&
            !definitionTrigger.EssenceDefinitionId.Equals(trigger.EssenceDefinitionId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(definitionTrigger.Route) &&
            !RoutesMatch(definitionTrigger.Route, trigger.Route))
        {
            return false;
        }

        return true;
    }

    private static bool RoutesMatch(string expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var normalizedExpected = NormalizeRoute(expected);
        var normalizedActual = NormalizeRoute(actual);

        return normalizedActual.Equals(normalizedExpected, StringComparison.OrdinalIgnoreCase) ||
               normalizedActual.StartsWith($"{normalizedExpected}?", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRoute(string route) =>
        route.StartsWith('/') ? route : $"/{route}";

    private async Task<bool> CompleteLegacyLumoStepAsync(
        CharacterTutorialProgress progress,
        CancellationToken cancellationToken)
    {
        if (progress.CurrentStep != TutorialConstants.StepDefeatLumoRuins)
        {
            return false;
        }

        await GrantCompletionRewardAsync(progress, cancellationToken);
        Complete(progress);
        await PublishTutorialStateChangedAsync(progress, cancellationToken);
        return true;
    }

    private async Task<bool> SynchronizeCurrentStepAsync(
        CharacterTutorialProgress progress,
        CancellationToken cancellationToken)
    {
        if (progress.CurrentStep == TutorialConstants.StepEquipEssence
            && await HasTutorialEssenceInActiveLoadoutAsync(
                progress.CharacterId,
                _definitionProvider
                    .GetStep(progress.TutorialId, progress.CurrentStep)?
                    .Trigger?
                    .EssenceDefinitionId ?? TutorialConstants.TutorialEssenceDefinitionId,
                cancellationToken))
        {
            progress.EssenceEquippedAt ??= DateTimeOffset.UtcNow;
            SetStep(progress, TutorialConstants.StepCraftEquipment);
            await PublishTutorialStateChangedAsync(progress, cancellationToken);
            return true;
        }

        if (progress.CurrentStep == TutorialConstants.StepEquipEquipment)
        {
            var step = _definitionProvider.GetStep(progress.TutorialId, progress.CurrentStep);
            progress.EquippedTierOneEquipmentCount = await GetEquippedTutorialEquipmentCountAsync(
                progress.CharacterId,
                step?.Trigger?.ItemBaseIds ?? [],
                cancellationToken);

            var requiredCount =
                step?.Trigger?.RequiredCount ?? TutorialConstants.RequiredEquippedEquipmentCount;
            if (progress.EquippedTierOneEquipmentCount >= requiredCount)
            {
                await GrantCompletionRewardAsync(progress, cancellationToken);
                Complete(progress);
                await PublishTutorialStateChangedAsync(progress, cancellationToken);
                return true;
            }
        }

        return await CompleteLegacyLumoStepAsync(progress, cancellationToken);
    }

    private async Task<int> GetEquippedTutorialEquipmentCountAsync(
        Guid characterId,
        IReadOnlyCollection<string> tutorialItemBaseIds,
        CancellationToken cancellationToken)
    {
        if (tutorialItemBaseIds.Count == 0)
        {
            tutorialItemBaseIds =
            [
                TutorialConstants.TutorialChestItemBaseId
            ];
        }

        var equippedTutorialItemCount = await _context.EquipmentSlots
            .Include(slot => slot.EquipmentInstance)
            .Where(slot =>
                slot.EntityId == characterId &&
                slot.EquipmentInstance != null &&
                tutorialItemBaseIds.Contains(slot.EquipmentInstance.ItemBaseId))
            .CountAsync(cancellationToken);

        return equippedTutorialItemCount;
    }

    private async Task<CharacterTutorialProgress> GetOrCreateProgressAsync(
        Guid characterId,
        bool saveWhenCreated,
        CancellationToken cancellationToken)
    {
        var progress = await _context.CharacterTutorialProgresses
            .FirstOrDefaultAsync(x =>
                x.CharacterId == characterId &&
                x.TutorialId == TutorialConstants.FirstStepsTutorialId,
                cancellationToken);

        if (progress is not null)
        {
            return progress;
        }

        progress = new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = await ShouldAutoCompleteForExistingCharacterAsync(characterId, cancellationToken)
                ? TutorialConstants.StepComplete
                : TutorialConstants.StepDefeatTrainingCreature
        };

        if (progress.CurrentStep == TutorialConstants.StepComplete)
        {
            progress.CompletedAt = DateTimeOffset.UtcNow;
        }

        await _context.CharacterTutorialProgresses.AddAsync(progress, cancellationToken);

        if (saveWhenCreated)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return progress;
    }

    private async Task<bool> ShouldAutoCompleteForExistingCharacterAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _context.Characters
            .Where(x => x.Id == characterId)
            .Select(x => new { x.Level, x.Experience, x.Cinders })
            .FirstOrDefaultAsync(cancellationToken);

        return character is not null && (character.Level > 1 || character.Experience > 0 || character.Cinders > 0);
    }

    private async Task<IReadOnlyList<InventoryItem>> GrantTrainingEssenceAsync(
        CharacterTutorialProgress progress,
        CancellationToken cancellationToken)
    {
        if (progress.TrainingEssenceRewardGranted)
        {
            return [];
        }

        var loot = await AddItemRewardsAsync(progress.CharacterId, new Dictionary<string, int>
        {
            [TutorialConstants.TutorialEssenceItemBaseId] = 1
        }, cancellationToken, publishAsLoot: true);

        progress.TrainingEssenceRewardGranted = true;
        return loot;
    }

    private async Task GrantTutorialEquipmentAsync(CharacterTutorialProgress progress, CancellationToken cancellationToken)
    {
        await EnsureTutorialEquipmentItemBasesAsync(cancellationToken);
        await AddItemRewardsAsync(progress.CharacterId, new Dictionary<string, int>
        {
            [TutorialConstants.TutorialChestItemBaseId] = 1
        }, cancellationToken);
    }

    private async Task GrantCompletionRewardAsync(CharacterTutorialProgress progress, CancellationToken cancellationToken)
    {
        if (progress.CompletionRewardGranted)
        {
            return;
        }

        var character = await _context.Characters
            .FirstOrDefaultAsync(x => x.Id == progress.CharacterId, cancellationToken);
        if (character is not null)
        {
            character.Cinders += 150;
        }

        progress.CompletionRewardGranted = true;
    }

    private async Task<IReadOnlyList<InventoryItem>> AddItemRewardsAsync(
        Guid characterId,
        IReadOnlyDictionary<string, int> itemRewards,
        CancellationToken cancellationToken,
        bool publishAsLoot = false)
    {
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemRewards.Keys.ToArray(), cancellationToken);
        var items = new List<InventoryItem>();

        foreach (var (itemBaseId, quantity) in itemRewards)
        {
            if (!itemBases.TryGetValue(itemBaseId, out var itemBase))
            {
                throw new InvalidOperationException($"Tutorial reward item '{itemBaseId}' does not exist.");
            }

            items.AddRange(_inventoryItemFactory.CreateForQuantity(itemBase, quantity, characterId));
        }

        if (items.Count > 0)
        {
            if (publishAsLoot)
            {
                await _lootRewardWriter.AddLootAsync(characterId, items, cancellationToken);
                return items;
            }

            await _inventory.AddItemsToInventory(characterId, items, cancellationToken);
        }

        return items;
    }

    private TutorialState? MapState(CharacterTutorialProgress progress)
    {
        var definition = _definitionProvider.Get(progress.TutorialId);
        var step = _definitionProvider.GetStep(progress.TutorialId, progress.CurrentStep);

        if (progress.IsCompleted || step is null)
        {
            return null;
        }

        var current = progress.CurrentStep switch
        {
            TutorialConstants.StepCraftEquipment => progress.CraftedTierOneEquipmentCount >= TutorialConstants.RequiredCraftedEquipmentCount ? 1 : 0,
            TutorialConstants.StepEquipEquipment => progress.EquippedTierOneEquipmentCount,
            _ => 0
        };

        return new TutorialState(
            progress.TutorialId,
            definition.Title,
            definition.Version,
            progress.CurrentStep,
            step.Objective,
            current,
            step.RequiredAmount,
            new TutorialStepPresentation(
                step.ActionLabel,
                step.DestinationRoute,
                step.GuidePageId,
                step.TourPageId),
            step.ActionLabel,
            step.DestinationRoute,
            step.GuidePageId,
            step.TourPageId,
            progress.IsCompleted);
    }

    private async Task EnsureTutorialEquipmentItemBasesAsync(CancellationToken cancellationToken)
    {
        var tutorialItems = CreateTutorialEquipmentItemBases();
        await _itemBases.AddMissingItemBasesAsync(tutorialItems, cancellationToken);
    }

    private static IReadOnlyList<ItemBase> CreateTutorialEquipmentItemBases()
    {
        var chest = new EquipmentBase
        {
            Id = TutorialConstants.TutorialChestItemBaseId,
            Name = "Tutorial Chest",
            Description = "Basic protective gear for first steps beyond the training yard.",
            Rarity = Rarity.Common,
            EquipmentType = EquipmentType.Chest,
            ScalingAttribute = AttributeType.MaxHealth,
            ScalingAmount = 0,
            AttributeModifiers =
            [
                CreateModifier("10000000-0000-0000-0000-000000010001", AttributeType.Armor, 10),
                CreateModifier("10000000-0000-0000-0000-000000010002", AttributeType.Resistance, 10),
                CreateModifier("10000000-0000-0000-0000-000000010003", AttributeType.MaxHealth, 35)
            ]
        };

        foreach (var item in new[] { chest })
        {
            foreach (var modifier in item.AttributeModifiers)
            {
                modifier.ItemBaseId = item.Id;
            }
        }

        return [chest];
    }

    private static ItemAttributeModifier CreateModifier(string id, AttributeType attributeType, float amount) =>
        new(attributeType, amount, ModifierType.Flat)
        {
            Id = Guid.Parse(id)
        };

    private static void SetStep(CharacterTutorialProgress progress, string step)
    {
        progress.CurrentStep = step;
        Touch(progress);
    }

    private static void Complete(CharacterTutorialProgress progress)
    {
        progress.CurrentStep = TutorialConstants.StepComplete;
        progress.CompletedAt ??= DateTimeOffset.UtcNow;
        Touch(progress);
    }

    private static void Advance(CharacterTutorialProgress progress, TutorialStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.NextStepKey) ||
            step.NextStepKey.Equals(TutorialConstants.StepComplete, StringComparison.OrdinalIgnoreCase))
        {
            Complete(progress);
            return;
        }

        SetStep(progress, step.NextStepKey);
    }

    private static void Touch(CharacterTutorialProgress progress)
    {
        progress.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private async Task PublishTutorialStateChangedAsync(
        CharacterTutorialProgress progress,
        CancellationToken cancellationToken)
    {
        CacheProgress(progress);

        if (_eventPublisher is null)
        {
            return;
        }

        var audience = new Audience.Character(progress.CharacterId);
        if (progress.IsCompleted)
        {
            await _eventPublisher.PublishAsync(
                audience,
                new TutorialCompletedMsg(progress.TutorialId));
            return;
        }

        var state = MapState(progress);
        if (state is not null)
        {
            await _eventPublisher.PublishAsync(
                audience,
                new TutorialProgressedMsg(state));
        }
    }

    private bool IsCachedInactive(Guid characterId) =>
        _progressCache.Get(characterId) is { IsActive: false };

    private void CacheProgress(CharacterTutorialProgress progress)
    {
        if (progress.IsCompleted)
        {
            _progressCache.SetInactive(progress.CharacterId);
            return;
        }

        _progressCache.SetActive(progress.CharacterId, progress.TutorialId, progress.CurrentStep);
    }
}
