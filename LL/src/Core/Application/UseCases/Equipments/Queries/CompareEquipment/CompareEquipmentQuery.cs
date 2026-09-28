using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.Equipments.Sets;
using Domain.Models.Items.Equipments.Progression;
using MediatR;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.CombatStyles;
using Application.UseCases.Equipments.Dtos;

namespace Application.UseCases.Equipments.Queries.CompareEquipment;

public sealed record CompareEquipmentQuery(
    Guid CharacterId,
    Guid EquipmentInstanceId,
    EquipmentSlotType? SlotType,
    EssenceCombatActivity Activity = EssenceCombatActivity.None)
    : IQuery<Response<EquipmentComparisonDto>>;

public sealed record EquipmentComparisonValueDto(
    AttributeType AttributeType,
    float Before,
    float After)
{
    public float Difference => After - Before;
}

public sealed record EquipmentComparisonDto(
    Guid EquipmentInstanceId,
    int CharacterLevel,
    EquipmentSlotType SlotType,
    IReadOnlyList<EquipmentComparisonValueDto> Ratings,
    IReadOnlyList<EquipmentComparisonValueDto> EffectiveAttributes)
{
    public int AttributeRulesVersion { get; init; } = AttributeRules.CurrentVersion;
    public EssenceCombatActivity Activity { get; init; }
    public string? Doctrine { get; init; }
    public IReadOnlyList<string> EssenceIds { get; init; } = [];
    public EquipmentBudgetBreakdown? Allocation { get; init; }
    public IReadOnlyList<EquipmentDerivedMetricDto> Metrics { get; init; } = [];
    public IReadOnlyList<EquipmentSetChangeDto> SetChanges { get; init; } = [];
    public IReadOnlyList<EquipmentAttributeBreakdownDto> Breakdown { get; init; } = [];
}

