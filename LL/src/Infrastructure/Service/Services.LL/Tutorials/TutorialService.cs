using Application.Common.Interfaces;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Tutorials;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Tutorials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Tutorials;

public sealed class TutorialService : ITutorialService, ITutorialProgressionService
{
    private readonly IDbContext _context;
    private readonly IItemBaseRepository _itemBases;
    private readonly IInventoryRepository _inventory;
    private readonly IEquipmentSlotService _equipmentSlots;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly ILootRewardWriter _lootRewardWriter;
    private readonly IGameEventPublisher? _eventPublisher;
    private readonly ITutorialDefinitionProvider _definitionProvider;
    private readonly ITutorialProgressCache _progressCache;
    private readonly TutorialDebugOptions _debugOptions;

    public TutorialService(
        IDbContext context,
        IItemBaseRepository itemBases,
        IInventoryRepository inventory,
        IEquipmentSlotService equipmentSlots,
        IInventoryItemFactory inventoryItemFactory,
        ILootRewardWriter lootRewardWriter,
        ITutorialDefinitionProvider definitionProvider,
        ITutorialProgressCache progressCache,
        IGameEventPublisher? eventPublisher = null,
        IOptions<TutorialDebugOptions>? debugOptions = null)
    {
        _context = context;
        _itemBases = itemBases;
        _inventory = inventory;
        _equipmentSlots = equipmentSlots;
        _inventoryItemFactory = inventoryItemFactory;
        _lootRewardWriter = lootRewardWriter;
        _definitionProvider = definitionProvider;
        _progressCache = progressCache;
        _eventPublisher = eventPublisher;
        _debugOptions = debugOptions?.Value ?? new TutorialDebugOptions();
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

    public async Task<TutorialState?> AcknowledgeWelcomeAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var progress = await GetOrCreateProgressAsync(characterId, false, cancellationToken);
        if (progress.IsCompleted)
        {
            CacheProgress(progress);
            return null;
        }

        if (!progress.WelcomeAcknowledgedAt.HasValue)
        {
            progress.WelcomeAcknowledgedAt = DateTimeOffset.UtcNow;
            Touch(progress);
            await _context.SaveChangesAsync(cancellationToken);
            await PublishTutorialStateChangedAsync(progress, cancellationToken);
        }

        CacheProgress(progress);
        return MapState(progress);
    }

    public async Task<TutorialState?> AttuneStarterEssenceAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var progress = await GetOrCreateProgressAsync(characterId, false, cancellationToken);
        if (progress.IsCompleted ||
            progress.CurrentStep != TutorialConstants.StepEquipEssence)
        {
            CacheProgress(progress);
            return MapState(progress);
        }

        var tutorialEssence = await EnsureTutorialEssenceAbsorbedAsync(
            characterId,
            cancellationToken);
        await EnsureTutorialEssenceEquippedAsync(
            characterId,
            tutorialEssence,
            cancellationToken);
        await EnsureTutorialCraftingMaterialsAsync(progress, cancellationToken);

        progress.EssenceEquippedAt ??= DateTimeOffset.UtcNow;
        SetStep(progress, TutorialConstants.StepCraftEquipment);

