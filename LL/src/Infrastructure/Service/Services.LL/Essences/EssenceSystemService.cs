using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Prophecies;
using Application.UseCases.Outbox;
using Application.UseCases.Prophecies.Events;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Bonuses;
using Domain.Models.Combat.Abilities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;
using MediatR;
using Services.LL.Extensions;
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
    private readonly ICreatureEssenceLootTableRepository _creatureEssenceLootTables;
    private readonly IEssenceProgressionService _progression;
    private readonly IEssenceSlotUnlockService _slotUnlocks;
    private readonly IEssenceLoadoutLimitService _loadoutLimits;
    private readonly IInventoryItemFactory _inventoryItemFactory;
    private readonly IRandomProvider _random;
    private readonly IBonusService? _bonusService;
    private readonly ICreatureArchiveService? _creatureArchiveService;
    private readonly IPublisher? _publisher;
    private readonly IGameEventOutbox _outbox;
    private readonly Dictionary<Guid, string?> _essenceFocusCache = [];
    private readonly Dictionary<Guid, Dictionary<string, CreatureResonance>> _resonanceCache = [];
    private readonly Dictionary<string, ItemBase> _essenceItemBaseCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingEssenceItemBaseIds = new(StringComparer.OrdinalIgnoreCase);

    public EssenceSystemService(
        IEssenceRepository essences,
        IInventoryRepository inventory,
        IItemBaseRepository itemBases,
        IEssenceDefinitionRepository definitions,
        ICreatureEssenceLootTableRepository creatureEssenceLootTables,
        IEssenceProgressionService progression,
        IEssenceSlotUnlockService slotUnlocks,
        IEssenceLoadoutLimitService loadoutLimits,
        IInventoryItemFactory inventoryItemFactory,
        IRandomProvider random,
        IGameEventOutbox outbox,
        IPublisher? publisher = null,
        IBonusService? bonusService = null,
        ICreatureArchiveService? creatureArchiveService = null)
    {
        _essences = essences;
        _inventory = inventory;
        _itemBases = itemBases;
        _definitions = definitions;
        _creatureEssenceLootTables = creatureEssenceLootTables;
        _progression = progression;
        _slotUnlocks = slotUnlocks;
        _loadoutLimits = loadoutLimits;
        _inventoryItemFactory = inventoryItemFactory;
        _random = random;
        _bonusService = bonusService;
        _creatureArchiveService = creatureArchiveService;
        _publisher = publisher;
        _outbox = outbox;
    }

    public async Task<SoulArchive> GetSoulArchiveAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var defaultSlots = await GetDefaultSlotsAsync(characterId, cancellationToken);
        var dust = await GetInventoryQuantityAsync(characterId, EssenceDustItemId, cancellationToken);
        var entries = await _essences.GetPlayerEssencesAsync(characterId, cancellationToken);
        return new(entries.Select(x => new PlayerEssenceArchiveEntry(x, defaultSlots.FirstOrDefault(slot => slot.PlayerEssenceId == x.Id)?.SlotIndex)).ToList(), dust);
    }

    public async Task<EssenceLoadouts> GetLoadoutsAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var character = await _essences.GetCharacterWithEssenceLoadoutsAsync(characterId, cancellationToken);

        var loadouts = character is null
            ? []
            : EssenceLoadoutSelection.InArchiveOrder(character.EssenceLoadouts).ToList();

        return new(loadouts, _loadoutLimits.GetLoadoutLimit(characterId), _slotUnlocks.GetUnlockedSlotCount(character?.Level ?? 0));
    }

    public async Task<EssenceOperationResult> AbsorbUnboundEssenceAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken)
    {
        var inventoryItem = await GetInventoryItemAsync(characterId, inventoryItemId, cancellationToken);
        if (inventoryItem?.ItemInstance.ItemBase is not EssenceItemBase essenceItem)
            return Fail("The selected inventory item is not an Unbound Essence.");

        var definitionId = essenceItem.ResolveDefinitionId();
        var definition = _definitions.GetById(definitionId);
        if (definition is null) return Fail("The Essence definition no longer exists.");

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
                ProphecyProgressKind.EssenceAbsorbed)), cancellationToken);
        }

        await _outbox.EnqueueAsync(
            GameEventTypes.EssenceAbsorbed,
            new EssenceAbsorbedPayload(
                characterId,
                definitionId,
                archivedEssenceIds.Count,
                GetCompletedCollectionKeys(archivedEssenceIds)),
            characterId,
            null,
            cancellationToken);

        return Ok("Essence absorbed into the Soul Archive.");
    }

    public async Task<DismantleEssenceResult> DismantleUnboundEssenceAsync(
        Guid characterId,
        Guid inventoryItemId,
        CancellationToken cancellationToken,
        int quantity = 1)
    {
        if (quantity <= 0)
            return new(false, "The shatter quantity must be greater than zero.", 0);

        var inventoryItem = await GetInventoryItemAsync(characterId, inventoryItemId, cancellationToken);
        if (inventoryItem?.ItemInstance.ItemBase is not EssenceItemBase essenceItem)
            return new(false, "The selected inventory item is not an Unbound Essence.", 0);

        if (inventoryItem.Quantity < quantity)
            return new(false, "You do not have enough copies of the selected Essence.", 0);

        var dust = checked(Math.Max(1, essenceItem.DismantleDustAmount) * quantity);
        var definitionId = essenceItem.ResolveDefinitionId();
        if (!string.IsNullOrWhiteSpace(definitionId) &&
            await _essences.HasPlayerEssenceAsync(characterId, definitionId, cancellationToken))
        {
            dust = checked(dust + await RollDuplicateEchoBonusesAsync(characterId, quantity, cancellationToken));
        }

        ConsumeInventoryItem(inventoryItem, quantity);
        await AddInventoryQuantityAsync(characterId, EssenceDustItemId, dust, cancellationToken);
        var message = quantity == 1
            ? "Essence dismantled into Essence Dust."
            : $"{quantity} Essences dismantled into Essence Dust.";
        return new(true, message, dust);
    }

    public async Task<SpendEssenceDustResult> SpendEssenceDustAsync(Guid characterId, Guid playerEssenceId, int dustAmount, CancellationToken cancellationToken)
    {
        if (dustAmount <= 0) return new(false, "Dust amount must be greater than zero.", 0, 0, 0, false);

        var essence = await _essences.GetPlayerEssenceAsync(characterId, playerEssenceId, cancellationToken);
        if (essence is null) return new(false, "Absorbed Essence not found.", 0, 0, 0, false);

        var definition = _definitions.GetById(essence.EssenceDefinitionId);
        if (definition is null) return new(false, "Essence definition not found.", 0, 0, 0, false);

        var availableLevels = _progression.GetLevelCap(essence.AscensionTier) - essence.Level;
        if (availableLevels <= 0)
            return new(false, "This Essence is at its current Ascension level cap.", 0, 0, 0, true);

        var ownedDust = await GetInventoryQuantityAsync(characterId, EssenceDustItemId, cancellationToken);
        var dustToSpend = Math.Min(Math.Min(dustAmount, ownedDust), availableLevels);
        if (dustToSpend <= 0) return new(false, "Not enough Essence Dust.", 0, 0, 0, false);

        var xpGained = 0;
        var levelsGained = 0;
        for (var index = 0; index < dustToSpend; index++)
        {
            var xpToNextLevel = _progression.GetXpRequiredForNextLevel(essence, definition) - essence.CurrentXp;
            var result = _progression.GrantXp(essence, definition, xpToNextLevel);
            xpGained += result.XpGained;
            levelsGained += result.LevelsGained;
        }

        await RemoveInventoryQuantityAsync(characterId, EssenceDustItemId, levelsGained, cancellationToken);
        return new(
            true,
            "Essence Dust spent.",
            levelsGained,
            xpGained,
            levelsGained,
            essence.Level >= _progression.GetLevelCap(essence.AscensionTier));
    }

    public async Task<EssenceOperationResult> AscendEssenceAsync(Guid characterId, Guid playerEssenceId, CancellationToken cancellationToken)
    {
        var essence = await _essences.GetPlayerEssenceAsync(characterId, playerEssenceId, cancellationToken);
        if (essence is null) return Fail("Absorbed Essence not found.");
        if (essence.AscensionTier >= EssenceProgressionConstants.MaxAscensionTier) return Fail("Essence is already at the maximum Ascension Tier.");

        var nextTier = essence.AscensionTier + 1;
        var requirement = EssenceProgressionConstants.GetAscensionRequirement(nextTier);
        if (essence.Level < requirement.RequiredLevel)
            return Fail($"Essence must reach Level {requirement.RequiredLevel} before ascending.");

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

        await _outbox.EnqueueAsync(
            GameEventTypes.EssenceAscended,
            new EssenceAscendedPayload(characterId, nextTier, ascendedToTierCount),
            characterId,
            null,
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
        if (!await HasUniqueCreatureSourcesAsync(characterId, essenceIds, cancellationToken))
            throw new InvalidOperationException("A loadout cannot attune more than one Essence from the same creature.");

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
        await _outbox.EnqueueAsync(
            GameEventTypes.EssenceLoadoutChanged,
            new EssenceLoadoutChangedPayload(
                characterId,
                essenceIds,
                normalizedSlots.Count,
                await HasCompatibleEssenceTrioAsync(characterId, essenceIds, cancellationToken)),
            characterId,
            null,
            cancellationToken);

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

    public async Task<EssenceOperationResult> SetAutoUseActivitiesAsync(
        Guid characterId,
        Guid loadoutId,
        IReadOnlyCollection<EssenceCombatActivity> activities,
        CancellationToken cancellationToken)
    {
        if (activities.Any(activity => !EssenceLoadoutSelection.IsValidSingleActivity(activity)))
            return Fail("An unsupported combat activity was selected.");

        var requestedActivities = activities
            .Distinct()
            .Aggregate(EssenceCombatActivity.None, (current, activity) => current | activity);
        var loadouts = await _essences.GetLoadoutsWithSlotsAsync(characterId, cancellationToken);
        var selected = loadouts.FirstOrDefault(loadout => loadout.Id == loadoutId);
        if (selected is null) return Fail("Essence loadout not found.");

        foreach (var loadout in loadouts)
        {
            loadout.AutoUseActivities = loadout.Id == loadoutId
                ? requestedActivities
                : loadout.AutoUseActivities & ~requestedActivities;
            loadout.UpdatedAt = DateTimeOffset.UtcNow;
        }

        return Ok("Automatic Essence loadout use updated.");
    }

    private async Task<bool> HasCompatibleEssenceTrioAsync(
        Guid characterId,
        IReadOnlyCollection<Guid> playerEssenceIds,
        CancellationToken cancellationToken)
    {
        if (playerEssenceIds.Count < 3)
        {
            return false;
        }

        var selectedIds = playerEssenceIds.ToHashSet();
        var selectedEssences = (await _essences.GetPlayerEssencesAsync(characterId, cancellationToken))
            .Where(essence => selectedIds.Contains(essence.Id))
            .ToList();
        var compatibleTags = selectedEssences
            .SelectMany(essence =>
            {
                var definition = _definitions.GetById(essence.EssenceDefinitionId);
                if (definition is null)
                {
                    return Enumerable.Empty<(Guid EssenceId, string Tag)>();
                }

                return definition.ActiveAbility.Tags
                    .Concat(definition.PassiveAbility.Tags)
                    .Where(tag =>
                        !tag.Equals("Physical", StringComparison.OrdinalIgnoreCase) &&
                        !tag.Equals("Melee", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(tag => (EssenceId: essence.Id, Tag: tag));
            })
            .GroupBy(entry => entry.Tag, StringComparer.OrdinalIgnoreCase);

        return compatibleTags.Any(group => group.Select(entry => entry.EssenceId).Distinct().Count() >= 3);
    }

    private async Task<bool> HasUniqueCreatureSourcesAsync(
        Guid characterId,
        IReadOnlyCollection<Guid> playerEssenceIds,
        CancellationToken cancellationToken)
    {
        if (playerEssenceIds.Count < 2)
            return true;

        var selectedIds = playerEssenceIds.ToHashSet();
        var creatureIds = (await _essences.GetPlayerEssencesAsync(characterId, cancellationToken))
            .Where(essence => selectedIds.Contains(essence.Id))
            .Select(essence => _creatureEssenceLootTables
                .GetByEssenceDefinitionId(essence.EssenceDefinitionId)?
                .CreatureId)
            .Where(creatureId => !string.IsNullOrWhiteSpace(creatureId))
            .Cast<string>()
            .ToList();

        return creatureIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == creatureIds.Count;
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

    public Task GrantCombatXpToAttunedEssencesAsync(Guid characterId, int xp, CancellationToken cancellationToken) =>
        GrantCombatXpToAttunedEssencesAsync(characterId, xp, EssenceCombatActivity.None, cancellationToken);

    public async Task GrantCombatXpToAttunedEssencesAsync(
        Guid characterId,
        int xp,
        EssenceCombatActivity activity,
        CancellationToken cancellationToken)
    {
        var totalGranted = 0;
        var factors = await GetBonusFactorsAsync(characterId, DateTimeOffset.UtcNow, cancellationToken);
        var adjustedXp = xp.ApplyPositiveBps(factors.Get(BonusKind.EssenceExperienceGainBps));
        var loadout = EssenceLoadoutSelection.Select(
            await _essences.GetLoadoutsWithSlotsAsync(characterId, cancellationToken),
            activity);
        foreach (var slot in loadout?.Slots.Where(x => x.PlayerEssence is not null) ?? [])
        {
            var definition = _definitions.GetById(slot.PlayerEssence!.EssenceDefinitionId);
            if (definition is not null)
            {
                var result = _progression.GrantXp(slot.PlayerEssence, definition, adjustedXp);
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

    public Task<IReadOnlyList<AttributeModifierBase>> GetAttunedAttributeModifiersAsync(Guid characterId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AttributeModifierBase>>([]);

    public IReadOnlyList<AttributeModifierBase> GetAttunedAttributeModifiers(IEnumerable<PlayerEssence> essences) => [];

    public async Task<IReadOnlyList<AbilitySpec>> GetAttunedAbilitiesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var defaultSlots = await GetDefaultSlotsAsync(characterId, cancellationToken);
        return defaultSlots
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

    public Task<EssenceCombatLoadout> ResolveAsync(Guid characterId, CancellationToken cancellationToken) =>
        ResolveAsync(characterId, EssenceCombatActivity.None, cancellationToken);

    public async Task<EssenceCombatLoadout> ResolveAsync(
        Guid characterId,
        EssenceCombatActivity activity,
        CancellationToken cancellationToken)
    {
        var loadout = EssenceLoadoutSelection.Select(
            await _essences.GetLoadoutsWithSlotsAsync(characterId, cancellationToken),
            activity);
        var equippedEssences = loadout?.Slots
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Cast<PlayerEssence>()
            .ToList() ?? [];

        return Resolve(characterId, equippedEssences);
    }

    public EssenceCombatLoadout Resolve(Guid characterId, IEnumerable<PlayerEssence> equippedEssences)
    {
        var essences = equippedEssences.ToList();
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var essence in essences)
        {
            var definition = _definitions.GetById(essence.EssenceDefinitionId);
            if (definition is null) continue;

            foreach (var tag in GetEssenceTags(definition, essence))
                tags.Add(tag);
        }

        return new EssenceCombatLoadout(
            characterId,
            essences,
            [],
            tags);
    }

    public async Task<EssenceDropRollResult> RollMonsterEssenceDropAsync(
        Guid characterId,
        string monsterId,
        bool eligible,
        CancellationToken cancellationToken,
        EssenceDropRollModifiers? modifiers = null)
    {
        if (!eligible || _creatureEssenceLootTables.GetByCreatureId(monsterId) is null) return new(false, null, 0, 0);

        var factors = await GetBonusFactorsAsync(characterId, DateTimeOffset.UtcNow, cancellationToken);

        return await RollMonsterEssenceDropAsync(
            characterId,
            monsterId,
            eligible,
            factors,
            (candidateMonsterId, ct) => IsEssenceFocusAsync(characterId, candidateMonsterId, ct),
            cancellationToken,
            modifiers ?? new EssenceDropRollModifiers());
    }

    public async Task PrepareEssenceDropsAsync(
        Guid characterId,
        IReadOnlyList<Creature> defeatedCreatures,
        bool loadEssenceFocus,
        CancellationToken cancellationToken)
    {
        var monsterIds = defeatedCreatures
            .Select(CreatureEssenceSource.GetMonsterDefinitionId)
            .Where(monsterId => _creatureEssenceLootTables.GetByCreatureId(monsterId) is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (monsterIds.Length == 0)
        {
            return;
        }

        var resonances = await _essences.GetCreatureResonancesAsync(
            characterId,
            monsterIds,
            cancellationToken);
        var resonanceByCreature = GetOrCreateResonanceCache(characterId);
        foreach (var resonance in resonances)
        {
            resonanceByCreature[resonance.CreatureId] = resonance;
        }

        foreach (var monsterId in monsterIds.Where(id => !resonanceByCreature.ContainsKey(id)))
        {
            var resonance = new CreatureResonance
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                CreatureId = monsterId
            };
            await _essences.AddCreatureResonanceAsync(resonance, cancellationToken);
            resonanceByCreature[monsterId] = resonance;
        }

        if (loadEssenceFocus && !_essenceFocusCache.ContainsKey(characterId))
        {
            _essenceFocusCache[characterId] = _creatureArchiveService is null
                ? null
                : await _creatureArchiveService.GetEssenceFocusCreatureIdAsync(characterId, cancellationToken);
        }

        var possibleItemBaseIds = monsterIds
            .Select(_creatureEssenceLootTables.GetByCreatureId)
            .Where(table => table is not null)
            .SelectMany(table => table!.Variants)
            .Select(variant => $"item.{variant.EssenceDefinitionId}")
            .Where(itemBaseId => !_essenceItemBaseCache.ContainsKey(itemBaseId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var itemBases = await _itemBases.GetItemBasesByIdsAsync(possibleItemBaseIds, cancellationToken);
        foreach (var (itemBaseId, itemBase) in itemBases)
        {
            _essenceItemBaseCache[itemBaseId] = itemBase;
        }
        foreach (var missingItemBaseId in possibleItemBaseIds.Where(id => !itemBases.ContainsKey(id)))
        {
            _missingEssenceItemBaseIds.Add(missingItemBaseId);
        }
    }

    private async Task<EssenceDropRollResult> RollMonsterEssenceDropAsync(
        Guid characterId,
        string monsterId,
        bool eligible,
        IReadOnlyDictionary<BonusKind, double> factors,
        Func<string, CancellationToken, Task<bool>> isEssenceFocusAsync,
        CancellationToken cancellationToken,
        EssenceDropRollModifiers modifiers)
    {
        var lootTable = _creatureEssenceLootTables.GetByCreatureId(monsterId);
        if (!eligible || lootTable is null) return new(false, null, 0, 0);

        var resonanceByCreature = GetOrCreateResonanceCache(characterId);
        if (!resonanceByCreature.TryGetValue(monsterId, out var resonance))
        {
            resonance = await _essences.GetCreatureResonanceAsync(characterId, monsterId, cancellationToken);
        }

        if (resonance is null)
        {
            resonance = new CreatureResonance { Id = Guid.NewGuid(), CharacterId = characterId, CreatureId = monsterId };
            await _essences.AddCreatureResonanceAsync(resonance, cancellationToken);
        }
        resonanceByCreature[monsterId] = resonance;

        var maximumDropChanceBonus =
            CreatureResonanceConstants.MaximumDropChanceBonus * modifiers.ResonanceCapMultiplier;
        var bonus = Math.Min(
            maximumDropChanceBonus,
            resonance.ResonanceValue * CreatureResonanceConstants.DropChanceBonusPerPoint);
        var relativeDropRateBps = factors.Get(BonusKind.EssenceDropRateRelativeBps);
        if (factors.Get(BonusKind.FocusedMonsterEssenceDropRateRelativeBps) > 0 &&
            await isEssenceFocusAsync(monsterId, cancellationToken))
        {
            relativeDropRateBps += factors.Get(BonusKind.FocusedMonsterEssenceDropRateRelativeBps);
        }

        var pityProgressionGainBps = factors.Get(BonusKind.EssencePityProgressionGainBps);
        var effective = Math.Clamp(
            (lootTable.BaseDropChance + bonus).ApplyPositiveBps(relativeDropRateBps) *
            modifiers.DropChanceMultiplier,
            0,
            1);
        var dropped = _random.NextDouble() < effective;
        var essenceDefinitionId = dropped ? RollEssenceDefinitionId(lootTable) : null;
        if (dropped) resonance.ResonanceValue = 0;
        else resonance.ResonanceValue +=
            CreatureResonanceConstants.GainPerFailedEligibleKill.ApplyPositiveBps(pityProgressionGainBps) *
            modifiers.PityProgressionMultiplier;

        resonance.UpdatedAt = DateTimeOffset.UtcNow;
        return new(dropped, essenceDefinitionId, effective, resonance.ResonanceValue);
    }

    public async Task<IReadOnlyList<InventoryItem>> RollEssenceDropsAsync(
        Guid characterId,
        IReadOnlyList<Creature> defeatedCreatures,
        bool eligible,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<BonusKind, double>? bonusFactors = null,
        EssenceDropRollModifiers? modifiers = null)
    {
        var groups = await RollEssenceDropGroupsAsync(
            characterId,
            [defeatedCreatures],
            eligible,
            cancellationToken,
            bonusFactors,
            modifiers);
        return groups.Count == 0 ? [] : groups[0];
    }

    public async Task<IReadOnlyList<IReadOnlyList<InventoryItem>>> RollEssenceDropGroupsAsync(
        Guid characterId,
        IReadOnlyList<IReadOnlyList<Creature>> defeatedCreatureGroups,
        bool eligible,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<BonusKind, double>? bonusFactors = null,
        EssenceDropRollModifiers? modifiers = null)
    {
        if (defeatedCreatureGroups.Count == 0)
        {
            return [];
        }

        if (!eligible)
        {
            return defeatedCreatureGroups
                .Select(_ => (IReadOnlyList<InventoryItem>)Array.Empty<InventoryItem>())
                .ToArray();
        }

        var factors = bonusFactors ?? await GetBonusFactorsAsync(
            characterId,
            DateTimeOffset.UtcNow,
            cancellationToken);
        var rollModifiers = modifiers ?? new EssenceDropRollModifiers();
        var groups = new List<IReadOnlyList<InventoryItem>>(defeatedCreatureGroups.Count);
        foreach (var defeatedCreatures in defeatedCreatureGroups)
        {
            groups.Add(await RollEssenceDropGroupCoreAsync(
                characterId,
                defeatedCreatures,
                factors,
                rollModifiers,
                cancellationToken));
        }

        return groups;
    }

    private async Task<IReadOnlyList<InventoryItem>> RollEssenceDropGroupCoreAsync(
        Guid characterId,
        IReadOnlyList<Creature> defeatedCreatures,
        IReadOnlyDictionary<BonusKind, double> factors,
        EssenceDropRollModifiers rollModifiers,
        CancellationToken cancellationToken)
    {
        var drops = new List<InventoryItem>();
        if (defeatedCreatures.Count == 0) return drops;

        var monsterIds = defeatedCreatures
            .Select(CreatureEssenceSource.GetMonsterDefinitionId)
            .Where(monsterId => _creatureEssenceLootTables.GetByCreatureId(monsterId) is not null)
            .ToList();

        if (monsterIds.Count == 0) return drops;

        foreach (var monsterId in monsterIds)
        {
            var roll = await RollMonsterEssenceDropAsync(
                characterId,
                monsterId,
                true,
                factors,
                (candidateMonsterId, ct) => IsEssenceFocusAsync(characterId, candidateMonsterId, ct),
                cancellationToken,
                rollModifiers);
            if (!roll.Dropped || string.IsNullOrWhiteSpace(roll.EssenceDefinitionId)) continue;

            var itemBaseId = $"item.{roll.EssenceDefinitionId}";
            if (_missingEssenceItemBaseIds.Contains(itemBaseId)) continue;

            if (!_essenceItemBaseCache.TryGetValue(itemBaseId, out var itemBase))
            {
                var itemBases = await _itemBases.GetItemBasesByIdsAsync([itemBaseId], cancellationToken);
                if (!itemBases.TryGetValue(itemBaseId, out itemBase))
                {
                    _missingEssenceItemBaseIds.Add(itemBaseId);
                    continue;
                }
                _essenceItemBaseCache[itemBaseId] = itemBase;
            }

            drops.Add(_inventoryItemFactory.Create(itemBase, 1, characterId));
            if (await IsEssenceFocusAsync(characterId, monsterId, cancellationToken))
            {
                await _outbox.EnqueueAsync(
                    GameEventTypes.FocusedCreatureEssenceReceived,
                    new FocusedCreatureEssenceReceivedPayload(
                        characterId,
                        monsterId,
                        roll.EssenceDefinitionId),
                    characterId,
                    null,
                    cancellationToken);
            }
        }

        return drops;
    }

    private async Task<bool> IsEssenceFocusAsync(
        Guid characterId,
        string creatureId,
        CancellationToken cancellationToken)
    {
        if (_creatureArchiveService is null)
        {
            return false;
        }

        if (!_essenceFocusCache.TryGetValue(characterId, out var focusedCreatureId))
        {
            focusedCreatureId = await _creatureArchiveService.GetEssenceFocusCreatureIdAsync(
                characterId,
                cancellationToken);
            _essenceFocusCache[characterId] = focusedCreatureId;
        }

        return string.Equals(focusedCreatureId, creatureId, StringComparison.OrdinalIgnoreCase);
    }

    private Dictionary<string, CreatureResonance> GetOrCreateResonanceCache(Guid characterId)
    {
        if (!_resonanceCache.TryGetValue(characterId, out var resonanceByCreature))
        {
            resonanceByCreature = new Dictionary<string, CreatureResonance>(StringComparer.OrdinalIgnoreCase);
            _resonanceCache[characterId] = resonanceByCreature;
        }

        return resonanceByCreature;
    }

    private string RollEssenceDefinitionId(CreatureEssenceLootTableDefinition lootTable)
    {
        var totalWeight = lootTable.Variants.Sum(x => x.Weight);
        var roll = _random.NextDouble() * totalWeight;

        foreach (var variant in lootTable.Variants)
        {
            roll -= variant.Weight;
            if (roll < 0)
                return variant.EssenceDefinitionId;
        }

        return lootTable.Variants[^1].EssenceDefinitionId;
    }

    private async ValueTask<IReadOnlyDictionary<BonusKind, double>> GetBonusFactorsAsync(
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        _bonusService is null
            ? new Dictionary<BonusKind, double>()
            : await _bonusService.GetAggregatedAsync(characterId, now, cancellationToken);

    private async Task<IReadOnlyCollection<EssenceLoadoutSlot>> GetDefaultSlotsAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        EssenceLoadoutSelection.Select(
            await _essences.GetLoadoutsWithSlotsAsync(characterId, cancellationToken),
            EssenceCombatActivity.None)?.Slots.ToList() ?? [];

    private async Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken) =>
        await _inventory.GetInventoryItemAsync(characterId, inventoryItemId, cancellationToken);

    private async Task<int> GetInventoryQuantityAsync(Guid characterId, string itemBaseId, CancellationToken cancellationToken) =>
        await _inventory.GetInventoryQuantityAsync(characterId, itemBaseId, cancellationToken);

    private async Task<int> RollDuplicateEchoBonusesAsync(
        Guid characterId,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (_bonusService is null)
        {
            return 0;
        }

        var factors = await _bonusService.GetAggregatedAsync(characterId, DateTimeOffset.UtcNow, cancellationToken);
        var chanceBps = factors.Get(BonusKind.DuplicateEssenceExtraMaterialChanceBps);
        if (chanceBps <= 0) return 0;

        var chance = Math.Clamp(chanceBps, 0d, 10000d) / 10000d;
        var bonusDust = 0;
        for (var index = 0; index < quantity; index++)
        {
            if (_random.NextDouble() < chance) bonusDust++;
        }

        return bonusDust;
    }

    private async Task AddInventoryQuantityAsync(Guid characterId, string itemBaseId, int quantity, CancellationToken cancellationToken)
    {
        var itemBases = await _itemBases.GetItemBasesByIdsAsync([itemBaseId], cancellationToken);
        if (!itemBases.TryGetValue(itemBaseId, out var itemBase))
            throw new InvalidOperationException($"Item '{itemBaseId}' does not exist.");

        await _inventory.AddItemsToInventory(
            characterId,
            [_inventoryItemFactory.Create(itemBase, quantity, characterId)],
            ItemAcquisitionSources.EssenceSystem,
            cancellationToken);
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

    private static IEnumerable<string> GetEssenceTags(EssenceDefinition definition, PlayerEssence essence) =>
        definition.Tags.Concat(essence.IsEvolved ? definition.Evolution.AddsTags : []);

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
