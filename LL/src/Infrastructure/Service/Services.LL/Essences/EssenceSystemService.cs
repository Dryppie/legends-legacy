using Application.Interfaces.Services.LL.Essences;
using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities;
using Domain.Models.AbilityDefinitions;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.Intervals;
using Domain.Models.Combat.Abilities.Effects.Trigger;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Combat.Abilities.ResourceCosts;
using Domain.Models.Combat.Abilities.Triggers;
using Domain.Models.Combat.Abilities.Triggers.TriggerFilters;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Damages;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Essences.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.EssenceItems;

namespace Services.LL.Essences;

public sealed class EssenceSystemService : IEssenceService, IEssenceBonusProvider, IEssenceAbilityProvider, IEssenceResonanceService
{
    private const string EssenceDustItemId = "soul_dust";
    private readonly IEssenceRepository _essences;
    private readonly IInventoryRepository _inventory;
    private readonly IItemBaseRepository _itemBases;
    private readonly IEssenceDefinitionRepository _definitions;
    private readonly IEssenceProgressionService _progression;
    private readonly IEssenceSlotUnlockService _slotUnlocks;
    private readonly IEssenceLoadoutLimitService _loadoutLimits;
    private readonly IRandomProvider _random;

    public EssenceSystemService(
        IEssenceRepository essences,
        IInventoryRepository inventory,
        IItemBaseRepository itemBases,
        IEssenceDefinitionRepository definitions,
        IEssenceProgressionService progression,
        IEssenceSlotUnlockService slotUnlocks,
        IEssenceLoadoutLimitService loadoutLimits,
        IRandomProvider random)
    {
        _essences = essences;
        _inventory = inventory;
        _itemBases = itemBases;
        _definitions = definitions;
        _progression = progression;
        _slotUnlocks = slotUnlocks;
        _loadoutLimits = loadoutLimits;
        _random = random;
    }

