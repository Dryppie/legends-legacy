using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Professions;
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
using Services.LL.Interfaces.Combat.Reward;
using Common.Randomness;
using System.Globalization;

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
    private readonly IResolutionRandomSource? _resolutionRandom;

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
        IMapper mapper,
        IResolutionRandomSource? resolutionRandom = null)
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
        _resolutionRandom = resolutionRandom;
    }

    public async Task<TemperingSession> PerformIdleCrafting(
        CharacterAction characterAction,
        int actionsToPerform,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var actionDetails = (characterAction.ActionDetails as CraftingActionDetails)!;
        var sessionStartedAt = characterAction.NextResolutionAtUtc
            ?? throw new InvalidOperationException("Active tempering requires a next-resolution boundary.");

        var temperingSummary = new TemperingSummary();
        var outcomes = new List<TemperingOutcomeEntry>();
        var completedItems = new List<EquipmentInstance>();
        using var randomScope = _resolutionRandom?.UseSeed(StableRandom.Seed(
            "tempering-batch-v1",
            characterAction.CharacterId.ToString("N"),
            characterAction.ScheduleGeneration.ToString(CultureInfo.InvariantCulture),
            sessionStartedAt.UtcTicks.ToString(CultureInfo.InvariantCulture)));

        var factors = await _bonusService.GetAggregatedAsync(characterAction.CharacterId, now, cancellationToken);

        var craftingExperienceGainBps = factors.Get(BonusKind.CraftingExperienceGainBps);
        var negativeOutcomeReductionBps = factors.Get(BonusKind.TemperingNegativeOutcomeReductionBps);

        while (actionsToPerform > 0 && actionDetails.CraftingQueueItems.Count > 0)
        {
            var current = actionDetails.CraftingQueueItems.First();
            var attemptBoundary = characterAction.NextResolutionAtUtc
                ?? throw new InvalidOperationException("Active tempering requires a next-resolution boundary.");
            var identity = new[]
            {
                "tempering-attempt-v1",
                characterAction.CharacterId.ToString("N"),
                characterAction.ScheduleGeneration.ToString(CultureInfo.InvariantCulture),
                attemptBoundary.UtcTicks.ToString(CultureInfo.InvariantCulture),
                current.Id.ToString("N")
            };
            var rng = new Random(StableRandom.Seed(identity));
            var result = _temperingService.HandleTempering(
                current,
                temperingSummary,
                rng,
                craftingExperienceGainBps,
                negativeOutcomeReductionBps);
            if (result == null)
            {
                await CompleteCurrentQueueItemAsync(characterAction.CharacterId, actionDetails, current, temperingSummary, completedItems, cancellationToken);
                continue;
            }

            characterAction.NextResolutionAtUtc = attemptBoundary.AddSeconds(TemperingConstants.ActionDurationSeconds);
            actionsToPerform--;
            temperingSummary.TotalActions++;
            outcomes.Add(new TemperingOutcomeEntry
            {
                Id = StableRandom.Guid(identity),
                QueueItemId = current.Id,
                EquipmentInstanceId = result.Equipment.Id,
                EquipmentName = result.Equipment.DisplayName,
                OccurredAt = attemptBoundary,
                Outcome = result.Outcome,
                PotentialSpent = result.PotentialSpent,
                PreviousPotential = result.PreviousPotential,
                NewPotential = result.NewPotential,
                PreviousItemXp = result.PreviousItemXp,
                NewItemXp = result.NewItemXp,
                BecameMasterpiece = result.BecameMasterpiece,
                BecameLevelingItem = result.BecameLevelingItem,
                PreviousRarity = result.PreviousRarity,
                NewRarity = result.NewRarity,
                RarityUpgraded = result.RarityUpgraded,
                QualityIncreased = result.QualityIncreased,
                PreviousQuality = result.PreviousQuality,
                NewQuality = result.NewQuality,
                ImprovedStat = result.ImprovedStat,
                PreviousStatValue = result.PreviousStatValue,
                NewStatValue = result.NewStatValue
            });

            if (!_temperingService.CanTemper(current))
            {
                await CompleteCurrentQueueItemAsync(characterAction.CharacterId, actionDetails, current, temperingSummary, completedItems, cancellationToken);
            }
        }

        if (actionDetails.CraftingQueueItems.Count == 0)
        {
            characterAction.IsDeleted = true;
            characterAction.NextResolutionAtUtc = null;
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
            TemperingSummary = temperingSummary,
            Outcomes = outcomes
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

    public Task<bool> MoveCraftingQueueItemAsync(
        Guid characterId,
        Guid queueItemId,
        CraftingQueueMoveDirection direction,
        CancellationToken cancellationToken) =>
        _craftingRepository.MoveCraftingQueueItemAsync(
            characterId,
            queueItemId,
            direction,
            cancellationToken);

    public async Task<Response<IReadOnlyList<CraftingRecipeDto>>> GetCraftingRecipesAsync(Guid characterId, int targetTier, CancellationToken cancellationToken)
    {
        var blueprintUnlocks = await _progressionService.GetBlueprintUnlocksAsync(characterId, cancellationToken);
        var masteries = (await _progressionService.GetRecipeMasteriesAsync(characterId, cancellationToken))
            .ToDictionary(x => x.RecipeId, StringComparer.OrdinalIgnoreCase);
        var ownedByItemId = await GetOwnedItemQuantitiesAsync(characterId, cancellationToken);
        var recipeDefinitions = _definitions.GetRecipes();
        var itemBases = await _itemCatalogService.GetCraftableEquipmentBasesAsync(
            recipeDefinitions
                .Where(recipe => recipe.Enabled)
                .Select(recipe => recipe.OutputItemId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            cancellationToken);
        var craftingLevel = await _professionService.GetProfessionLevelAsync(
            characterId,
            ProfessionType.Crafting,
            cancellationToken);

        var recipes = recipeDefinitions
            .Select(recipe => ToRecipeDto(
                recipe,
                targetTier,
                masteries.GetValueOrDefault(recipe.Id),
                ownedByItemId,
                GetUnlockedBlueprintIdsForRecipe(blueprintUnlocks, recipe.Id),
                itemBases,
                craftingLevel))
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToList();

        return Response<IReadOnlyList<CraftingRecipeDto>>.Success(recipes);
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
        if (blueprint is not { Enabled: true })
            return Response<LearnBlueprintResult>.Fail("Item is not a learnable Blueprint.");

        var recipe = _definitions.GetRecipe(recipeId);
        if (recipe is not { Enabled: true })
            return Response<LearnBlueprintResult>.Fail("Recipe does not exist.");
        if (!EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint))
            return Response<LearnBlueprintResult>.Fail("Blueprint is not compatible with the selected recipe.");

        if (await _progressionService.HasBlueprintUnlockAsync(
                characterId,
                recipe.Id,
                blueprint.Id,
                cancellationToken))
        {
            return Response<LearnBlueprintResult>.Fail(
                $"Blueprint is already learned for {recipe.Name}.");
        }

        if (!await _inventoryService.TryConsumeInventoryItemAsync(characterId, blueprintItemInstanceId, cancellationToken))
            return Response<LearnBlueprintResult>.Fail("Blueprint item could not be consumed.");

        var unlocked = await _progressionService.TryUnlockBlueprintAsync(
            characterId,
            recipe.Id,
            blueprint.Id,
            cancellationToken);
        if (!unlocked)
        {
            throw new InvalidOperationException(
                $"Concurrent Blueprint learning detected for Blueprint '{blueprint.Id}' and recipe '{recipe.Id}'.");
        }

        await _outbox.EnqueueAsync(
            GameEventTypes.BlueprintUnlocked,
            new BlueprintUnlockedPayload(characterId),
            characterId,
            null,
            cancellationToken);

        return Response<LearnBlueprintResult>.Success(new LearnBlueprintResult(
            blueprint.Id,
            blueprint.Name,
            recipe.Id,
            recipe.Name));
    }

    public async Task<Response<CraftItemsResult>> CraftItemsAsync(
        Guid characterId,
        string recipeId,
        string? blueprintId,
        int targetTier,
        int quantity,
        CancellationToken cancellationToken)
    {
        var craftQuantity = Math.Clamp(quantity, 1, 100);
        var recipe = _definitions.GetRecipe(recipeId);
        if (recipe is not { Enabled: true })
            return Response<CraftItemsResult>.Fail("Recipe does not exist.");

        BlueprintDefinition? blueprint = null;
        if (!string.IsNullOrWhiteSpace(blueprintId))
        {
            blueprint = _definitions.GetBlueprint(blueprintId);
            if (blueprint is not { Enabled: true })
                return Response<CraftItemsResult>.Fail("Blueprint does not exist.");
            if (!EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint))
                return Response<CraftItemsResult>.Fail("Blueprint is not compatible with the selected recipe.");

            var hasUnlock = await _progressionService.HasBlueprintUnlockAsync(
                characterId,
                recipe.Id,
                blueprint.Id,
                cancellationToken);
            if (!hasUnlock)
                return Response<CraftItemsResult>.Fail("Blueprint is locked.");
        }

        var design = EquipmentCraftingDesignComposer.Compose(recipe, blueprint);

        if (targetTier < recipe.TierRange.Min || targetTier > recipe.TierRange.Max)
            return Response<CraftItemsResult>.Fail("Recipe cannot be crafted at the selected tier.");
        var itemBase = await _itemCatalogService.GetCraftableEquipmentBaseAsync(recipe.OutputItemId, cancellationToken);
        if (itemBase == null) return Response<CraftItemsResult>.Fail("Recipe output item does not exist.");
        if (itemBase.EquipmentType == EquipmentType.Tool) return Response<CraftItemsResult>.Fail("Tools cannot be crafted.");
        if (itemBase.EquipmentType != recipe.OutputItemType)
            return Response<CraftItemsResult>.Fail("Recipe output slot is invalid.");

        var professionType = ResolveCraftingProfession(itemBase.EquipmentType);
        if (professionType == ProfessionType.None)
            return Response<CraftItemsResult>.Fail("Recipe output does not map to a crafting profession.");

        var craftingLevel = await _professionService.GetProfessionLevelAsync(characterId, professionType, cancellationToken);
        if (craftingLevel < recipe.MinimumProfessionLevel)
            return Response<CraftItemsResult>.Fail($"Crafting level {recipe.MinimumProfessionLevel} is required.");

        var resolvedCosts = _requirementResolver.ResolveCosts(
            recipe,
            targetTier,
            design.AdditionalMaterialRequirements);
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
                BaseRecipeId = recipe.Id,
                BlueprintId = blueprint?.Id,
                CraftedName = design.Name,
                Tier = targetTier,
                StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion,
                Rarity = Rarity.Common,
                Quality = quality,
                Potential = potential,
                MaxPotential = potential,
                TemperingProgress = 0,
                AffinityTags = [.. design.Tags],
                InstanceModifiers = [.. _statRollService.RollBaseStats(itemBase, design, targetTier, quality, rng)]
            };

            created.Add(new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = equipmentInstance.Id,
                Quantity = 1,
                ItemInstance = equipmentInstance
            });
        }

        await _inventoryService.AddItemsToInventory(
            characterId,
            created,
            ItemAcquisitionSources.Crafting,
            cancellationToken);
        var craftedEquipment = created.Select(x => (EquipmentInstance)x.ItemInstance).ToList();
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
        var blueprintFactors = await _bonusService.GetAggregatedAsync(characterId, DateTimeOffset.UtcNow, cancellationToken);
        var blueprintProgressionGainBps = blueprintFactors.Get(BonusKind.BlueprintProgressionGainBps);
        var xpGained = (craftQuantity * CraftingMasteryProgression.ExperiencePerCraft)
            .ApplyPositiveBps(blueprintProgressionGainBps);
        mastery.Experience += xpGained;
        mastery.Level = CraftingMasteryProgression.GetLevelForExperience(mastery.Experience);
        mastery.UpdatedAt = DateTimeOffset.UtcNow;

        await _outbox.EnqueueAsync(
            GameEventTypes.EquipmentCrafted,
            new EquipmentCraftedPayload(
                characterId,
                [.. craftedEquipment.Select(ToOutboxEquipmentItem)],
                mastery.Level),
            characterId,
            null,
            cancellationToken);

        return Response<CraftItemsResult>.Success(new CraftItemsResult(
            recipe.Id,
            blueprint?.Id,
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
        _craftingRepository.RemoveCompletedCraftingQueueItem(actionDetails, current);
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

        await _publisher.Publish(new ProphecyProgressBatchNotification(
        [
            new ProphecyProgressEvent(
                characterId,
                occurredAt,
                ProphecyProgressKind.ItemTempered,
                temperingSummary.TotalActions),
            new ProphecyProgressEvent(
                characterId,
                occurredAt,
                ProphecyProgressKind.PotentialSpent,
                temperingSummary.TotalActions,
                PotentialSpent: temperingSummary.TotalActions)
        ]), cancellationToken);
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

    private static IReadOnlySet<string> GetUnlockedBlueprintIdsForRecipe(
        IReadOnlyList<CharacterRecipeUnlock> unlocks,
        string recipeId) =>
        unlocks
            .Where(unlock =>
                unlock.RecipeId == null ||
                unlock.RecipeId.Equals(recipeId, StringComparison.OrdinalIgnoreCase))
            .Select(unlock => unlock.BlueprintId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private CraftingRecipeDto ToRecipeDto(
        CraftingRecipeDefinition recipe,
        int targetTier,
        CharacterRecipeMastery? mastery,
        IReadOnlyDictionary<string, int> ownedByItemId,
        IReadOnlySet<string> unlockedBlueprintIds,
        IReadOnlyDictionary<string, EquipmentBase> itemBases,
        int craftingLevel)
    {
        var masteryProgress = CraftingMasteryProgression.GetProgressForExperience(
            mastery?.Experience ?? 0);
        var masteryLevel = masteryProgress.Level;
        var tier = Math.Clamp(targetTier, recipe.TierRange.Min, recipe.TierRange.Max);
        var costs = MapMaterialCosts(_requirementResolver.ResolveCosts(recipe, tier), ownedByItemId);
        itemBases.TryGetValue(recipe.OutputItemId, out var itemBase);
        var baseDesign = EquipmentCraftingDesignComposer.Compose(recipe, null);
        var blueprints = _definitions.GetBlueprints()
            .Where(blueprint =>
                blueprint.Enabled &&
                EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint))
            .OrderBy(blueprint => blueprint.Name)
            .Select(blueprint =>
            {
                var design = EquipmentCraftingDesignComposer.Compose(recipe, blueprint);
                var (primary, secondary, summary) = DescribeTemperingProfile(design.TemperingProfile);
                var blueprintCosts = MapMaterialCosts(
                    _requirementResolver.ResolveCosts(recipe, tier, design.AdditionalMaterialRequirements),
                    ownedByItemId);

                return new CraftingBlueprintDto
                {
                    Id = blueprint.Id,
                    ItemId = blueprint.ItemId,
                    Name = blueprint.Name,
                    Description = blueprint.Description,
                    CraftedItemName = design.Name,
                    IsLearned = unlockedBlueprintIds.Contains(blueprint.Id),
                    SourceType = blueprint.SourceType,
                    SourceId = blueprint.SourceId,
                    Behavior = design.Behavior,
                    InitialStatProfile = design.InitialStatProfile,
                    BonusStatProfile = blueprint.BonusStatProfile,
                    BonusStatBudgetMultiplier = blueprint.BonusStatBudgetMultiplier,
                    PrimaryTemperingStats = primary,
                    SecondaryTemperingStats = secondary,
                    TemperingProfileSummary = summary,
                    Tags = design.Tags,
                    MaterialCosts = blueprintCosts,
                    ItemPreview = itemBase == null
                        ? null
                        : BuildItemPreview(
                            itemBase,
                            design,
                            tier,
                            masteryLevel,
                            craftingLevel)
                };
            })
            .ToList();
        var (basePrimary, baseSecondary, baseSummary) = DescribeTemperingProfile(baseDesign.TemperingProfile);

        return new CraftingRecipeDto
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Icon = recipe.Icon,
            Category = recipe.Category,
            OutputItemId = recipe.OutputItemId,
            OutputItemType = recipe.OutputItemType,
            MinTier = recipe.TierRange.Min,
            MaxTier = recipe.TierRange.Max,
            CurrentMasteryLevel = masteryLevel,
            CurrentMasteryExperience = masteryProgress.Experience,
            MasteryExperienceRequiredForNextLevel = masteryProgress.ExperienceRequiredForNextLevel,
            MinimumProfessionLevel = recipe.MinimumProfessionLevel,
            Behavior = baseDesign.Behavior,
            InitialStatProfile = baseDesign.InitialStatProfile,
            PrimaryTemperingStats = basePrimary,
            SecondaryTemperingStats = baseSecondary,
            TemperingProfileSummary = baseSummary,
            AffinityTags = recipe.AffinityTags,
            Tags = baseDesign.Tags,
            MaterialCosts = costs,
            ItemPreview = itemBase == null
                ? null
                : BuildItemPreview(itemBase, baseDesign, tier, masteryLevel, craftingLevel),
            Blueprints = blueprints
        };
    }

    private CraftingItemPreviewDto BuildItemPreview(
        EquipmentBase itemBase,
        EquipmentCraftingDesign design,
        int tier,
        int masteryLevel,
        int craftingLevel)
    {
        var qualityChances = _qualityRollService.GetQualityChances(masteryLevel)
            .Where(x => x.Value > 0d)
            .OrderBy(x => x.Key)
            .ToList();
        var possibleQualities = qualityChances.Select(x => x.Key).ToList();
        var craftedRanges = _statRollService.GetBaseStatRanges(
                itemBase,
                design,
                tier,
                possibleQualities)
            .ToDictionary(x => x.AttributeType);
        var attributes = craftedRanges.Keys
            .OrderBy(x => x)
            .Select(attributeType =>
            {
                craftedRanges.TryGetValue(attributeType, out var crafted);
                return new CraftingAttributePreviewDto
                {
                    AttributeType = attributeType,
                    BaseAmount = 0,
                    MinimumCraftedAmount = crafted?.MinimumAmount ?? 0,
                    MaximumCraftedAmount = crafted?.MaximumAmount ?? 0
                };
            })
            .ToList();
        var potentialValues = possibleQualities
            .Select(quality => _potentialService.CalculateStartingPotential(
                itemBase,
                tier,
                quality,
                masteryLevel,
                craftingLevel))
            .ToList();

        return new CraftingItemPreviewDto
        {
            Name = design.Name,
            Description = string.IsNullOrWhiteSpace(itemBase.Description)
                ? design.Description
                : itemBase.Description,
            EquipmentType = itemBase.EquipmentType,
            Rarity = itemBase.Rarity,
            Tier = tier,
            RequiredLevel = EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(tier),
            StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion,
            Attributes = attributes,
            QualityChances = qualityChances
                .Select(x => new CraftingQualityChanceDto
                {
                    Quality = x.Key,
                    ChancePercent = x.Value
                })
                .ToList(),
            MinimumStartingPotential = potentialValues.Min(),
            MaximumStartingPotential = potentialValues.Max(),
        };
    }

    private static (IReadOnlyList<string> Primary, IReadOnlyList<string> Secondary, string Summary)
        DescribeTemperingProfile(TemperingProfileDefinition profile)
    {
        var primary = profile.Stats
            .Where(stat => stat.Category == TemperingStatCategory.Primary)
            .OrderByDescending(stat => stat.Weight)
            .Select(stat => stat.Stat.ToString())
            .ToList();
        var secondary = profile.Stats
            .Where(stat => stat.Category == TemperingStatCategory.Secondary)
            .OrderByDescending(stat => stat.Weight)
            .Select(stat => stat.Stat.ToString())
            .ToList();
        var summary = primary.Count == 0
            ? $"Can develop {string.Join(", ", secondary)}."
            : secondary.Count == 0
                ? $"Favors {string.Join(", ", primary)}."
                : $"Favors {string.Join(", ", primary)}. Can also develop {string.Join(", ", secondary)}.";
        return (primary, secondary, summary);
    }

    private IReadOnlyList<CraftingMaterialCostDto> MapMaterialCosts(
        IReadOnlyList<ResolvedMaterialCost> costs,
        IReadOnlyDictionary<string, int> ownedByItemId)
    {
        return _mapper.Map<IReadOnlyList<CraftingMaterialCostDto>>(costs, opt =>
            opt.Items["OwnedByItemId"] = ownedByItemId);
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

    private static OutboxEquipmentItemPayload ToOutboxEquipmentItem(EquipmentInstance item) =>
        new(
            item.ItemBaseId,
            item.Tier,
            item.Rarity,
            item.Quality,
            item.Potential,
            item.BaseRecipeId,
            item.BlueprintId,
            item.AffinityTags,
            item.IsMasterpiece);
}
