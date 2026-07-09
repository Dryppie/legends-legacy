using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Professions;
using Application.UseCases.Crafting;
using Application.UseCases.Crafting.Dtos;
using Application.UseCases.Outbox;
using Application.UseCases.Prophecies.Events;
using Application.UseCases.Soulstones.Events;
using AutoMapper;
using Common.Primitives;
using Domain.Helpers.Constants;
using Domain.Models.Bonuses;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Guilds.Missions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using MediatR;
using Services.LL.Extensions;
using Services.LL.Interfaces;

namespace Services.LL.Professions.Craftings;

public class CraftingService : ICraftingService
{
    private readonly ICraftingRepository _craftingRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IProfessionService _professionService;
    private readonly ITemperingService _temperingService;
    private readonly ILevelingService _levelingService;
    private readonly IBonusService _bonusService;
    private readonly ILootService _lootService;
    private readonly IPublisher _publisher;
    private readonly ICraftingDefinitionProvider _definitions;
    private readonly ICraftingRequirementResolver _requirementResolver;
    private readonly IItemQualityRollService _qualityRollService;
    private readonly IItemPotentialService _potentialService;
    private readonly IItemStatRollService _statRollService;
    private readonly ICraftingProgressionService _progressionService;
    private readonly ICraftingItemCatalogService _itemCatalogService;
    private readonly IGameEventOutbox _outbox;
    private readonly IGuildMissionService _guildMissionService;
    private readonly IMapper _mapper;

    public CraftingService(
        ICraftingRepository cr,
        IInventoryService invS,
        IProfessionService ps,
        ITemperingService ts,
        ILevelingService lvlS,
        IBonusService bs,
        ILootService ls,
        IPublisher p,
        ICraftingDefinitionProvider definitions,
        ICraftingRequirementResolver requirementResolver,
        IItemQualityRollService qualityRollService,
        IItemPotentialService potentialService,
        IItemStatRollService statRollService,
        ICraftingProgressionService progressionService,
        ICraftingItemCatalogService itemCatalogService,
        IGameEventOutbox outbox,
        IGuildMissionService guildMissionService,
        IMapper mapper)
    {
        _craftingRepository = cr;
        _inventoryService = invS;
        _professionService = ps;
        _temperingService = ts;
        _levelingService = lvlS;
        _bonusService = bs;
        _lootService = ls;
        _publisher = p;
        _definitions = definitions;
        _requirementResolver = requirementResolver;
        _qualityRollService = qualityRollService;
        _potentialService = potentialService;
        _statRollService = statRollService;
        _progressionService = progressionService;
        _itemCatalogService = itemCatalogService;
        _outbox = outbox;
        _guildMissionService = guildMissionService;
        _mapper = mapper;
    }

    public async Task<TemperingSession> PerformIdleCrafting(CharacterAction characterAction, int actionsToPerform, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var actionDetails = (characterAction.ActionDetails as CraftingActionDetails)!;
        var sessionStartedAt = characterAction.UpdatedAt;

        var temperingSummary = new TemperingSummary();
        var completedItems = new List<EquipmentInstance>();
        var rng = Random.Shared;

        var factors = await _bonusService.GetAggregatedAsync(characterAction.CharacterId, now, cancellationToken);

        var craftingExperienceGainBps = factors.Get(BonusKind.CraftingExperienceGainBps);
        var negativeOutcomeReductionBps = factors.Get(BonusKind.TemperingNegativeOutcomeReductionBps);

        while (actionsToPerform > 0 && actionDetails.CraftingQueueItems.Count > 0)
        {
            var current = actionDetails.CraftingQueueItems.First();
            if (!_temperingService.HandleTempering(current, temperingSummary, rng, craftingExperienceGainBps, negativeOutcomeReductionBps))
            {
                await CompleteCurrentQueueItemAsync(characterAction.CharacterId, actionDetails, current, temperingSummary, completedItems, cancellationToken);
                continue;
            }

            characterAction.UpdatedAt += TimeSpan.FromSeconds(TemperingConstants.ActionDurationSeconds);
            actionsToPerform--;
            temperingSummary.TotalActions++;

            if (!_temperingService.CanTemper(current))
            {
                await CompleteCurrentQueueItemAsync(characterAction.CharacterId, actionDetails, current, temperingSummary, completedItems, cancellationToken);
            }
        }

        if (actionDetails.CraftingQueueItems.Count == 0)
        {
            characterAction.IsDeleted = true;
        }

        temperingSummary.TotalSoulstones = await ProcessSoulstoneDrops(
            characterAction.CharacterId,
            temperingSummary.TotalActions,
            cancellationToken);
        await UpdateCharacterProfessionsAsync(characterAction.CharacterId, temperingSummary, cancellationToken);
        await _outbox.EnqueueAsync(
            GameEventTypes.EquipmentTempered,
            new EquipmentTemperedPayload(
                characterAction.CharacterId,
                temperingSummary,
                [.. completedItems.Select(ToOutboxEquipmentItem)]),
            characterAction.CharacterId,
            null,
            cancellationToken);
        await RecordGuildCraftingContributionsAsync(
            characterAction.CharacterId,
            sessionStartedAt,
            now,
            temperingSummary,
            completedItems.Count,
            cancellationToken);
        await PublishProphecyProgressAsync(characterAction.CharacterId, now, temperingSummary, cancellationToken);

        return new TemperingSession
        {
            From = sessionStartedAt,
            To = now,
            TemperingSummary = temperingSummary
        };
    }

