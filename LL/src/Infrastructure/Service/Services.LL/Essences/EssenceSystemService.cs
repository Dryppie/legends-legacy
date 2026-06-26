using Application.Interfaces.Services.LL.Achievements;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Prophecies;
using Application.UseCases.Prophecies.Events;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using MediatR;
using Services.LL.Interfaces;

namespace Services.LL.Essences;

public sealed class EssenceSystemService : IEssenceService, IEssenceBonusProvider, IEssenceAbilityProvider, IEssenceCombatLoadoutResolver, IEssenceResonanceService
{
    private const string EssenceDustItemId = "soul_dust";
    private static readonly string[] CollectionAchievementTags = ["Beast"];
    private readonly IEssenceRepository _essences;
    private readonly IInventoryRepository _inventory;
    private readonly IItemBaseRepository _itemBases;
    private readonly IEssenceDefinitionRepository _definitions;
    private readonly IEssenceProgressionService _progression;
    private readonly IEssenceSlotUnlockService _slotUnlocks;
    private readonly IEssenceLoadoutLimitService _loadoutLimits;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IRandomProvider _random;
    private readonly IPublisher? _publisher;
    private readonly IAchievementService _achievementService;

    public EssenceSystemService(
        IEssenceRepository essences,
        IInventoryRepository inventory,
        IItemBaseRepository itemBases,
        IEssenceDefinitionRepository definitions,
        IEssenceProgressionService progression,
        IEssenceSlotUnlockService slotUnlocks,
        IEssenceLoadoutLimitService loadoutLimits,
        IInventoryItemFactory inventoryItemFactory,
        IRandomProvider random,
        IPublisher? publisher = null,
        IAchievementService achievementService)
    {
        _essences = essences;
        _inventory = inventory;
        _itemBases = itemBases;
        _definitions = definitions;
        _progression = progression;
        _slotUnlocks = slotUnlocks;
        _loadoutLimits = loadoutLimits;
        _inventoryItemFactory = inventoryItemFactory;
        _random = random;
        _publisher = publisher;
        _achievementService = achievementService;
    }