public sealed class CompareEquipmentQueryHandler
    : IRequestHandler<CompareEquipmentQuery, Response<EquipmentComparisonDto>>
{
    private readonly ICharacterService _characters;
    private readonly IInventoryService _inventories;
    private readonly IEssenceCombatLoadoutResolver _essenceLoadouts;
    private readonly EquipmentCatalog? _equipmentCatalog;
    private readonly IEquipmentLoadoutService? _equipmentLoadouts;
    private readonly ICombatStyleService? _combatStyles;
    private readonly IEssenceDefinitionRepository? _essenceDefinitions;

    public CompareEquipmentQueryHandler(
        ICharacterService characters,
        IInventoryService inventories,
        IEssenceCombatLoadoutResolver essenceLoadouts,
        EquipmentCatalog? equipmentCatalog = null,
        IEquipmentLoadoutService? equipmentLoadouts = null,
        ICombatStyleService? combatStyles = null,
        IEssenceDefinitionRepository? essenceDefinitions = null)
    {
        _characters = characters;
        _inventories = inventories;
        _essenceLoadouts = essenceLoadouts;
        _equipmentCatalog = equipmentCatalog;
        _equipmentLoadouts = equipmentLoadouts;
        _combatStyles = combatStyles;
        _essenceDefinitions = essenceDefinitions;
    }

    public async Task<Response<EquipmentComparisonDto>> Handle(
        CompareEquipmentQuery request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Activity))
            return Response<EquipmentComparisonDto>.Fail("Select one combat activity.");
        var character = await _characters.GetMyCharacterOverviewAsync(
            request.CharacterId,
            cancellationToken);
        if (character is null)
            return Response<EquipmentComparisonDto>.Fail("Character was not found.");

        var inventory = await _inventories.GetInventoryByIdAsync(
            request.CharacterId,
            cancellationToken);
        var candidate = inventory?.InventoryItems
            .Where(item => item.ItemInstanceId == request.EquipmentInstanceId)
            .Select(item => item.ItemInstance)
            .OfType<EquipmentInstance>()
            .SingleOrDefault();
        if (candidate is null)
            return Response<EquipmentComparisonDto>.Fail("Equipment was not found in this character's inventory.");

        var loadout = _essenceLoadouts.Resolve(
            character.Id,
            EssenceLoadoutSelection.Select(character.EssenceLoadouts, request.Activity)?
                .Slots
                .Select(slot => slot.PlayerEssence)
                .Where(essence => essence is not null)
                .Cast<Domain.Models.Essences.PlayerEssence>() ?? []);

        var activitySlots = _equipmentLoadouts is null ? null : await _equipmentLoadouts.ResolveAsync(character.Id, request.Activity, cancellationToken);
        var projectedCharacter = new Character { Id = character.Id, Level = character.Level, AttributeRulesVersion = character.AttributeRulesVersion, BaseAttributes = character.BaseAttributes,
            EquipmentSlots = activitySlots ?? character.EquipmentSlots };
        if (!EquipmentComparisonProjector.TryProject(
                projectedCharacter,
                candidate,
                request.SlotType,
                loadout.AttributeModifiers,
                _equipmentCatalog?.EquipmentSets,
                out var comparison))
        {
            return Response<EquipmentComparisonDto>.Fail(
                "The selected equipment cannot be placed in that slot.");
        }

        var doctrine = _combatStyles is null ? null : await _combatStyles.ResolveAsync(character.Id, request.Activity, cancellationToken,
            loadout.EquippedEssences);
        var metrics = comparison!.Metrics.ToList();
        var cooldownAttribute = comparison.AttributeRulesVersion == AttributeRules.CurrentVersion ? AttributeType.AbilityHaste : AttributeType.Cooldown;
        var haste = comparison.EffectiveAttributes.FirstOrDefault(x => x.AttributeType == cooldownAttribute);
        // Unchanged values are absent from the delta list; the breakdown retains their exact value.
        var afterHaste = comparison.Breakdown.FirstOrDefault(x => x.AttributeType == cooldownAttribute)?.Effective ?? 0;
        var beforeHaste = haste?.Before ?? afterHaste;
        foreach (var essence in loadout.EquippedEssences)
        {
            if (_essenceDefinitions?.GetById(essence.EssenceDefinitionId) is not { } definition) continue;
            var ability = EssenceAbilityProgressionScaler.Apply(definition.ActiveAbility, essence.AscensionTier);
            if (ability.CooldownTicks <= 0) continue;
            metrics.Add(new($"cooldown:{ability.Id}", ability.Name,
                Cooldown(ability.CooldownTicks, beforeHaste) / 10d,
                Cooldown(ability.CooldownTicks, afterHaste) / 10d, "s",
                comparison.AttributeRulesVersion == AttributeRules.CurrentVersion
                    ? "Initial and repeat cooldown after ascension; temporary effects and action denial excluded."
                    : "Repeat cooldown after ascension; legacy initial timing and temporary effects excluded."));
        }
        comparison = comparison with { Activity = request.Activity, Doctrine = doctrine?.Kind.ToString(),
            EssenceIds = loadout.EquippedEssences.Select(x => x.EssenceDefinitionId).ToArray(), Metrics = metrics };
        return Response<EquipmentComparisonDto>.Success(comparison);

        int Cooldown(int ticks, float value) => comparison.AttributeRulesVersion == AttributeRules.CurrentVersion
            ? AttributeRules.CooldownTicks(ticks, value) : AttributeCombatRules.CalculateCooldownTicks(ticks, value);
    }
}

/// <summary>
/// Produces the same complete-character projection used by combat, before and after
/// a hypothetical replacement. Keeping this on the server prevents clients from
/// reimplementing rating aggregation, diminishing returns, or hand-slot rules.
/// </summary>
public static class EquipmentComparisonProjector
{
    public static bool TryProject(
        Character character,
        EquipmentInstance candidate,
        EquipmentSlotType? requestedSlot,
        IEnumerable<Domain.Models.Attributes.Modifiers.AttributeModifierBase> additionalModifiers,
        out EquipmentComparisonDto? comparison) =>
        TryProject(
            character,
            candidate,
            requestedSlot,
            additionalModifiers,
            null,
            out comparison);