    public async Task<bool> RemoveCraftingQueueItemsAsync(Guid characterId, List<Guid> queueItemIds, CancellationToken cancellationToken)
    {
        var anyItemAdded = false;

        foreach (var queueItemId in queueItemIds)
        {
            var equipmentInstance = await _craftingRepository.RemoveCraftingQueueItemAndReturnItemAsync(characterId, queueItemId, cancellationToken);
            if (equipmentInstance == null) continue;

            var itemAdded = await _inventoryService.AddItemInstanceBackToInventory(characterId, equipmentInstance, cancellationToken);
            if (itemAdded) anyItemAdded = true;
        }

        return anyItemAdded;
    }

    public async Task<Response<IReadOnlyList<CraftingRecipeDto>>> GetCraftingRecipesAsync(Guid characterId, int targetTier, CancellationToken cancellationToken)
    {
        var unlocked = await _progressionService.GetUnlockedRecipeIdsAsync(characterId, cancellationToken);
        var unlockedBlueprintsByRecipe = await _progressionService.GetUnlockedBlueprintIdsByRecipeIdAsync(characterId, cancellationToken);
        var masteries = await _progressionService.GetRecipeMasteryLevelsAsync(characterId, cancellationToken);
        var ownedByItemId = await GetOwnedItemQuantitiesAsync(characterId, cancellationToken);

        var recipes = _definitions.GetRecipes()
            .Where(x => x.RecipeType == RecipeType.Base || unlocked.Contains(x.Id))
            .Select(recipe => ToRecipeDto(
                recipe,
                targetTier,
                masteries.GetValueOrDefault(recipe.Id),
                ownedByItemId,
                unlockedBlueprintsByRecipe))
            .OrderBy(x => x.BaseRecipeId)
            .ThenBy(x => x.RecipeType)
            .ThenBy(x => x.Name)
            .ToList();

        return Response<IReadOnlyList<CraftingRecipeDto>>.Success(recipes);
    }

    public async Task<Response<IReadOnlyList<BlueprintLearningOptionDto>>> GetBlueprintLearningOptionsAsync(
        Guid characterId,
        Guid blueprintItemInstanceId,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetInventoryByIdAsync(characterId, cancellationToken);
        var inventoryItem = inventory?.InventoryItems.FirstOrDefault(x => x.ItemInstanceId == blueprintItemInstanceId);
        if (inventoryItem == null) return Response<IReadOnlyList<BlueprintLearningOptionDto>>.Fail("Blueprint item was not found.");

        var blueprint = _definitions.GetBlueprintByItemId(inventoryItem.ItemInstance.ItemBaseId);
        if (blueprint == null) return Response<IReadOnlyList<BlueprintLearningOptionDto>>.Fail("Item is not a learnable blueprint.");

        var usesCompatibilityUnlock = UsesCompatibilityUnlock(blueprint);
        var unlockedRecipeIds = await _progressionService.GetUnlockedRecipeIdsAsync(characterId, cancellationToken);
        var unlockedBlueprintsByRecipe = await _progressionService.GetUnlockedBlueprintIdsByRecipeIdAsync(characterId, cancellationToken);

        var options = _definitions.GetRecipes()
            .Where(recipe => usesCompatibilityUnlock
                ? recipe.RecipeType == RecipeType.Base
                : recipe.Id.Equals(blueprint.UnlocksRecipeId, StringComparison.OrdinalIgnoreCase))
            .Select(recipe => TryCreateBlueprintLearningOption(recipe, blueprint, usesCompatibilityUnlock, unlockedRecipeIds, unlockedBlueprintsByRecipe))
            .Where(option => option != null)
            .Select(option => option!)
            .OrderBy(option => option.RecipeName)
            .ToList();

        return Response<IReadOnlyList<BlueprintLearningOptionDto>>.Success(options);
    }