    public async Task<SoulArchive> GetSoulArchiveAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var activeSlots = await GetActiveSlotsAsync(characterId, cancellationToken);
        var dust = await GetInventoryQuantityAsync(characterId, EssenceDustItemId, cancellationToken);
        var entries = await _essences.GetPlayerEssencesAsync(characterId, cancellationToken);
        return new(entries.Select(x => new PlayerEssenceArchiveEntry(x, activeSlots.FirstOrDefault(slot => slot.PlayerEssenceId == x.Id)?.SlotIndex)).ToList(), dust);
    }

    public async Task<EssenceLoadouts> GetLoadoutsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _essences.GetCharacterWithEssenceLoadoutsAsync(characterId, cancellationToken);

        var loadouts = character?.EssenceLoadouts
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToList() ?? [];

        return new(loadouts, _loadoutLimits.GetLoadoutLimit(characterId), _slotUnlocks.GetUnlockedSlotCount(character?.Level ?? 0));
    }

    public async Task<EssenceLoadout?> GetActiveLoadoutAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var loadout = await _essences.GetActiveLoadoutAsync(characterId, cancellationToken);

        return loadout;
    }

    public async Task<EssenceOperationResult> AbsorbUnboundEssenceAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken)
    {
        var inventoryItem = await GetInventoryItemAsync(characterId, inventoryItemId, cancellationToken);
        if (inventoryItem?.ItemInstance.ItemBase is not EssenceItemBase essenceItem)
            return Fail("The selected inventory item is not an Unbound Essence.");

        var definitionId = ResolveDefinitionId(essenceItem);
        if (_definitions.GetById(definitionId) is null) return Fail("The Essence definition no longer exists.");

        var alreadyAbsorbed = await _essences.HasPlayerEssenceAsync(characterId, definitionId, cancellationToken);
        if (alreadyAbsorbed) return Fail("This Essence is already absorbed in the Soul Archive.");

        var existingEssences = await _essences.GetPlayerEssencesAsync(characterId, cancellationToken);
        var archivedEssenceIds = existingEssences
            .Select(x => x.EssenceDefinitionId)
            .Append(definitionId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await _essences.AddPlayerEssenceAsync(new PlayerEssence
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            EssenceDefinitionId = definitionId,
            Level = 1,
            AbsorbedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        ConsumeInventoryItem(inventoryItem, 1);
        if (_publisher is not null)
        {
            await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                characterId,
                DateTimeOffset.UtcNow,
                ProphecyProgressKind.EssenceArchived)), cancellationToken);
        }

        await _achievementService.RecordEssenceAbsorbedAsync(
            characterId,
            archivedEssenceIds.Count,
            GetCompletedCollectionKeys(archivedEssenceIds),
            cancellationToken);

        return Ok("Essence absorbed into the Soul Archive.");
    }

    public async Task<DismantleEssenceResult> DismantleUnboundEssenceAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken)
    {
        var inventoryItem = await GetInventoryItemAsync(characterId, inventoryItemId, cancellationToken);
        if (inventoryItem?.ItemInstance.ItemBase is not EssenceItemBase essenceItem)
            return new(false, "The selected inventory item is not an Unbound Essence.", 0);

        var dust = Math.Max(1, essenceItem.DismantleDustAmount);
        ConsumeInventoryItem(inventoryItem, 1);
        await AddInventoryQuantityAsync(characterId, EssenceDustItemId, dust, cancellationToken);
        return new(true, "Essence dismantled into Essence Dust.", dust);
    }

    public async Task<SpendEssenceDustResult> SpendEssenceDustAsync(Guid characterId, Guid playerEssenceId, int dustAmount, CancellationToken cancellationToken)
    {
        if (dustAmount <= 0) return new(false, "Dust amount must be greater than zero.", 0, 0, 0, false);

        var essence = await _essences.GetPlayerEssenceAsync(characterId, playerEssenceId, cancellationToken);
        if (essence is null) return new(false, "Absorbed Essence not found.", 0, 0, 0, false);

        var definition = _definitions.GetById(essence.EssenceDefinitionId);
        if (definition is null) return new(false, "Essence definition not found.", 0, 0, 0, false);

        var ownedDust = await GetInventoryQuantityAsync(characterId, EssenceDustItemId, cancellationToken);
        var dustToSpend = Math.Min(dustAmount, ownedDust);
        if (dustToSpend <= 0) return new(false, "Not enough Essence Dust.", 0, 0, 0, false);

        var result = _progression.GrantXp(essence, definition, dustToSpend * 25);
        var spent = (int)Math.Ceiling(result.XpGained / 25d);
        if (spent <= 0) return new(false, "This Essence is at its current Ascension Tier cap.", 0, 0, 0, true);

        await RemoveInventoryQuantityAsync(characterId, EssenceDustItemId, spent, cancellationToken);
        return new(true, "Essence Dust spent.", spent, result.XpGained, result.LevelsGained, result.ReachedTierCap);
    }

    public async Task<EssenceOperationResult> AscendEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken)
    {
        var essence = await _essences.GetPlayerEssenceAsync(characterId, playerEssenceId, cancellationToken);
        if (essence is null) return Fail("Absorbed Essence not found.");
        if (essence.AscensionTier >= 3) return Fail("Essence is already at the maximum Ascension Tier.");
        if (essence.Level < _progression.GetLevelCap(essence.AscensionTier)) return Fail("Essence must reach the current tier level cap before ascending.");

        var nextTier = essence.AscensionTier + 1;
        var ascensionCounts = await GetAscensionCountsAsync(characterId, cancellationToken);
        var cost = EssenceProgressionConstants.GetAscensionCost(
            nextTier,
            ascensionCounts.TierOneOrHigher,
            ascensionCounts.TierTwoOrHigher);

        if (!await RemoveInventoryQuantityAsync(characterId, cost.ItemId, cost.Amount, cancellationToken))
            return Fail($"Requires {cost.Amount} {FormatItemName(cost.ItemId)}.");

        essence.AscensionTier = nextTier;
        essence.UpdatedAt = DateTimeOffset.UtcNow;
        var ascendedToTierCount = nextTier switch
        {
            1 => ascensionCounts.TierOneOrHigher + 1,
            2 => ascensionCounts.TierTwoOrHigher + 1,
            3 => ascensionCounts.TierThreeOrHigher + 1,
            _ => 1
        };

        await _achievementService.RecordEssenceAscendedAsync(
            characterId,
            nextTier,
            ascendedToTierCount,
            cancellationToken);

        return Ok("Essence ascended.");
    }

    public async Task<EssenceOperationResult> EvolveEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken)
    {
        var essence = await _essences.GetPlayerEssenceAsync(characterId, playerEssenceId, cancellationToken);
        if (essence is null) return Fail("Absorbed Essence not found.");
        if (essence.IsEvolved) return Fail("Essence has already evolved.");

        var definition = _definitions.GetById(essence.EssenceDefinitionId);
        if (definition is null) return Fail("Essence definition not found.");
        if (essence.AscensionTier < definition.Evolution.RequiredAscensionTier) return Fail("Essence does not meet the required Ascension Tier.");
        if (!await RemoveInventoryQuantityAsync(characterId, definition.Evolution.RequiredCatalystItemId, 1, cancellationToken)) return Fail("Required Evolution Catalyst is missing.");

        essence.IsEvolved = true;
        essence.EvolutionUnlockedAt = DateTimeOffset.UtcNow;
        essence.UpdatedAt = DateTimeOffset.UtcNow;
        return Ok("Essence evolved.");
    }

    public async Task<EssenceLoadout> SaveLoadoutAsync(Guid characterId, SaveEssenceLoadoutRequest request, CancellationToken cancellationToken)
    {
        var characterLevel = await GetCharacterLevelAsync(characterId, cancellationToken);
        var unlockedSlots = _slotUnlocks.GetUnlockedSlotCount(characterLevel);
        var normalizedSlots = request.Slots.Where(x => x.PlayerEssenceId.HasValue).ToList();

        if (normalizedSlots.Any(x => x.SlotIndex < 0 || x.SlotIndex >= unlockedSlots))
            throw new InvalidOperationException("Loadout contains a locked Essence slot.");
        if (normalizedSlots.GroupBy(x => x.PlayerEssenceId).Any(x => x.Count() > 1))
            throw new InvalidOperationException("A loadout cannot attune the same Essence twice.");

        var essenceIds = normalizedSlots.Select(x => x.PlayerEssenceId!.Value).ToList();
        var ownedCount = await _essences.CountOwnedPlayerEssencesAsync(characterId, essenceIds, cancellationToken);
        if (ownedCount != essenceIds.Count) throw new InvalidOperationException("A loadout can only use absorbed Essences.");

        EssenceLoadout? loadout = null;
        if (request.Id.HasValue)
            loadout = await _essences.GetLoadoutAsync(characterId, request.Id.Value, cancellationToken);

        if (loadout is null)
        {
            var count = await _essences.CountLoadoutsAsync(characterId, cancellationToken);
            if (count >= _loadoutLimits.GetLoadoutLimit(characterId)) throw new InvalidOperationException("Essence loadout limit reached.");
            loadout = new EssenceLoadout { Id = Guid.NewGuid(), CharacterId = characterId, Name = request.Name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
            await _essences.AddLoadoutAsync(loadout, cancellationToken);
        }

        loadout.Name = request.Name.Trim();
        loadout.UpdatedAt = DateTimeOffset.UtcNow;
        await ReplaceLoadoutSlotsAsync(loadout, normalizedSlots, cancellationToken);
        if (loadout.IsActive)
        {
            await _achievementService.RecordEssenceLoadoutSavedAsync(characterId, normalizedSlots.Count, cancellationToken);
        }

        return loadout;
    }

    private async Task ReplaceLoadoutSlotsAsync(EssenceLoadout loadout, IReadOnlyCollection<SaveEssenceLoadoutSlotRequest> requestedSlots, CancellationToken cancellationToken)
    {
        var slots = requestedSlots
            .OrderBy(x => x.SlotIndex)
            .Select(slot => new EssenceLoadoutSlot
            {
                Id = Guid.NewGuid(),
                EssenceLoadoutId = loadout.Id,
                SlotIndex = slot.SlotIndex,
                PlayerEssenceId = slot.PlayerEssenceId
            })
            .ToList();

        await _essences.ReplaceLoadoutSlotsAsync(loadout.Id, slots, cancellationToken);
        loadout.Slots = slots;
    }

    public async Task<EssenceOperationResult> ActivateLoadoutAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken)
    {
        var loadouts = await _essences.GetLoadoutsWithSlotsAsync(characterId, cancellationToken);
        var selected = loadouts.FirstOrDefault(x => x.Id == loadoutId);
        if (selected is null) return Fail("Essence loadout not found.");

        var essenceIds = selected.Slots.Where(x => x.PlayerEssenceId.HasValue).Select(x => x.PlayerEssenceId!.Value).ToList();
        var ownedCount = await _essences.CountOwnedPlayerEssencesAsync(characterId, essenceIds, cancellationToken);
        if (ownedCount != essenceIds.Count) return Fail("Loadout references an Essence that is no longer absorbed.");

        foreach (var loadout in loadouts) loadout.IsActive = loadout.Id == loadoutId;
        await _achievementService.RecordEssenceLoadoutSavedAsync(characterId, selected.Slots.Count(x => x.PlayerEssenceId.HasValue), cancellationToken);
        return Ok("Essence loadout activated.");
    }

    public async Task<EssenceOperationResult> DeleteLoadoutAsync(Guid characterId, Guid loadoutId, CancellationToken cancellationToken)
    {
        var loadout = await _essences.GetLoadoutAsync(characterId, loadoutId, cancellationToken);
        if (loadout is null) return Fail("Essence loadout not found.");
        _essences.RemoveLoadout(loadout);
        return Ok("Essence loadout deleted.");
    }

    public async Task<EssenceOperationResult> SetFavoriteAsync(Guid characterId, Guid playerEssenceId, bool isFavorite, CancellationToken cancellationToken)
    {
        var essence = await _essences.GetPlayerEssenceAsync(characterId, playerEssenceId, cancellationToken);
        if (essence is null) return Fail("Absorbed Essence not found.");
        essence.IsFavorite = isFavorite;
        essence.UpdatedAt = DateTimeOffset.UtcNow;
        return Ok("Favorite updated.");
    }

    public async Task GrantCombatXpToAttunedEssencesAsync(Guid characterId, int xp, CancellationToken cancellationToken)
    {
        var totalGranted = 0;
        var activeSlots = await GetActiveSlotsAsync(characterId, cancellationToken);
        foreach (var slot in activeSlots.Where(x => x.PlayerEssence is not null))
        {
            var definition = _definitions.GetById(slot.PlayerEssence!.EssenceDefinitionId);
            if (definition is not null)
            {
                var result = _progression.GrantXp(slot.PlayerEssence, definition, xp);
                totalGranted += result.XpGained;
            }
        }

        if (totalGranted > 0 && _publisher is not null)
        {
            await _publisher.Publish(new ProphecyProgressNotification(new ProphecyProgressEvent(
                characterId,
                DateTimeOffset.UtcNow,
                ProphecyProgressKind.EssenceXpGained,
                totalGranted)), cancellationToken);
        }
    }

    public async Task<IReadOnlyList<AttributeModifierBase>> GetAttunedAttributeModifiersAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var loadout = await ResolveAsync(characterId, cancellationToken);
        return loadout.AttributeModifiers;
    }

    public IReadOnlyList<AttributeModifierBase> GetAttunedAttributeModifiers(IEnumerable<PlayerEssence> essences)
    {
        return Resolve(Guid.Empty, essences).AttributeModifiers;
    }

    public async Task<IReadOnlyList<AbilitySpec>> GetAttunedAbilitiesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var activeSlots = await GetActiveSlotsAsync(characterId, cancellationToken);
        return activeSlots
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Select(x => _definitions.GetById(x!.EssenceDefinitionId))
            .Where(x => x is not null)
            .SelectMany(x => new[] { x!.ActiveAbility, x.PassiveAbility })
            .ToList();
    }

    public IReadOnlyList<AbilitySpec> GetAttunedAbilities(IEnumerable<PlayerEssence> essences) =>
        essences
            .Select(x => _definitions.GetById(x.EssenceDefinitionId))
            .Where(x => x is not null)
            .SelectMany(x => new[] { x!.ActiveAbility, x.PassiveAbility })
            .ToList();

    public async Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var activeSlots = await GetActiveSlotsAsync(characterId, cancellationToken);
        var equippedEssences = activeSlots
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Cast<PlayerEssence>()
            .ToList();

        return Resolve(characterId, equippedEssences);
    }

    public EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences)
    {
        var essences = equippedEssences.ToList();
        var attributeModifiers = new List<AttributeModifierBase>();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var essence in essences)
        {
            var definition = _definitions.GetById(essence.EssenceDefinitionId);
            if (definition is null) continue;

            foreach (var tag in GetEssenceTags(definition, essence))
                tags.Add(tag);

            foreach (var bonus in GetAttributeBonusDefinitions(definition, essence))
            {
                attributeModifiers.Add(new EssenceAttributeModifier(
                    bonus.Attribute,
                    (float)GetAttributeBonusValue(bonus, essence),
                    bonus.ModifierKind == EssenceModifierKind.Percent
                        ? ModifierType.Additive
                        : ModifierType.Flat));
            }
        }

        return new EssenceCombatLoadout(
            characterId,
            essences,
            attributeModifiers,
            tags);
    }

    public async Task<EssenceDropRollResult> RollMonsterEssenceDropAsync(Guid characterId, string monsterId, bool eligible, CancellationToken cancellationToken)
    {
        var definition = _definitions.GetByMonsterId(monsterId);
        if (!eligible || definition is null) return new(false, null, 0, 0);

        var resonance = await _essences.GetMonsterResonanceAsync(characterId, monsterId, cancellationToken);
        if (resonance is null)
        {
            resonance = new CreatureResonance { Id = Guid.NewGuid(), CharacterId = characterId, CreatureId = monsterId };
            await _essences.AddMonsterResonanceAsync(resonance, cancellationToken);
        }

        var bonus = Math.Min(definition.Drop.MaxResonanceBonus, resonance.ResonanceValue * definition.Drop.DropChanceBonusPerResonance);
        var effective = Math.Clamp(definition.Drop.BaseDropChance + bonus, 0, 1);
        var dropped = _random.NextDouble() < effective;
        if (dropped) resonance.ResonanceValue = 0;
        else resonance.ResonanceValue += definition.Drop.ResonanceGainPerFailedEligibleKill;

        resonance.UpdatedAt = DateTimeOffset.UtcNow;
        return new(dropped, dropped ? definition.Id : null, effective, resonance.ResonanceValue);
    }

    public async Task<IReadOnlyList<InventoryItem>> RollEssenceDropsAsync(Guid characterId, IReadOnlyList<Creature> defeatedCreatures, bool eligible, CancellationToken cancellationToken)
    {
        var drops = new List<InventoryItem>();
        if (!eligible || defeatedCreatures.Count == 0) return drops;

        foreach (var creature in defeatedCreatures)
        {
            var monsterId = CreatureEssenceSource.GetMonsterDefinitionId(creature);
            var roll = await RollMonsterEssenceDropAsync(characterId, monsterId, true, cancellationToken);
            if (!roll.Dropped || string.IsNullOrWhiteSpace(roll.EssenceDefinitionId)) continue;

            var itemBaseId = $"item.{roll.EssenceDefinitionId}";
            var itemBases = await _itemBases.GetItemBasesByIdsAsync([itemBaseId], cancellationToken);
            if (!itemBases.TryGetValue(itemBaseId, out var itemBase)) continue;

            drops.Add(_inventoryItemFactory.Create(itemBase, 1, characterId));
        }

        return drops;
    }

    private async Task<List<EssenceLoadoutSlot>> GetActiveSlotsAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _essences.GetActiveSlotsAsync(characterId, cancellationToken);

    private async Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken) =>
        await _inventory.GetInventoryItemAsync(characterId, inventoryItemId, cancellationToken);

    private async Task<int> GetInventoryQuantityAsync(Guid characterId, string itemBaseId, CancellationToken cancellationToken) =>
        await _inventory.GetInventoryQuantityAsync(characterId, itemBaseId, cancellationToken);

    private async Task AddInventoryQuantityAsync(Guid characterId, string itemBaseId, int quantity, CancellationToken cancellationToken)
    {
        var itemBases = await _itemBases.GetItemBasesByIdsAsync([itemBaseId], cancellationToken);
        if (!itemBases.TryGetValue(itemBaseId, out var itemBase))
            throw new InvalidOperationException($"Item '{itemBaseId}' does not exist.");

        await _inventory.AddItemsToInventory(characterId, [_inventoryItemFactory.Create(itemBase, quantity, characterId)], cancellationToken);
    }

    private async Task<bool> RemoveInventoryQuantityAsync(Guid characterId, string itemBaseId, int quantity, CancellationToken cancellationToken)
    {
        return await _inventory.TryRemoveItemsByBaseIdAsync(characterId, new Dictionary<string, int> { [itemBaseId] = quantity }, cancellationToken);
    }

    private async Task<int> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _essences.GetCharacterLevelAsync(characterId, cancellationToken);

    private async Task<AscensionMilestoneCounts> GetAscensionCountsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var essences = await _essences.GetPlayerEssencesAsync(characterId, cancellationToken);
        return new AscensionMilestoneCounts(
            essences.Count(x => x.AscensionTier >= 1),
            essences.Count(x => x.AscensionTier >= 2),
            essences.Count(x => x.AscensionTier >= 3));
    }

    private IReadOnlyCollection<string> GetCompletedCollectionKeys(IReadOnlyCollection<string> archivedEssenceIds)
    {
        var archived = archivedEssenceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return CollectionAchievementTags
            .Where(tag =>
            {
                var collectionIds = _definitions.GetAll()
                    .Where(definition => definition.Tags.Any(definitionTag => MatchesCollectionTag(definitionTag, tag)))
                    .Select(definition => definition.Id)
                    .ToList();

                return collectionIds.Count > 0 && collectionIds.All(archived.Contains);
            })
            .ToList();
    }

    private static bool MatchesCollectionTag(string definitionTag, string collectionKey) =>
        definitionTag.Equals(collectionKey, StringComparison.OrdinalIgnoreCase) ||
        definitionTag.EndsWith($".{collectionKey}", StringComparison.OrdinalIgnoreCase);

    private void ConsumeInventoryItem(InventoryItem inventoryItem, int quantity)
    {
        inventoryItem.Quantity -= quantity;
        if (inventoryItem.Quantity <= 0) _inventory.RemoveInventoryItem(inventoryItem);
    }

    private string ResolveDefinitionId(EssenceItemBase essenceItem)
    {
        if (!string.IsNullOrWhiteSpace(essenceItem.EssenceDefinitionId)) return essenceItem.EssenceDefinitionId;
        return InferDefinitionIdFromItemBaseId(essenceItem.Id);
    }

    private static string InferDefinitionIdFromItemBaseId(string itemBaseId)
    {
        const string itemPrefix = "item.";
        return itemBaseId.StartsWith(itemPrefix, StringComparison.OrdinalIgnoreCase)
            ? itemBaseId[itemPrefix.Length..]
            : string.Empty;
    }

    private static IEnumerable<EssenceAttributeBonusDefinition> GetAttributeBonusDefinitions(EssenceDefinition definition, PlayerEssence essence) =>
        definition.AttributeBonuses.Concat(essence.IsEvolved ? definition.Evolution.AttributeModifierChanges : []);

    private static IEnumerable<string> GetEssenceTags(EssenceDefinition definition, PlayerEssence essence) =>
        definition.Tags.Concat(essence.IsEvolved ? definition.Evolution.AddsTags : []);

    private static double GetAttributeBonusValue(EssenceAttributeBonusDefinition bonus, PlayerEssence essence) =>
        EssenceProgressionConstants.ScaleAttributeBonus(bonus.BaseValue, essence.Level);

    private static EssenceOperationResult Ok(string message) => new(true, message);
    private static EssenceOperationResult Fail(string message) => new(false, message);

    private static string FormatItemName(string itemId)
    {
        if (itemId.Equals(EssenceProgressionConstants.LesserMonsterCoreItemId, StringComparison.OrdinalIgnoreCase))
            return "Lesser Monster Core";
        if (itemId.Equals(EssenceProgressionConstants.GreaterMonsterCoreItemId, StringComparison.OrdinalIgnoreCase))
            return "Greater Monster Core";
        if (itemId.Equals(EssenceProgressionConstants.PrimalMonsterCoreItemId, StringComparison.OrdinalIgnoreCase))
            return "Primal Monster Core";

        var parts = itemId
            .Replace("item.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(x => x.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(x => x.Length == 0 ? x : char.ToUpperInvariant(x[0]) + x[1..]);

        return string.Join(' ', parts);
    }

    private sealed record AscensionMilestoneCounts(int TierOneOrHigher, int TierTwoOrHigher, int TierThreeOrHigher);
}