    public static bool TryProject(
        Character character,
        EquipmentInstance candidate,
        EquipmentSlotType? requestedSlot,
        IEnumerable<Domain.Models.Attributes.Modifiers.AttributeModifierBase> additionalModifiers,
        IEnumerable<EquipmentSetDefinition>? equipmentSetDefinitions,
        out EquipmentComparisonDto? comparison)
    {
        comparison = null;
        var slots = character.EquipmentSlots.ToDictionary(slot => slot.EquipmentSlotType);
        if (!TryResolveTargetSlot(slots, candidate, requestedSlot, out var targetSlot))
            return false;

        var beforeEquipment = slots.Values
            .Where(slot => slot.EquipmentInstance is not null)
            .Select(slot => slot.EquipmentInstance!)
            .DistinctBy(item => item.Id)
            .ToArray();
        var afterBySlot = slots.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.EquipmentInstance);

        ApplyReplacement(afterBySlot, candidate, targetSlot);
        var afterEquipment = afterBySlot.Values
            .Where(item => item is not null)
            .Cast<EquipmentInstance>()
            .DistinctBy(item => item.Id)
            .ToArray();
        var baseAttributes = character.BaseAttributes.ToDictionary(
            attribute => attribute.AttributeType,
            attribute => attribute.Value);
        var extras = additionalModifiers.ToArray();
        var definitions = equipmentSetDefinitions?.ToArray() ?? [];
        var beforeExtras = extras
            .Concat(EquipmentSetBonusResolver.ResolveAttributeModifiers(beforeEquipment, definitions))
            .ToArray();
        var afterExtras = extras
            .Concat(EquipmentSetBonusResolver.ResolveAttributeModifiers(afterEquipment, definitions))
            .ToArray();
        var beforeEffective = AttributeCalculator.CalculateProjectedEquipmentAttributes(
            baseAttributes, beforeEquipment, character.Level, beforeExtras, character.AttributeRulesVersion);
        var afterEffective = AttributeCalculator.CalculateProjectedEquipmentAttributes(
            baseAttributes, afterEquipment, character.Level, afterExtras, character.AttributeRulesVersion);
        var beforeRaw = AttributeCalculator.CalculateUncappedEquipmentAttributes(baseAttributes, beforeEquipment, character.Level, beforeExtras, character.AttributeRulesVersion);
        ApplyWeaponSpeed(beforeEffective, beforeRaw, slots.GetValueOrDefault(EquipmentSlotType.MainHand)?.EquipmentInstance);
        var beforeRatings = AttributeCalculator.CollectRawEquipmentRatings(beforeEquipment);
        var afterRatings = AttributeCalculator.CollectRawEquipmentRatings(afterEquipment);
        var afterRaw = AttributeCalculator.CalculateUncappedEquipmentAttributes(baseAttributes, afterEquipment, character.Level, afterExtras, character.AttributeRulesVersion);
        ApplyWeaponSpeed(afterEffective, afterRaw, afterBySlot.GetValueOrDefault(EquipmentSlotType.MainHand));
        var baseProjection = AttributeCalculator.CalculateProjectedAttributes(baseAttributes, [], character.AttributeRulesVersion);
        var equipmentProjection = AttributeCalculator.CalculateProjectedEquipmentAttributes(baseAttributes, afterEquipment, character.Level, rulesVersion: character.AttributeRulesVersion);
        var beforeSets = EquipmentSetBonusResolver.Resolve(beforeEquipment, definitions).SelectMany(x => x.ActiveBonuses)
            .Select(x => (x.SetId, x.Bonus.Id)).ToHashSet();
        var afterSets = EquipmentSetBonusResolver.Resolve(afterEquipment, definitions).SelectMany(x => x.ActiveBonuses)
            .Select(x => (x.SetId, x.Bonus.Id)).ToHashSet();