    public async Task<Response<LearnBlueprintResult>> LearnBlueprintAsync(
        Guid characterId,
        Guid blueprintItemInstanceId,
        string recipeId,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetInventoryByIdAsync(characterId, cancellationToken);
        var inventoryItem = inventory?.InventoryItems.FirstOrDefault(x => x.ItemInstanceId == blueprintItemInstanceId);
        if (inventoryItem == null) return Response<LearnBlueprintResult>.Fail("Blueprint item was not found.");

        var blueprint = _definitions.GetBlueprintByItemId(inventoryItem.ItemInstance.ItemBaseId);
        if (blueprint == null) return Response<LearnBlueprintResult>.Fail("Item is not a learnable blueprint.");

        var usesCompatibilityUnlock = UsesCompatibilityUnlock(blueprint);
        var requestedRecipeId = recipeId.Trim();
        var unlockRecipeId = usesCompatibilityUnlock ? requestedRecipeId : blueprint.UnlocksRecipeId;
        if (string.IsNullOrWhiteSpace(unlockRecipeId))
            return Response<LearnBlueprintResult>.Fail("Select a recipe for this blueprint.");

        var recipe = _definitions.GetRecipe(unlockRecipeId);
        if (recipe == null) return Response<LearnBlueprintResult>.Fail("Blueprint unlock target does not exist.");

        var validationError = ValidateBlueprintUnlockTarget(blueprint, recipe, requestedRecipeId, usesCompatibilityUnlock);
        if (validationError != null) return Response<LearnBlueprintResult>.Fail(validationError);

        var unlocked = usesCompatibilityUnlock
            ? await _progressionService.TryUnlockBlueprintForRecipeAsync(characterId, recipe.Id, blueprint.Id, cancellationToken)
            : await _progressionService.TryUnlockRecipeAsync(characterId, recipe.Id, blueprint.Id, cancellationToken);
        if (!unlocked) return Response<LearnBlueprintResult>.Fail("Blueprint is already known for this recipe.");

        await _inventoryService.TryConsumeInventoryItemAsync(characterId, blueprintItemInstanceId, cancellationToken);
        await _outbox.EnqueueAsync(
            GameEventTypes.BlueprintUnlocked,
            new BlueprintUnlockedPayload(characterId),
            characterId,
            null,
            cancellationToken);

        return Response<LearnBlueprintResult>.Success(new LearnBlueprintResult(blueprint.Id, recipe.Id, recipe.Name));
    }