    public Task<EssenceCatalog> GetCatalogAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new EssenceCatalog(_definitions.GetAll().ToList(), EssenceTagCatalog.TagsByCategory));

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
        var definition = _definitions.GetById(essence.EssenceDefinitionId);
        var coreItemId = definition?.Ascension.Tiers.FirstOrDefault(x => x.Tier == nextTier)?.RequiredCoreItemId ?? $"item.monster_core.tier_{nextTier}";
        if (!await RemoveInventoryQuantityAsync(characterId, coreItemId, 1, cancellationToken)) return Fail("Required Monster Core is missing.");

        essence.AscensionTier = nextTier;
        essence.UpdatedAt = DateTimeOffset.UtcNow;
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
            loadout = await _essences.GetLoadoutWithSlotsAsync(characterId, request.Id.Value, cancellationToken);

        if (loadout is null)
        {
            var count = await _essences.CountLoadoutsAsync(characterId, cancellationToken);
            if (count >= _loadoutLimits.GetLoadoutLimit(characterId)) throw new InvalidOperationException("Essence loadout limit reached.");
            loadout = new EssenceLoadout { Id = Guid.NewGuid(), CharacterId = characterId, Name = request.Name.Trim(), CreatedAt = DateTimeOffset.UtcNow };
            await _essences.AddLoadoutAsync(loadout, cancellationToken);
        }

        loadout.Name = request.Name.Trim();
        loadout.UpdatedAt = DateTimeOffset.UtcNow;
        loadout.Slots.Clear();
        foreach (var slot in request.Slots.OrderBy(x => x.SlotIndex))
        {
            loadout.Slots.Add(new EssenceLoadoutSlot { Id = Guid.NewGuid(), EssenceLoadoutId = loadout.Id, SlotIndex = slot.SlotIndex, PlayerEssenceId = slot.PlayerEssenceId });
        }

        return loadout;
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
        var activeSlots = await GetActiveSlotsAsync(characterId, cancellationToken);
        foreach (var slot in activeSlots.Where(x => x.PlayerEssence is not null))
        {
            var definition = _definitions.GetById(slot.PlayerEssence!.EssenceDefinitionId);
            if (definition is not null) _progression.GrantXp(slot.PlayerEssence, definition, xp);
        }
    }

    public async Task<IReadOnlyList<AttributeModifierBase>> GetAttunedAttributeModifiersAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var activeSlots = await GetActiveSlotsAsync(characterId, cancellationToken);
        return GetAttunedAttributeModifiers(activeSlots
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Cast<PlayerEssence>());
    }

    public IReadOnlyList<AttributeModifierBase> GetAttunedAttributeModifiers(IEnumerable<PlayerEssence> essences)
    {
        var result = new List<AttributeModifierBase>();
        foreach (var essence in essences)
        {
            var definition = _definitions.GetById(essence.EssenceDefinitionId);
            if (definition is null) continue;

            foreach (var bonus in GetAttributeBonusDefinitions(definition, essence))
            {
                result.Add(new EssenceAttributeModifier(
                    bonus.Attribute,
                    (float)GetAttributeBonusValue(bonus, essence),
                    bonus.ModifierKind == EssenceModifierKind.Percent
                        ? ModifierType.Additive
                        : ModifierType.Flat));
            }
        }
        return result;
    }

    public async Task<IReadOnlyList<AbilityDefinition>> GetAttunedAbilitiesAsync(Guid characterId, CancellationToken cancellationToken)
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

    public async Task<IReadOnlyList<CombatAbilityInstance>> GetAttunedCombatAbilitiesAsync(Guid characterId, CancellationToken cancellationToken)
    {
        var activeSlots = await GetActiveSlotsAsync(characterId, cancellationToken);
        return GetAttunedCombatAbilities(activeSlots
            .Select(x => x.PlayerEssence)
            .Where(x => x is not null)
            .Cast<PlayerEssence>());
    }

    public IReadOnlyList<CombatAbilityInstance> GetAttunedCombatAbilities(IEnumerable<PlayerEssence> essences)
    {
        var abilities = new List<CombatAbilityInstance>();
        foreach (var essence in essences)
        {
            var definition = _definitions.GetById(essence.EssenceDefinitionId);
            if (definition is null) continue;

            if (!string.IsNullOrWhiteSpace(definition.ActiveAbility.Id))
                abilities.Add(CreateCombatAbilityInstance(
                    MapCombatAbility(ApplyEvolutionModifiers(definition.ActiveAbility, definition.Evolution.ActiveAbilityModifiers, essence), essence, CombatAbilityType.Active)));

            if (!string.IsNullOrWhiteSpace(definition.PassiveAbility.Id))
                abilities.Add(CreateCombatAbilityInstance(
                    MapCombatAbility(ApplyEvolutionModifiers(definition.PassiveAbility, definition.Evolution.PassiveAbilityModifiers, essence), essence, CombatAbilityType.Passive)));
        }

        return abilities;
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
            var monsterId = GetMonsterDefinitionId(creature);
            var roll = await RollMonsterEssenceDropAsync(characterId, monsterId, true, cancellationToken);
            if (!roll.Dropped || string.IsNullOrWhiteSpace(roll.EssenceDefinitionId)) continue;

            var itemBaseId = $"item.{roll.EssenceDefinitionId}";
            var itemBases = await _itemBases.GetItemBasesByIdsAsync([itemBaseId], cancellationToken);
            if (!itemBases.TryGetValue(itemBaseId, out var itemBase)) continue;

            var itemInstance = new EssenceItemInstance
            {
                Id = Guid.NewGuid(),
                ItemBaseId = itemBase.Id,
                ItemBase = itemBase
            };

            drops.Add(new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = itemInstance.Id,
                ItemInstance = itemInstance,
                Quantity = 1
            });
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

        var instance = new ItemInstance { Id = Guid.NewGuid(), ItemBaseId = itemBase.Id, ItemBase = itemBase };
        await _inventory.AddItemsToInventory(characterId, [new InventoryItem { InventoryId = characterId, ItemInstanceId = instance.Id, ItemInstance = instance, Quantity = quantity }], cancellationToken);
    }

    private async Task<bool> RemoveInventoryQuantityAsync(Guid characterId, string itemBaseId, int quantity, CancellationToken cancellationToken)
    {
        return await _inventory.TryRemoveItemsByBaseIdAsync(characterId, new Dictionary<string, int> { [itemBaseId] = quantity }, cancellationToken);
    }

    private async Task<int> GetCharacterLevelAsync(Guid characterId, CancellationToken cancellationToken) =>
        await _essences.GetCharacterLevelAsync(characterId, cancellationToken);

    private void ConsumeInventoryItem(InventoryItem inventoryItem, int quantity)
    {
        inventoryItem.Quantity -= quantity;
        if (inventoryItem.Quantity <= 0) _inventory.RemoveInventoryItem(inventoryItem);
    }

    private string ResolveDefinitionId(EssenceItemBase essenceItem)
    {
        if (!string.IsNullOrWhiteSpace(essenceItem.EssenceDefinitionId)) return essenceItem.EssenceDefinitionId;
        return string.Empty;
    }

    private static IEnumerable<EssenceAttributeBonusDefinition> GetAttributeBonusDefinitions(EssenceDefinition definition, PlayerEssence essence) =>
        definition.AttributeBonuses.Concat(essence.IsEvolved ? definition.Evolution.AttributeModifierChanges : []);

    private static double GetAttributeBonusValue(EssenceAttributeBonusDefinition bonus, PlayerEssence essence) =>
        bonus.BaseValue + bonus.PerLevel * Math.Max(0, essence.Level - 1) + bonus.PerAscensionTier * essence.AscensionTier;

    private CombatAbilityDefinition MapCombatAbility(AbilityDefinition ability, PlayerEssence essence, CombatAbilityType type)
    {
        var combatAbility = new CombatAbilityDefinition
        {
            Id = ability.Id,
            Name = ability.Name,
            Description = ability.Description,
            Type = type,
            Cooldown = SecondsToCombatTicks(ability.CooldownSeconds),
            Usage = new UnlimitedUsage(),
            Condition = BuildCondition(ability.Conditions)
        };

        var triggers = type == CombatAbilityType.Active
            ? [new AbilityTriggerDefinition { Type = "OnAbilityUsed" }]
            : ability.Triggers.Count == 0
                ? [new AbilityTriggerDefinition { Type = "OnCombatStart" }]
                : ability.Triggers;

        foreach (var trigger in triggers)
        {
            var combatTrigger = new Trigger
            {
                Event = MapTrigger(trigger.Type),
                Actions = [.. ability.Effects.Select(effect => MapCombatEffect(ability, effect, essence))]
            };

            if (type == CombatAbilityType.Active && combatTrigger.Event == TriggerEvent.OnAbilityUsed)
                combatTrigger.Filters.Add(new AbilityIdFilter { AllowedIds = [ability.Id] });

            combatAbility.Triggers.Add(combatTrigger);
        }

        return combatAbility;
    }

    private static CombatAbilityInstance CreateCombatAbilityInstance(CombatAbilityDefinition definition)
    {
        var instance = new CombatAbilityInstance(definition);
        if (definition.Type == CombatAbilityType.Passive) instance.RemainingTimeUntilUse = 0;
        return instance;
    }

    private static AbilityDefinition ApplyEvolutionModifiers(
        AbilityDefinition ability,
        IReadOnlyCollection<AbilityModifierDefinition> modifiers,
        PlayerEssence essence)
    {
        if (!essence.IsEvolved || modifiers.Count == 0) return ability;

        var copy = new AbilityDefinition
        {
            Id = ability.Id,
            Name = ability.Name,
            Description = ability.Description,
            CooldownSeconds = ability.CooldownSeconds,
            Kind = ability.Kind,
            Targeting = ability.Targeting,
            Tags = [.. ability.Tags],
            Triggers = [.. ability.Triggers.Select(x => new AbilityTriggerDefinition { Type = x.Type, InternalCooldownSeconds = x.InternalCooldownSeconds })],
            Conditions = [.. ability.Conditions.Select(CloneCondition)],
            Effects = [.. ability.Effects.Select(CloneEffect)]
        };

        foreach (var modifier in modifiers)
        {
            if (modifier.Operation.Equals("AddEffect", StringComparison.OrdinalIgnoreCase) && modifier.Effect is not null)
            {
                copy.Effects.Add(CloneEffect(modifier.Effect));
                continue;
            }

            var effect = copy.Effects.FirstOrDefault(x => x.Id.Equals(modifier.Target, StringComparison.OrdinalIgnoreCase));
            if (effect is null) continue;

            if (modifier.Operation.Equals("AddMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                var multiplier = 1 + modifier.Value;
                effect.Scaling.BaseValue *= multiplier;
                effect.Scaling.PerLevel *= multiplier;
                effect.Scaling.PerAscensionTier *= multiplier;
            }
            else if (modifier.Operation.Equals("AddFlat", StringComparison.OrdinalIgnoreCase))
            {
                effect.Scaling.BaseValue += modifier.Value;
            }
        }

        return copy;
    }

    private static AbilityEffectDefinition CloneEffect(AbilityEffectDefinition effect) =>
        new()
        {
            Id = effect.Id,
            Type = effect.Type,
            Target = effect.Target,
            Attribute = effect.Attribute,
            Status = effect.Status,
            DurationSeconds = effect.DurationSeconds,
            Conditions = [.. effect.Conditions.Select(CloneCondition)],
            Scaling = new AbilityScalingFormula
            {
                BaseValue = effect.Scaling.BaseValue,
                PerLevel = effect.Scaling.PerLevel,
                PerAscensionTier = effect.Scaling.PerAscensionTier,
                AttributeScaling = [.. effect.Scaling.AttributeScaling.Select(x => new AbilityAttributeScalingDefinition { Attribute = x.Attribute, Coefficient = x.Coefficient })]
            }
        };

    private static AbilityConditionDefinition CloneCondition(AbilityConditionDefinition condition) =>
        new()
        {
            Type = condition.Type,
            Tag = condition.Tag,
            Status = condition.Status,
            Value = condition.Value
        };

    private EffectDefinition MapCombatEffect(AbilityDefinition ability, AbilityEffectDefinition effect, PlayerEssence essence)
    {
        var magnitude = Scale(effect.Scaling, essence);
        var action = BuildAction(effect, magnitude);
        IEffectDuration duration = effect.DurationSeconds is > 0
            ? new TimedDuration(SecondsToCombatTicks(effect.DurationSeconds.Value))
            : new NoDuration();
        var combatEffect = new EffectDefinition(
            action,
            duration,
            BuildCondition(effect.Conditions.Count == 0 ? ability.Conditions : effect.Conditions),
            new NoInterval(),
            new UnlimitedUsage(),
            effectTags: [],
            effectModifications: [],
            targeting: MapTargeting(string.IsNullOrWhiteSpace(effect.Target) ? ability.Targeting : effect.Target),
            attackType: action is DamageAction ? AttackType.Melee : AttackType.None,
            damageType: action is DamageAction ? DamageType.Magical : DamageType.None,
            chance: BuildChance(effect.Conditions.Count == 0 ? ability.Conditions : effect.Conditions))
        {
            Log = BuildEffectLog(effect.Type),
            SourceName = ability.Name
        };

        return combatEffect;
    }

    private static IEffectAction BuildAction(AbilityEffectDefinition effect, int magnitude) =>
        effect.Type switch
        {
            AbilityEffectType.Damage => new DamageAction(magnitude, FirstScalingAttribute(effect), FirstScalingCoefficient(effect)),
            AbilityEffectType.Heal => new ResourceRestoreAction(magnitude, ResourceType.Health, FirstScalingAttribute(effect), FirstScalingCoefficient(effect)),
            AbilityEffectType.GrantBarrier => new ResourceRestoreAction(magnitude, ResourceType.Barrier, FirstScalingAttribute(effect), FirstScalingCoefficient(effect)),
            AbilityEffectType.RemoveStatus => new RemoveStatusAction(effect.Status ?? string.Empty),
            AbilityEffectType.Cleanse => new CleanseAction(),
            AbilityEffectType.Summon => new SummonAction(effect.Status ?? effect.Attribute ?? effect.Id, Math.Max(0, (int)(effect.DurationSeconds ?? 0))),
            AbilityEffectType.Taunt => new ModifyAttributeAction(
                new AbilityAttributeModifier(AttributeType.Fortitude, magnitude, ModifierType.Flat),
                stackable: false),
            AbilityEffectType.ReflectDamage => new DamageAction(magnitude, FirstScalingAttribute(effect), FirstScalingCoefficient(effect)),
            AbilityEffectType.AbsorbDamage => new ResourceRestoreAction(magnitude, ResourceType.Barrier, FirstScalingAttribute(effect), FirstScalingCoefficient(effect)),
            AbilityEffectType.TriggerSecondaryEffect => new TriggerSecondaryEffectAction(effect.Status ?? effect.Id),
            AbilityEffectType.RestoreResource when effect.Attribute?.Equals(nameof(AttributeType.Cooldown), StringComparison.OrdinalIgnoreCase) == true =>
                new ModifyAttributeAction(
                    new AbilityAttributeModifier(AttributeType.Cooldown, magnitude, ModifierType.Flat),
                    stackable: false),
            AbilityEffectType.RestoreResource when effect.Attribute?.Equals(nameof(AttributeType.MaxHealth), StringComparison.OrdinalIgnoreCase) == true =>
                new ResourceRestoreAction(magnitude, ResourceType.Health, FirstScalingAttribute(effect), FirstScalingCoefficient(effect)),
            AbilityEffectType.RestoreResource =>
                new ModifyAttributeAction(
                    new AbilityAttributeModifier(AttributeType.Cooldown, magnitude, ModifierType.Flat),
                    stackable: false),
            AbilityEffectType.ModifyAttribute => new ModifyAttributeAction(
                new AbilityAttributeModifier(ParseAttribute(effect.Attribute), magnitude, ModifierType.Flat),
                stackable: false),
            AbilityEffectType.ApplyStatus when !string.IsNullOrWhiteSpace(effect.Status) => new ApplyStatusAction(effect.Status),
            _ => throw new NotSupportedException($"Essence effect type '{effect.Type}' is not supported by combat mapping.")
        };

    private static ICondition BuildCondition(IReadOnlyCollection<AbilityConditionDefinition> conditions)
    {
        var mapped = conditions
            .Select(MapCondition)
            .Where(x => x is not null)
            .Cast<ICondition>()
            .ToList();

        return mapped.Count switch
        {
            0 => new NoCondition(),
            1 => mapped[0],
            _ => new AllConditions(mapped)
        };
    }

    private static ICondition? MapCondition(AbilityConditionDefinition condition) =>
        condition.Type switch
        {
            AbilityConditionType.TargetHealthBelowPercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: false, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            "HealthBelowPercent" when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: false, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            AbilityConditionType.SourceHealthBelowPercent when condition.Value is > 0 =>
                new CombatantHealthCondition(useSource: true, (int)Math.Round(condition.Value.Value), ComparisonType.LessThan),
            AbilityConditionType.TargetHasStatus when !string.IsNullOrWhiteSpace(condition.Status) =>
                new CombatantStatusCondition(useSource: false, condition.Status),
            AbilityConditionType.SourceHasStatus when !string.IsNullOrWhiteSpace(condition.Status) =>
                new CombatantStatusCondition(useSource: true, condition.Status),
            AbilityConditionType.RandomChance => null,
            AbilityConditionType.CooldownReady => null,
            AbilityConditionType.SourceHasTag when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: true, condition.Tag),
            AbilityConditionType.TargetHasTag when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: false, condition.Tag),
            AbilityConditionType.IsSpecies when !string.IsNullOrWhiteSpace(condition.Tag) =>
                new CombatantTagCondition(useSource: false, NormalizeSpeciesTag(condition.Tag)),
            _ => null
        };

    private static string NormalizeSpeciesTag(string tag) =>
        tag.StartsWith("Species.", StringComparison.OrdinalIgnoreCase) ? tag : $"Species.{tag}";

    private static int BuildChance(IReadOnlyCollection<AbilityConditionDefinition> conditions)
    {
        var chance = conditions.FirstOrDefault(x => x.Type.Equals("RandomChance", StringComparison.OrdinalIgnoreCase));
        return chance?.Value is > 0 ? Math.Clamp((int)Math.Round(chance.Value.Value), 1, 100) : 100;
    }

    private static TriggerEvent MapTrigger(string trigger)
    {
        var normalized = trigger.StartsWith("Trigger.", StringComparison.OrdinalIgnoreCase)
            ? trigger["Trigger.".Length..]
            : trigger;

        return normalized switch
        {
            "OnCombatStart" => TriggerEvent.OnCombatStart,
            "OnAbilityUsed" => TriggerEvent.OnAbilityUsed,
            AbilityTriggerType.OnAbilityUse => TriggerEvent.OnAbilityUsed,
            "OnHit" => TriggerEvent.OnAttack,
            "OnCrit" => TriggerEvent.OnCriticalHit,
            "OnTakeDamage" => TriggerEvent.OnDamaged,
            "OnKill" => TriggerEvent.OnKill,
            "OnDodge" => TriggerEvent.OnDodge,
            _ => throw new NotSupportedException($"Essence trigger '{trigger}' is not supported by combat mapping.")
        };
    }

    private static CombatTargeting MapTargeting(string target) =>
        target switch
        {
            AbilityTargetSelector.Self => CombatTargeting.Self,
            AbilityTargetSelector.CurrentTarget => CombatTargeting.SingleEnemy,
            AbilityTargetSelector.RandomEnemy => CombatTargeting.SingleEnemy,
            AbilityTargetSelector.LowestHealthEnemy => CombatTargeting.SingleEnemy,
            AbilityTargetSelector.HighestHealthEnemy => CombatTargeting.SingleEnemy,
            AbilityTargetSelector.LowestHealthAlly => CombatTargeting.SingleAlly,
            AbilityTargetSelector.RandomAlly => CombatTargeting.SingleAlly,
            AbilityTargetSelector.AllEnemies => CombatTargeting.AllEnemies,
            AbilityTargetSelector.AllAllies => CombatTargeting.AllAllies,
            _ => CombatTargeting.SingleEnemy
        };

    private static string BuildEffectLog(string effectType) =>
        effectType switch
        {
            AbilityEffectType.Damage => "{Actor}'s Essence hit {Target} for {Amount}.",
            AbilityEffectType.Heal => "{Actor}'s Essence restored {Amount} health to {Target}.",
            AbilityEffectType.GrantBarrier => "{Actor}'s Essence granted {Amount} barrier to {Target}.",
            AbilityEffectType.RestoreResource => "{Actor}'s Essence restored {Amount} resource to {Target}.",
            AbilityEffectType.ModifyAttribute => "{Actor}'s Essence modified {Target} by {Amount}.",
            AbilityEffectType.ApplyStatus => "{Actor}'s Essence applied {Status} to {Target}.",
            AbilityEffectType.RemoveStatus => "{Actor}'s Essence removed {Status} from {Target}.",
            AbilityEffectType.Cleanse => "{Actor}'s Essence cleansed {Amount} effects from {Target}.",
            AbilityEffectType.Summon => "{Actor}'s Essence summoned {Target}.",
            AbilityEffectType.Taunt => "{Actor}'s Essence drew {Amount} threat from {Target}.",
            AbilityEffectType.ReflectDamage => "{Actor}'s Essence reflected {Amount} damage to {Target}.",
            AbilityEffectType.AbsorbDamage => "{Actor}'s Essence absorbed {Amount} damage for {Target}.",
            AbilityEffectType.TriggerSecondaryEffect => "{Actor}'s Essence triggered {Status} on {Target}.",
            _ => "{Actor}'s Essence affected {Target} for {Amount}."
        };

    private static int Scale(AbilityScalingFormula scaling, PlayerEssence essence) =>
        Math.Max(0, (int)Math.Round(scaling.BaseValue + scaling.PerLevel * Math.Max(0, essence.Level - 1) + scaling.PerAscensionTier * essence.AscensionTier));

    private static AttributeType? FirstScalingAttribute(AbilityEffectDefinition effect) =>
        effect.Scaling.AttributeScaling.FirstOrDefault()?.Attribute;

    private static float FirstScalingCoefficient(AbilityEffectDefinition effect) =>
        (float)(effect.Scaling.AttributeScaling.FirstOrDefault()?.Coefficient ?? 0);

    private static AttributeType ParseAttribute(string? attribute) =>
        Enum.TryParse<AttributeType>(attribute, ignoreCase: true, out var parsed)
            ? parsed
            : throw new NotSupportedException($"Essence attribute '{attribute}' is not supported by combat mapping.");

    private static int SecondsToCombatTicks(double seconds) => Math.Max(0, (int)Math.Round(seconds * 10));

    private static EssenceOperationResult Ok(string message) => new(true, message);
    private static EssenceOperationResult Fail(string message) => new(false, message);

    private static string GetMonsterDefinitionId(Creature creature) =>
        "monster." + creature.Name.Trim().Replace("'", "", StringComparison.Ordinal).Replace(" ", "_", StringComparison.Ordinal).ToLowerInvariant();
}