        comparison = new EquipmentComparisonDto(
            candidate.Id,
            character.Level,
            targetSlot,
            BuildValues(beforeRatings, afterRatings),
            BuildValues(beforeEffective, afterEffective))
        {
            AttributeRulesVersion = character.AttributeRulesVersion,
            Allocation = candidate.ProgressionData?.Allocation,
            Metrics = BuildMetrics(beforeEffective, afterEffective, slots.GetValueOrDefault(EquipmentSlotType.MainHand)?.EquipmentInstance,
                afterBySlot.GetValueOrDefault(EquipmentSlotType.MainHand), character.AttributeRulesVersion),
            SetChanges = definitions.SelectMany(set => set.Bonuses.Select(bonus => new EquipmentSetChangeDto(set.Id, set.Name, bonus.Id,
                bonus.Description, beforeSets.Contains((set.Id, bonus.Id)), afterSets.Contains((set.Id, bonus.Id)))))
                .Where(change => change.ActiveBefore != change.ActiveAfter).ToArray(),
            Breakdown = afterRaw.Keys.Order().Select(attribute => new EquipmentAttributeBreakdownDto(attribute,
                baseProjection.GetValueOrDefault(attribute), equipmentProjection.GetValueOrDefault(attribute), afterRaw[attribute],
                afterEffective.GetValueOrDefault(attribute), Math.Max(0, afterRaw[attribute] - afterEffective.GetValueOrDefault(attribute)))).ToArray()
        };
        return true;
    }

    private static IReadOnlyList<EquipmentDerivedMetricDto> BuildMetrics(IReadOnlyDictionary<AttributeType, float> before,
        IReadOnlyDictionary<AttributeType, float> after, EquipmentInstance? beforeWeapon, EquipmentInstance? afterWeapon, int rulesVersion)
    {
        double Value(IReadOnlyDictionary<AttributeType, float> values, AttributeType type) => values.GetValueOrDefault(type);
        var metrics = new List<EquipmentDerivedMetricDto>();
        void Add(string id, string label, Func<IReadOnlyDictionary<AttributeType, float>, double> calculate, string unit, string assumption) =>
            metrics.Add(new(id, label, calculate(before), calculate(after), unit, assumption));
        Add("physical-ehp", "Physical survival", x => Value(x, AttributeType.MaxHealth) / (1d - Value(x, AttributeType.Armor) / 100d), "HP", "Physical damage only; no penetration, block, shields or recovery.");
        Add("magical-ehp", "Magical survival", x => Value(x, AttributeType.MaxHealth) / (1d - Value(x, AttributeType.Resistance) / 100d), "HP", "Magical damage only; no penetration, block, shields or recovery.");
        Add("crit-multiplier", "Critical multiplier", x => 1d + Value(x, AttributeType.CritDamage) / 100d, "×", "Eligible direct damage and healing; standard damage over time cannot crit.");
        if (rulesVersion == AttributeRules.CurrentVersion) Add("tenacity-resist", "Harmful effect resist chance", x => 100d * AttributeRules.TenacityResistanceChance((float)Value(x, AttributeType.Tenacity)), "%", "Chance per harmful application, including Doom and harmful stacks. Effects that land keep full duration and strength; excludes beneficial effects, boss Stagger and guaranteed applications.");
        Add("barrier-capacity", "Barrier capacity", x => 2.5d * Value(x, AttributeType.MaxHealth), "HP", "Maximum stored barrier, not a granted shield.");
        Add("regen", "Regeneration pulse", x => Value(x, AttributeType.HealthRegeneration), "HP / 5s", "Before Renewal, Decay, received-healing effects and overheal.");
        Add("restoration", "Authored recovery multiplier", x => AttributeRules.RestorationMultiplier((float)Value(x, AttributeType.Restoration)), "×", "Heals and barriers only; excludes regeneration, life steal and refunds.");
        metrics.Add(new("basic-interval", "Basic attack interval", 3d / AttributeCombatRules.CalculateBasicAttackRate(before.GetValueOrDefault(AttributeType.AttackSpeed), beforeWeapon?.ProgressionData?.Behavior.BasicAttackIntervalMultiplier ?? 1d),
            3d / AttributeCombatRules.CalculateBasicAttackRate(after.GetValueOrDefault(AttributeType.AttackSpeed), afterWeapon?.ProgressionData?.Behavior.BasicAttackIntervalMultiplier ?? 1d), "s", "Before Haste, Slow, Chill and action denial."));
        return metrics;
    }

    private static void ApplyWeaponSpeed(Dictionary<AttributeType, float> effective,
        IReadOnlyDictionary<AttributeType, float> raw, EquipmentInstance? weapon) =>
        effective[AttributeType.AttackSpeed] = Math.Clamp(raw.GetValueOrDefault(AttributeType.AttackSpeed), 0,
            AttributeCombatRules.CalculateUsefulAttackSpeedCapPercent(weapon?.ProgressionData?.Behavior.BasicAttackIntervalMultiplier ?? 1d));

    private static IReadOnlyList<EquipmentComparisonValueDto> BuildValues<T>(
        IReadOnlyDictionary<AttributeType, T> before,
        IReadOnlyDictionary<AttributeType, T> after)
        where T : struct, IConvertible =>
        before.Keys
            .Concat(after.Keys)
            .Distinct()
            .OrderBy(attribute => attribute)
            .Select(attribute => new EquipmentComparisonValueDto(
                attribute,
                Convert.ToSingle(before.GetValueOrDefault(attribute)),
                Convert.ToSingle(after.GetValueOrDefault(attribute))))
            .Where(value => Math.Abs(value.Difference) > 0.0001f)
            .ToArray();

    private static bool TryResolveTargetSlot(
        IReadOnlyDictionary<EquipmentSlotType, EquipmentSlot> slots,
        EquipmentInstance candidate,
        EquipmentSlotType? requested,
        out EquipmentSlotType target)
    {
        target = candidate.EquipmentBase.EquipmentType switch
        {
            EquipmentType.Head => EquipmentSlotType.Head,
            EquipmentType.Relic => EquipmentSlotType.Relic,
            EquipmentType.Chest => EquipmentSlotType.Chest,
            EquipmentType.Necklace => EquipmentSlotType.Necklace,
            EquipmentType.Legs => EquipmentSlotType.Legs,
            EquipmentType.Ring => EquipmentSlotType.Ring,
            EquipmentType.TwoHanded => EquipmentSlotType.MainHand,
            EquipmentType.OffHand => EquipmentSlotType.OffHand,
            EquipmentType.OneHanded when requested is EquipmentSlotType.MainHand or EquipmentSlotType.OffHand => requested.Value,
            EquipmentType.OneHanded when slots.GetValueOrDefault(EquipmentSlotType.MainHand)?.EquipmentInstance is null => EquipmentSlotType.MainHand,
            EquipmentType.OneHanded when slots.GetValueOrDefault(EquipmentSlotType.OffHand)?.EquipmentInstance is null => EquipmentSlotType.OffHand,
            EquipmentType.OneHanded => EquipmentSlotType.MainHand,
            _ => (EquipmentSlotType)(-1)
        };

        if (!slots.ContainsKey(target))
            return false;
        return candidate.EquipmentBase.EquipmentType != EquipmentType.OneHanded ||
            requested is null or EquipmentSlotType.MainHand or EquipmentSlotType.OffHand;
    }

    private static void ApplyReplacement(
        IDictionary<EquipmentSlotType, EquipmentInstance?> slots,
        EquipmentInstance candidate,
        EquipmentSlotType target)
    {
        var type = candidate.EquipmentBase.EquipmentType;
        slots.TryGetValue(EquipmentSlotType.MainHand, out var main);
        if (type is EquipmentType.TwoHanded)
        {
            slots[EquipmentSlotType.MainHand] = candidate;
            slots[EquipmentSlotType.OffHand] = candidate;
            return;
        }

        if ((type is EquipmentType.OneHanded or EquipmentType.OffHand) &&
            main?.EquipmentBase.EquipmentType == EquipmentType.TwoHanded)
        {
            slots[EquipmentSlotType.MainHand] = null;
            slots[EquipmentSlotType.OffHand] = null;
        }

        slots[target] = candidate;
    }
}