    public async Task<Response<CraftItemsResult>> CraftItemsAsync(
        Guid characterId,
        string recipeId,
        string? formId,
        string? blueprintId,
        int targetTier,
        int quantity,
        CancellationToken cancellationToken)
    {
        var craftQuantity = Math.Clamp(quantity, 1, 100);
        var recipe = _definitions.GetRecipe(recipeId);
        if (recipe == null) return Response<CraftItemsResult>.Fail("Recipe does not exist.");
        if (targetTier < recipe.TierRange.Min || targetTier > recipe.TierRange.Max)
            return Response<CraftItemsResult>.Fail("Recipe cannot be crafted at the selected tier.");

        if (recipe.RecipeType == RecipeType.Variant)
        {
            var hasUnlock = await _progressionService.HasRecipeUnlockAsync(characterId, recipe.Id, cancellationToken);
            if (!hasUnlock) return Response<CraftItemsResult>.Fail("Recipe variant is locked.");
        }

        var form = ResolveForm(recipe, formId);
        if (recipe.Forms.Count > 0 && form == null)
            return Response<CraftItemsResult>.Fail("Recipe form does not exist.");

        var blueprint = await ResolveCraftingBlueprintAsync(characterId, recipe, form, blueprintId, cancellationToken);
        if (blueprint.Error != null) return Response<CraftItemsResult>.Fail(blueprint.Error);

        var outputItemId = form?.OutputItemId ?? recipe.OutputItemId;
        var itemBase = await _itemCatalogService.GetCraftableEquipmentBaseAsync(outputItemId, cancellationToken);
        if (itemBase == null) return Response<CraftItemsResult>.Fail("Recipe output item does not exist.");
        if (itemBase.EquipmentType == EquipmentType.Tool) return Response<CraftItemsResult>.Fail("Tools cannot be crafted.");
        var professionType = ResolveCraftingProfession(itemBase.EquipmentType);
        if (professionType == ProfessionType.None)
            return Response<CraftItemsResult>.Fail("Recipe output does not map to a crafting profession.");

        var resolvedCosts = _requirementResolver.ResolveCosts(recipe, targetTier, blueprint.Value?.SpecialResourceRequirements);
        var tierValidationError = ValidateTierDefiningMaterialCosts(resolvedCosts, targetTier);
        if (tierValidationError != null) return Response<CraftItemsResult>.Fail(tierValidationError);

        var costs = resolvedCosts
            .Select(cost => new Material
            {
                ItemId = cost.ItemId,
                Quantity = cost.Quantity * craftQuantity
            })
            .ToList();
        var removedMaterials = await _inventoryService.TryRemoveCraftingMaterialsAsync(characterId, costs, cancellationToken);
        if (!removedMaterials) return Response<CraftItemsResult>.Fail("Not enough materials.");

        var mastery = await _progressionService.GetOrCreateRecipeMasteryAsync(characterId, recipe.Id, cancellationToken);
        var craftingLevel = await _professionService.GetProfessionLevelAsync(characterId, professionType, cancellationToken);
        var rng = Random.Shared;
        var created = new List<InventoryItem>();
        var qualityCounts = new Dictionary<ItemQuality, int>();

        for (var i = 0; i < craftQuantity; i++)
        {
            var quality = _qualityRollService.RollQuality(recipe.Id, mastery.Level, rng);
            var potential = _potentialService.CalculateStartingPotential(itemBase, targetTier, quality, mastery.Level, craftingLevel);
            qualityCounts[quality] = qualityCounts.GetValueOrDefault(quality) + 1;

            var equipmentInstance = new EquipmentInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase,
                RecipeId = recipe.Id,
                BaseRecipeId = recipe.BaseRecipeId ?? recipe.Id,
                BlueprintId = blueprint.Value?.Id,
                CraftedName = blueprint.Value == null
                    ? null
                    : CraftingBlueprintRules.ResolveOutputName(blueprint.Value, recipe, form, itemBase.Name),
                Tier = targetTier,
                Rarity = Rarity.Common,
                Quality = quality,
                Potential = potential,
                MaxPotential = potential,
                TemperingProgress = 0,
                AffinityTags = [.. recipe.AffinityTags.Concat(form?.Tags ?? []).Concat(blueprint.Value?.Tags ?? [])],
                SpecialModifiers = [],
                InstanceModifiers = [.. _statRollService.RollBaseStats(itemBase, recipe, targetTier, quality, rng)]
            };

            created.Add(new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = equipmentInstance.Id,
                Quantity = 1,
                ItemInstance = equipmentInstance
            });
        }

        await _inventoryService.AddItemsToInventory(characterId, created, cancellationToken);
        var craftedEquipment = created.Select(x => (EquipmentInstance)x.ItemInstance).ToList();
        await _outbox.EnqueueAsync(
            GameEventTypes.EquipmentCrafted,
            new EquipmentCraftedPayload(
                characterId,
                [.. craftedEquipment.Select(ToOutboxEquipmentItem)]),
            characterId,
            null,
            cancellationToken);
        var craftedAt = DateTimeOffset.UtcNow;
        await _guildMissionService.RecordContributionAsync(
            new GuildContributionEvent(
                characterId,
                GuildContributionSource.Crafting,
                GuildContributionMetric.ItemsCrafted,
                created.Count,
                OccurredAt: craftedAt,
                IdempotencyKey: $"craft-items:{characterId}:{recipe.Id}:{targetTier}:{created.Count}:{craftedAt:O}"),
            cancellationToken);
        var xpGained = craftQuantity * CraftingMasteryProgression.ExperiencePerCraft;
        mastery.Experience += xpGained;
        mastery.Level = CraftingMasteryProgression.GetLevelForExperience(mastery.Experience);
        mastery.UpdatedAt = DateTimeOffset.UtcNow;

        return Response<CraftItemsResult>.Success(new CraftItemsResult(
            recipe.Id,
            targetTier,
            created,
            qualityCounts,
            xpGained,
            mastery.Level));
    }

    private async Task CompleteCurrentQueueItemAsync(
        Guid characterId,
        CraftingActionDetails actionDetails,
        CraftingQueueItem current,
        TemperingSummary temperingSummary,
        List<EquipmentInstance> completedItems,
        CancellationToken cancellationToken)
    {
        temperingSummary.TotalItemsCrafted++;
        actionDetails.CraftingQueueItems.Remove(current);
        completedItems.Add(current.EquipmentInstance);
        await _inventoryService.AddItemInstanceBackToInventory(characterId, current.EquipmentInstance, cancellationToken);
    }

    private async Task RecordGuildCraftingContributionsAsync(
        Guid characterId,
        DateTimeOffset sessionStartedAt,
        DateTimeOffset now,
        TemperingSummary temperingSummary,
        int completedItemCount,
        CancellationToken cancellationToken)
    {
        if (temperingSummary.TotalActions > 0)
        {
            await _guildMissionService.RecordContributionAsync(
                new GuildContributionEvent(
                    characterId,
                    GuildContributionSource.Tempering,
                    GuildContributionMetric.TemperingActionsCompleted,
                    temperingSummary.TotalActions,
                    OccurredAt: now,
                    IdempotencyKey: $"tempering:{characterId}:{sessionStartedAt:O}:{now:O}:{temperingSummary.TotalActions}"),
                cancellationToken);
        }

        if (completedItemCount > 0)
        {
            await _guildMissionService.RecordContributionAsync(
                new GuildContributionEvent(
                    characterId,
                    GuildContributionSource.Crafting,
                    GuildContributionMetric.ItemsCrafted,
                    completedItemCount,
                    OccurredAt: now,
                    IdempotencyKey: $"tempering-items:{characterId}:{sessionStartedAt:O}:{now:O}:{completedItemCount}"),
                cancellationToken);
        }
    }

    private async Task<int> ProcessSoulstoneDrops(Guid characterId, int actionsPerformed, CancellationToken cancellationToken)
    {
        var durationInSeconds = TemperingConstants.ActionDurationSeconds * actionsPerformed;
        var soulstonesEarned = _lootService.GenerateSoulstoneLoot(durationInSeconds);
        if (soulstonesEarned < 1) return 0;

        await _publisher.Publish(new SoulstoneDropEvent(characterId, soulstonesEarned), cancellationToken);
        return soulstonesEarned;
    }

    private async Task PublishProphecyProgressAsync(
        Guid characterId,
        DateTimeOffset occurredAt,
        TemperingSummary temperingSummary,
        CancellationToken cancellationToken)
    {
        if (temperingSummary.TotalActions <= 0)
        {
            return;
        }

        await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
            characterId,
            occurredAt,
            ProphecyProgressKind.ItemTempered,
            temperingSummary.TotalActions)), cancellationToken);

        await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
            characterId,
            occurredAt,
            ProphecyProgressKind.PotentialSpent,
            temperingSummary.TotalActions,
            PotentialSpent: temperingSummary.TotalActions)), cancellationToken);
    }

    private async Task UpdateCharacterProfessionsAsync(Guid characterId, TemperingSummary temperingSummary, CancellationToken cancellationToken)
    {
        if (temperingSummary.TotalExperience == 0) return;
        var profession = await _professionService.GetOrCreateProfessionAsync(characterId, ProfessionType.Crafting, cancellationToken);
        profession.Experience += temperingSummary.TotalExperience;

        await _levelingService.UpdateProfessionLevel(profession, cancellationToken);
        _professionService.UpdateProfessionLevel([profession]);
    }

    private async Task<IReadOnlyDictionary<string, int>> GetOwnedItemQuantitiesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetInventoryByIdAsync(characterId, cancellationToken);
        return inventory?.InventoryItems
            .GroupBy(x => x.ItemInstance.ItemBaseId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    private CraftingRecipeDto ToRecipeDto(
        CraftingRecipeDefinition recipe,
        int targetTier,
        int masteryLevel,
        IReadOnlyDictionary<string, int> ownedByItemId,
        IReadOnlyDictionary<string, IReadOnlySet<string>> unlockedBlueprintsByRecipe)
    {
        var tier = Math.Clamp(targetTier, recipe.TierRange.Min, recipe.TierRange.Max);
        var costs = MapMaterialCosts(_requirementResolver.ResolveCosts(recipe, tier), ownedByItemId);
        var blueprintOptions = _definitions.GetBlueprints()
            .Where(blueprint =>
                unlockedBlueprintsByRecipe.TryGetValue(recipe.Id, out var blueprintIds) &&
                blueprintIds.Contains(blueprint.Id))
            .Select(blueprint => ToBlueprintOption(recipe, blueprint, tier, ownedByItemId))
            .Where(option => option != null)
            .Select(option => option!)
            .OrderBy(option => option.Name)
            .ToList();

        return _mapper.Map<CraftingRecipeDto>(recipe, opt =>
        {
            opt.Items["CurrentMasteryLevel"] = masteryLevel;
            opt.Items["Blueprints"] = blueprintOptions;
            opt.Items["MaterialCosts"] = costs;
        });
    }

    private CraftingBlueprintOptionDto? ToBlueprintOption(
        CraftingRecipeDefinition recipe,
        BlueprintDefinition blueprint,
        int tier,
        IReadOnlyDictionary<string, int> ownedByItemId)
    {
        var compatibleFormIds = CraftingBlueprintRules.GetCompatibleFormIds(blueprint, recipe);
        var compatibleWithoutForms = recipe.Forms.Count == 0 && CraftingBlueprintRules.IsCompatible(blueprint, recipe, null);
        if (!compatibleWithoutForms && compatibleFormIds.Count == 0) return null;

        var materialCosts = MapMaterialCosts(
            _requirementResolver.ResolveCosts(recipe, tier, blueprint.SpecialResourceRequirements),
            ownedByItemId);

        return _mapper.Map<CraftingBlueprintOptionDto>(blueprint, opt =>
        {
            opt.Items["CompatibleFormIds"] = compatibleFormIds;
            opt.Items["MaterialCosts"] = materialCosts;
        });
    }

    private IReadOnlyList<CraftingMaterialCostDto> MapMaterialCosts(
        IReadOnlyList<ResolvedMaterialCost> costs,
        IReadOnlyDictionary<string, int> ownedByItemId)
    {
        return _mapper.Map<IReadOnlyList<CraftingMaterialCostDto>>(costs, opt =>
            opt.Items["OwnedByItemId"] = ownedByItemId);
    }

    private BlueprintLearningOptionDto? TryCreateBlueprintLearningOption(
        CraftingRecipeDefinition recipe,
        BlueprintDefinition blueprint,
        bool usesCompatibilityUnlock,
        IReadOnlySet<string> unlockedRecipeIds,
        IReadOnlyDictionary<string, IReadOnlySet<string>> unlockedBlueprintsByRecipe)
    {
        if (!usesCompatibilityUnlock && unlockedRecipeIds.Contains(recipe.Id)) return null;

        if (unlockedBlueprintsByRecipe.TryGetValue(recipe.Id, out var blueprintIds) &&
            blueprintIds.Contains(blueprint.Id))
        {
            return null;
        }

        var compatibleFormIds = usesCompatibilityUnlock
            ? CraftingBlueprintRules.GetCompatibleFormIds(blueprint, recipe)
            : recipe.Forms.Select(form => form.FormId).ToList();

        if (usesCompatibilityUnlock)
        {
            var compatibleWithoutForms = recipe.Forms.Count == 0 && CraftingBlueprintRules.IsCompatible(blueprint, recipe, null);
            if (!compatibleWithoutForms && compatibleFormIds.Count == 0) return null;
        }

        var compatibleFormNames = recipe.Forms
            .Where(form => compatibleFormIds.Contains(form.FormId, StringComparer.OrdinalIgnoreCase))
            .Select(form => form.DisplayName)
            .ToList();

        return _mapper.Map<BlueprintLearningOptionDto>(recipe, opt =>
        {
            opt.Items["CompatibleFormIds"] = compatibleFormIds;
            opt.Items["CompatibleFormNames"] = compatibleFormNames;
        });
    }

    private static string? ValidateBlueprintUnlockTarget(
        BlueprintDefinition blueprint,
        CraftingRecipeDefinition recipe,
        string requestedRecipeId,
        bool usesCompatibilityUnlock)
    {
        if (usesCompatibilityUnlock)
        {
            if (recipe.RecipeType != RecipeType.Base)
                return "Blueprints can only be applied to base recipes.";

            var compatibleFormIds = CraftingBlueprintRules.GetCompatibleFormIds(blueprint, recipe);
            var compatibleWithoutForms = recipe.Forms.Count == 0 && CraftingBlueprintRules.IsCompatible(blueprint, recipe, null);
            if (!compatibleWithoutForms && compatibleFormIds.Count == 0)
                return "Blueprint is not compatible with this recipe.";
        }
        else if (!string.IsNullOrWhiteSpace(requestedRecipeId) &&
                 !requestedRecipeId.Equals(blueprint.UnlocksRecipeId, StringComparison.OrdinalIgnoreCase))
        {
            return "Blueprint cannot unlock the selected recipe.";
        }

        return null;
    }

    private static CraftingRecipeFormDefinition? ResolveForm(CraftingRecipeDefinition recipe, string? formId)
    {
        if (recipe.Forms.Count == 0) return null;
        if (string.IsNullOrWhiteSpace(formId)) return recipe.Forms[0];

        return recipe.Forms.FirstOrDefault(x => x.FormId.Equals(formId, StringComparison.OrdinalIgnoreCase));
    }

    private static ProfessionType ResolveCraftingProfession(EquipmentType equipmentType)
    {
        return equipmentType switch
        {
            EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs => ProfessionType.Crafting,
            EquipmentType.Ring or EquipmentType.Necklace or EquipmentType.Relic => ProfessionType.Crafting,
            EquipmentType.OneHanded or EquipmentType.TwoHanded or EquipmentType.OffHand => ProfessionType.Crafting,
            _ => ProfessionType.None
        };
    }

    private static string? ValidateTierDefiningMaterialCosts(IReadOnlyList<ResolvedMaterialCost> costs, int targetTier)
    {
        var tierDefiningCosts = costs.Where(x => x.Tier.HasValue).ToList();
        if (tierDefiningCosts.Count == 0)
            return "Crafted equipment requires tier-defining materials.";

        var mismatchedCost = tierDefiningCosts.FirstOrDefault(x => x.Tier!.Value != targetTier);
        if (mismatchedCost != null)
            return $"Material '{mismatchedCost.Name}' is tier {mismatchedCost.Tier}; crafted equipment tier must match the primary material tier.";

        var mixedTier = tierDefiningCosts
            .Select(x => x.Tier!.Value)
            .Distinct()
            .Skip(1)
            .Any();
        if (mixedTier)
            return "Tier-defining materials must all come from the same tier.";

        return null;
    }

    private async Task<(BlueprintDefinition? Value, string? Error)> ResolveCraftingBlueprintAsync(
        Guid characterId,
        CraftingRecipeDefinition recipe,
        CraftingRecipeFormDefinition? form,
        string? blueprintId,
        CancellationToken cancellationToken)
    {
        var requestedBlueprintId = string.IsNullOrWhiteSpace(blueprintId)
            ? recipe.BlueprintId
            : blueprintId;
        if (string.IsNullOrWhiteSpace(requestedBlueprintId)) return (null, null);

        var blueprint = _definitions.GetBlueprint(requestedBlueprintId);
        if (blueprint == null) return (null, "Blueprint does not exist.");

        var hasUnlock = await _progressionService.HasBlueprintUnlockAsync(characterId, recipe.Id, blueprint.Id, cancellationToken);
        if (!hasUnlock) return (null, "Blueprint is locked.");

        if (!CraftingBlueprintRules.IsCompatible(blueprint, recipe, form))
            return (null, "Blueprint is not compatible with this recipe form.");

        return (blueprint, null);
    }

    private static bool UsesCompatibilityUnlock(BlueprintDefinition blueprint)
    {
        return blueprint.AllowedBaseRecipeIds.Count > 0 || blueprint.AllowedRecipeTags.Count > 0;
    }

    private static OutboxEquipmentItemPayload ToOutboxEquipmentItem(EquipmentInstance item) =>
        new(
            item.ItemBaseId,
            item.Tier,
            item.Rarity,
            item.Quality,
            item.Potential,
            item.RecipeId,
            item.BaseRecipeId,
            item.BlueprintId,
            item.AffinityTags,
            item.SpecialModifiers,
            item.IsMasterpiece);
}