        await _context.SaveChangesAsync(cancellationToken);
        await PublishTutorialStateChangedAsync(progress, cancellationToken);
        return MapState(progress);
    }

    public async Task<TutorialCompletion> SkipAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var progress = await GetOrCreateProgressAsync(characterId, false, cancellationToken);
        if (!progress.IsCompleted)
        {
            var tutorialEssence = await EnsureTutorialEssenceAbsorbedAsync(
                characterId,
                cancellationToken);
            await EnsureTutorialEssenceEquippedAsync(
                characterId,
                tutorialEssence,
                cancellationToken);
            var starterWeapon = await EnsureTutorialStarterWeaponAsync(
                characterId,
                cancellationToken);
            await EnsureTutorialGatheringToolsAsync(characterId, cancellationToken);

            if (starterWeapon is not null)
            {
                // The equipment service reloads Inventory from the database, so
                // newly granted starter items must be persisted before equipping.
                await _context.SaveChangesAsync(cancellationToken);
                var equipped = await _equipmentSlots.EquipEquipmentAsync(
                    characterId,
                    starterWeapon.Id,
                    EquipmentSlotType.MainHand,
                    cancellationToken);
                if (!equipped)
                {
                    throw new InvalidOperationException(
                        "The tutorial starter Mace could not be equipped.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            progress.TrainingCombatWonAt ??= now;
            progress.EssenceAbsorbedAt ??= now;
            progress.EssenceEquippedAt ??= now;
            progress.TrainingEssenceRewardGranted = true;
            progress.CraftedTierOneEquipmentCount = TutorialConstants.RequiredCraftedEquipmentCount;
            progress.EquippedTierOneEquipmentCount = TutorialConstants.RequiredEquippedEquipmentCount;

            Complete(progress);
            await _context.SaveChangesAsync(cancellationToken);
            await PublishTutorialStateChangedAsync(progress, cancellationToken, wasSkipped: true);
        }

        CacheProgress(progress);
        return CreateCompletion(wasSkipped: true);
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

        if (areaId.Equals(TutorialConstants.LumoRuinsAreaId, StringComparison.OrdinalIgnoreCase))
        {
            return progress.IsCompleted ||
                   progress.CurrentStep == TutorialConstants.StepStartLumoRuins;
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
            await EnsureTutorialCraftingMaterialsAsync(progress, cancellationToken);
            Advance(progress, step);
            return (true, []);
        }

        if (triggerType.Equals("ClientRouteVisited", StringComparison.OrdinalIgnoreCase))
        {
            Advance(progress, step);
            return (true, []);
        }

        if (triggerType.Equals("CraftedEquipment", StringComparison.OrdinalIgnoreCase))
        {
            var allowedItemBaseIds = step.Trigger.ItemBaseIds.Count > 0
                ? step.Trigger.ItemBaseIds
                : TutorialConstants.TutorialOneHandedWeaponItemBaseIds;
            var craftedTierOneItems = (trigger.CraftedItemBaseIds ?? [])
                .Zip(trigger.CraftedItemTiers ?? [])
                .Count(item =>
                    item.Second == 1 &&
                    allowedItemBaseIds.Contains(item.First, StringComparer.OrdinalIgnoreCase));
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
                Advance(progress, step);
            }

            return (true, []);
        }

        if (triggerType.Equals("EquipmentChanged", StringComparison.OrdinalIgnoreCase))
        {
            var isGatheringToolStep = step.Key.Equals(
                TutorialConstants.StepEquipGatheringTool,
                StringComparison.OrdinalIgnoreCase);
            progress.EquippedTierOneEquipmentCount = isGatheringToolStep
                ? await GetEquippedTutorialGatheringToolCountAsync(
                    progress.CharacterId,
                    step.Trigger.ItemBaseIds,
                    cancellationToken)
                : await GetEquippedTutorialEquipmentCountAsync(
                    progress.CharacterId,
                    step.Trigger.ItemBaseIds,
                    cancellationToken);
            Touch(progress);

            var requiredCount = step.Trigger.RequiredCount ?? TutorialConstants.RequiredEquippedEquipmentCount;
            if (progress.EquippedTierOneEquipmentCount < requiredCount)
            {
                return (true, []);
            }

            IReadOnlyList<InventoryItem> loot = isGatheringToolStep
                ? []
                : await EnsureTutorialGatheringToolsAsync(progress.CharacterId, cancellationToken);
            if (!isGatheringToolStep)
            {
                progress.EquippedTierOneEquipmentCount = 0;
            }

            Advance(progress, step);
            return (true, loot);
        }

        if (triggerType.Equals("CombatActionStarted", StringComparison.OrdinalIgnoreCase))
        {
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
            await EnsureTutorialCraftingMaterialsAsync(progress, cancellationToken);
            SetStep(progress, TutorialConstants.StepCraftEquipment);
            await PublishTutorialStateChangedAsync(progress, cancellationToken);
            return true;
        }

        if (progress.CurrentStep == TutorialConstants.StepCraftEquipment)
        {
            if (await HasCraftedTutorialWeaponAsync(progress.CharacterId, cancellationToken))
            {
                progress.CraftedTierOneEquipmentCount =
                    TutorialConstants.RequiredCraftedEquipmentCount;
                SetStep(progress, TutorialConstants.StepEquipEquipment);
                await PublishTutorialStateChangedAsync(progress, cancellationToken);
                return true;
            }

            return await EnsureTutorialCraftingMaterialsAsync(progress, cancellationToken);
        }

        if (progress.CurrentStep == TutorialConstants.StepEquipEquipment)
        {
            if (!await HasCraftedTutorialWeaponAsync(progress.CharacterId, cancellationToken))
            {
                progress.CraftedTierOneEquipmentCount = 0;
                progress.EquippedTierOneEquipmentCount = 0;
                await EnsureTutorialCraftingMaterialsAsync(progress, cancellationToken);
                SetStep(progress, TutorialConstants.StepCraftEquipment);
                await PublishTutorialStateChangedAsync(progress, cancellationToken);
                return true;
            }

            var step = _definitionProvider.GetStep(progress.TutorialId, progress.CurrentStep);
            progress.EquippedTierOneEquipmentCount = await GetEquippedTutorialEquipmentCountAsync(
                progress.CharacterId,
                step?.Trigger?.ItemBaseIds ?? [],
                cancellationToken);

            var requiredCount =
                step?.Trigger?.RequiredCount ?? TutorialConstants.RequiredEquippedEquipmentCount;
            if (progress.EquippedTierOneEquipmentCount >= requiredCount)
            {
                await EnsureTutorialGatheringToolsAsync(progress.CharacterId, cancellationToken);
                progress.EquippedTierOneEquipmentCount = 0;
                SetStep(progress, TutorialConstants.StepEquipGatheringTool);
                await PublishTutorialStateChangedAsync(progress, cancellationToken);
                return true;
            }
        }

        if (progress.CurrentStep == TutorialConstants.StepEquipGatheringTool)
        {
            var toolsGranted = (await EnsureTutorialGatheringToolsAsync(
                progress.CharacterId,
                cancellationToken)).Count > 0;
            var step = _definitionProvider.GetStep(progress.TutorialId, progress.CurrentStep);
            progress.EquippedTierOneEquipmentCount = await GetEquippedTutorialGatheringToolCountAsync(
                progress.CharacterId,
                step?.Trigger?.ItemBaseIds ?? [],
                cancellationToken);

            var requiredCount =
                step?.Trigger?.RequiredCount ?? TutorialConstants.RequiredEquippedEquipmentCount;
            if (progress.EquippedTierOneEquipmentCount >= requiredCount)
            {
                SetStep(progress, TutorialConstants.StepStartLumoRuins);
                await PublishTutorialStateChangedAsync(progress, cancellationToken);
                return true;
            }

            return toolsGranted;
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
            tutorialItemBaseIds = TutorialConstants.TutorialOneHandedWeaponItemBaseIds;
        }

        var equippedTutorialItemCount = await _context.EquipmentSlots
            .Include(slot => slot.EquipmentInstance)
            .Where(slot =>
                slot.EntityId == characterId &&
                slot.EquipmentInstance != null &&
                slot.EquipmentInstance.Tier == 1 &&
                slot.EquipmentInstance.BaseRecipeId != null &&
                tutorialItemBaseIds.Contains(slot.EquipmentInstance.ItemBaseId))
            .CountAsync(cancellationToken);

        return equippedTutorialItemCount;
    }

    private async Task<int> GetEquippedTutorialGatheringToolCountAsync(
        Guid characterId,
        IReadOnlyCollection<string> tutorialItemBaseIds,
        CancellationToken cancellationToken)
    {
        if (tutorialItemBaseIds.Count == 0)
        {
            tutorialItemBaseIds = TutorialConstants.TutorialGatheringToolItemBaseIds;
        }

        return await _context.EquipmentSlots
            .Include(slot => slot.EquipmentInstance)
            .Where(slot =>
                slot.EntityId == characterId &&
                slot.EquipmentSlotType == Domain.Models.Items.Equipments.Slots.EquipmentSlotType.Tool &&
                slot.EquipmentInstance != null &&
                tutorialItemBaseIds.Contains(slot.EquipmentInstance.ItemBaseId))
            .CountAsync(cancellationToken);
    }

    private async Task<bool> HasCraftedTutorialWeaponAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var tutorialWeaponIds = TutorialConstants.TutorialOneHandedWeaponItemBaseIds;
        var craftedWeaponIds = _context.ItemInstances
            .OfType<EquipmentInstance>()
            .Where(item =>
                item.Tier == 1 &&
                item.BaseRecipeId != null &&
                tutorialWeaponIds.Contains(item.ItemBaseId))
            .Select(item => item.Id);

        return await _context.InventoryItems.AnyAsync(
                   item =>
                       item.InventoryId == characterId &&
                       craftedWeaponIds.Contains(item.ItemInstanceId),
                   cancellationToken) ||
               await _context.EquipmentSlots.AnyAsync(
                   slot =>
                       slot.EntityId == characterId &&
                       slot.EquipmentInstanceId != null &&
                       craftedWeaponIds.Contains(slot.EquipmentInstanceId.Value),
                   cancellationToken);
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
            if (ShouldAutoCompleteNewProgressForDebug() && progress.IsCompleted)
            {
                await EnsureDebugTutorialCompletionStateAsync(progress, cancellationToken);
                if (saveWhenCreated && _context.HasChanges)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return progress;
        }

        var shouldAutoCompleteForDebug = ShouldAutoCompleteNewProgressForDebug();
        progress = new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = shouldAutoCompleteForDebug ||
                          await ShouldAutoCompleteForExistingCharacterAsync(characterId, cancellationToken)
                ? TutorialConstants.StepComplete
                : TutorialConstants.StepDefeatTrainingCreature
        };

        if (shouldAutoCompleteForDebug)
        {
            await GrantDebugTutorialCompletionAsync(progress, cancellationToken);
        }
        else if (progress.CurrentStep == TutorialConstants.StepComplete)
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

    private bool ShouldAutoCompleteNewProgressForDebug() =>
        _debugOptions.IsDevelopment && !_debugOptions.Enabled;

    private async Task GrantDebugTutorialCompletionAsync(
        CharacterTutorialProgress progress,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        progress.TrainingCombatWonAt ??= now;
        progress.EssenceAbsorbedAt ??= now;
        progress.EssenceEquippedAt ??= now;
        progress.CraftedTierOneEquipmentCount = TutorialConstants.RequiredCraftedEquipmentCount;
        progress.EquippedTierOneEquipmentCount = TutorialConstants.RequiredEquippedEquipmentCount;

        await EnsureDebugTutorialCompletionStateAsync(progress, cancellationToken);
        Complete(progress);
    }

    private async Task EnsureDebugTutorialCompletionStateAsync(
        CharacterTutorialProgress progress,
        CancellationToken cancellationToken)
    {
        var tutorialEssence = await EnsureTutorialEssenceAbsorbedAsync(
            progress.CharacterId,
            cancellationToken);

        await EnsureTutorialEssenceEquippedAsync(
            progress.CharacterId,
            tutorialEssence,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        progress.TrainingCombatWonAt ??= now;
        progress.EssenceAbsorbedAt ??= now;
        progress.EssenceEquippedAt ??= now;
        progress.TrainingEssenceRewardGranted = true;
        progress.CraftedTierOneEquipmentCount = TutorialConstants.RequiredCraftedEquipmentCount;
        progress.EquippedTierOneEquipmentCount = TutorialConstants.RequiredEquippedEquipmentCount;
    }

    private async Task<PlayerEssence> EnsureTutorialEssenceAbsorbedAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var essence = await _context.PlayerEssences
            .FirstOrDefaultAsync(x =>
                x.CharacterId == characterId &&
                x.EssenceDefinitionId == TutorialConstants.TutorialEssenceDefinitionId,
                cancellationToken);

        if (essence is null)
        {
            var now = DateTimeOffset.UtcNow;
            essence = new PlayerEssence
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                EssenceDefinitionId = TutorialConstants.TutorialEssenceDefinitionId,
                Level = 1,
                AbsorbedAt = now,
                UpdatedAt = now
            };

            await _context.PlayerEssences.AddAsync(essence, cancellationToken);
        }

        var unboundTutorialEssences = await _context.InventoryItems
            .Include(x => x.ItemInstance)
            .Where(x =>
                x.InventoryId == characterId &&
                x.ItemInstance.ItemBaseId == TutorialConstants.TutorialEssenceItemBaseId)
            .ToListAsync(cancellationToken);

        if (unboundTutorialEssences.Count > 0)
        {
            _context.InventoryItems.RemoveRange(unboundTutorialEssences);
        }

        return essence;
    }

    private async Task EnsureTutorialEssenceEquippedAsync(
        Guid characterId,
        PlayerEssence tutorialEssence,
        CancellationToken cancellationToken)
    {
        var loadouts = await _context.EssenceLoadouts
            .Include(x => x.Slots)
            .Where(x => x.CharacterId == characterId)
            .ToListAsync(cancellationToken);

        var activeLoadout = loadouts.FirstOrDefault(x => x.IsActive) ?? loadouts.FirstOrDefault();
        if (activeLoadout is null)
        {
            activeLoadout = new EssenceLoadout
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                Name = "Default",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _context.EssenceLoadouts.AddAsync(activeLoadout, cancellationToken);
            loadouts.Add(activeLoadout);
        }

        foreach (var loadout in loadouts)
        {
            loadout.IsActive = loadout.Id == activeLoadout.Id;
        }

        activeLoadout.UpdatedAt = DateTimeOffset.UtcNow;

        var duplicateSlots = activeLoadout.Slots
            .Where(slot =>
                (slot.SlotIndex == 0 && slot.PlayerEssenceId != tutorialEssence.Id) ||
                (slot.PlayerEssenceId == tutorialEssence.Id && slot.SlotIndex != 0))
            .ToList();

        foreach (var duplicateSlot in duplicateSlots)
        {
            activeLoadout.Slots.Remove(duplicateSlot);
            _context.EssenceLoadoutSlots.Remove(duplicateSlot);
        }

        var slotZero = activeLoadout.Slots.FirstOrDefault(x => x.SlotIndex == 0);
        if (slotZero is null)
        {
            slotZero = new EssenceLoadoutSlot
            {
                Id = Guid.NewGuid(),
                EssenceLoadoutId = activeLoadout.Id,
                SlotIndex = 0
            };
            activeLoadout.Slots.Add(slotZero);
            await _context.EssenceLoadoutSlots.AddAsync(slotZero, cancellationToken);
        }

        slotZero.PlayerEssenceId = tutorialEssence.Id;
        slotZero.PlayerEssence = tutorialEssence;
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

    private async Task<bool> EnsureTutorialCraftingMaterialsAsync(
        CharacterTutorialProgress progress,
        CancellationToken cancellationToken)
    {
        var requiredQuantities = new Dictionary<string, int>
        {
            [TutorialConstants.TutorialCraftingOreItemBaseId] =
                TutorialConstants.TutorialCraftingOreQuantity,
            [TutorialConstants.TutorialCraftingWoodItemBaseId] =
                TutorialConstants.TutorialCraftingWoodQuantity
        };
        var missingQuantities = new Dictionary<string, int>();

        foreach (var (itemBaseId, requiredQuantity) in requiredQuantities)
        {
            var ownedQuantity = await _inventory.GetInventoryQuantityAsync(
                progress.CharacterId,
                itemBaseId,
                cancellationToken);
            if (ownedQuantity < requiredQuantity)
            {
                missingQuantities[itemBaseId] = requiredQuantity - ownedQuantity;
            }
        }

        if (missingQuantities.Count == 0)
        {
            return false;
        }

        await AddItemRewardsAsync(
            progress.CharacterId,
            missingQuantities,
            cancellationToken,
            publishAsLoot: true);
        return true;
    }

    private async Task<IReadOnlyList<InventoryItem>> EnsureTutorialGatheringToolsAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var missingTools = new Dictionary<string, int>();

        foreach (var itemBaseId in TutorialConstants.TutorialGatheringToolItemBaseIds)
        {
            var inventoryQuantity = await _inventory.GetInventoryQuantityAsync(
                characterId,
                itemBaseId,
                cancellationToken);
            var isEquipped = await _context.EquipmentSlots.AnyAsync(
                slot =>
                    slot.EntityId == characterId &&
                    slot.EquipmentInstance != null &&
                    slot.EquipmentInstance.ItemBaseId == itemBaseId,
                cancellationToken);

            if (inventoryQuantity == 0 && !isEquipped)
            {
                missingTools[itemBaseId] = 1;
            }
        }

        return missingTools.Count == 0
            ? []
            : await AddItemRewardsAsync(
                characterId,
                missingTools,
                cancellationToken,
                publishAsLoot: true);
    }

    private async Task<EquipmentInstance?> EnsureTutorialStarterWeaponAsync(
        Guid characterId,
        CancellationToken cancellationToken)
    {
        var itemBaseId = TutorialConstants.TutorialStarterWeaponItemBaseId;
        var isEquipped = await _context.EquipmentSlots
            .Include(slot => slot.EquipmentInstance)
            .AnyAsync(
                slot =>
                    slot.EntityId == characterId &&
                    slot.EquipmentInstance != null &&
                    slot.EquipmentInstance.ItemBaseId == itemBaseId,
                cancellationToken);

        if (isEquipped)
        {
            return null;
        }

        var inventoryItem = await _context.InventoryItems
            .Include(item => item.ItemInstance)
            .ThenInclude(instance => instance.ItemBase)
            .FirstOrDefaultAsync(
                item =>
                    item.InventoryId == characterId &&
                    item.ItemInstance.ItemBaseId == itemBaseId,
                cancellationToken);
        if (inventoryItem?.ItemInstance is EquipmentInstance existingWeapon)
        {
            return existingWeapon;
        }

        var grantedItems = await AddItemRewardsAsync(
            characterId,
            new Dictionary<string, int> { [itemBaseId] = 1 },
            cancellationToken,
            publishAsLoot: true);
        return grantedItems
            .Select(item => item.ItemInstance)
            .OfType<EquipmentInstance>()
            .Single();
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
            TutorialConstants.StepEquipGatheringTool => progress.EquippedTierOneEquipmentCount,
            _ => 0
        };
        var currentStepIndex = definition.Steps.FindIndex(candidate =>
            candidate.Key.Equals(progress.CurrentStep, StringComparison.OrdinalIgnoreCase)) + 1;

        return new TutorialState(
            progress.TutorialId,
            definition.Title,
            definition.Version,
            progress.CurrentStep,
            step.Objective,
            current,
            step.RequiredAmount,
            currentStepIndex,
            definition.Steps.Count,
            new TutorialStepPresentation(
                step.ActionLabel,
                step.DestinationRoute,
                step.GuidePageId,
                step.TourPageId),
            step.ActionLabel,
            step.DestinationRoute,
            step.GuidePageId,
            step.TourPageId,
            !progress.WelcomeAcknowledgedAt.HasValue,
            progress.IsCompleted);
    }

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
        CancellationToken cancellationToken,
        bool wasSkipped = false)
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
                new TutorialCompletedMsg(
                    progress.TutorialId,
                    0,
                    wasSkipped
                        ? "/game/world/shenic?area=region_01_area_01"
                        : "/game/combat",
                    wasSkipped));
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

    private static TutorialCompletion CreateCompletion(bool wasSkipped) =>
        new(
            TutorialConstants.FirstStepsTutorialId,
            0,
            wasSkipped
                ? "/game/world/shenic?area=region_01_area_01"
                : "/game/combat",
            wasSkipped);

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
