using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Common.Primitives;
using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.Equipments.Sets;
using MediatR;

namespace Application.UseCases.Equipments.Queries.CompareEquipment;

public sealed record CompareEquipmentQuery(
    Guid CharacterId,
    Guid EquipmentInstanceId,
    EquipmentSlotType? SlotType)
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
    IReadOnlyList<EquipmentComparisonValueDto> EffectiveAttributes);

public sealed class CompareEquipmentQueryHandler
    : IRequestHandler<CompareEquipmentQuery, Response<EquipmentComparisonDto>>
{
    private readonly ICharacterService _characters;
    private readonly IInventoryService _inventories;
    private readonly IEssenceCombatLoadoutResolver _essenceLoadouts;
    private readonly ICraftingDefinitionProvider? _craftingDefinitions;

    public CompareEquipmentQueryHandler(
        ICharacterService characters,
        IInventoryService inventories,
        IEssenceCombatLoadoutResolver essenceLoadouts,
        ICraftingDefinitionProvider? craftingDefinitions = null)
    {
        _characters = characters;
        _inventories = inventories;
        _essenceLoadouts = essenceLoadouts;
        _craftingDefinitions = craftingDefinitions;
    }

    public async Task<Response<EquipmentComparisonDto>> Handle(
        CompareEquipmentQuery request,
        CancellationToken cancellationToken)
    {
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
            EssenceLoadoutSelection.Select(character.EssenceLoadouts, EssenceCombatActivity.None)?
                .Slots
                .Select(slot => slot.PlayerEssence)
                .Where(essence => essence is not null)
                .Cast<Domain.Models.Essences.PlayerEssence>() ?? []);

        if (!EquipmentComparisonProjector.TryProject(
                character,
                candidate,
                request.SlotType,
                loadout.AttributeModifiers,
                _craftingDefinitions?.GetEquipmentSets(),
                out var comparison))
        {
            return Response<EquipmentComparisonDto>.Fail(
                "The selected equipment cannot be placed in that slot.");
        }

        return Response<EquipmentComparisonDto>.Success(comparison!);
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
            baseAttributes, beforeEquipment, character.Level, beforeExtras);
        var afterEffective = AttributeCalculator.CalculateProjectedEquipmentAttributes(
            baseAttributes, afterEquipment, character.Level, afterExtras);
        var beforeRatings = AttributeCalculator.CollectRawEquipmentRatings(beforeEquipment);
        var afterRatings = AttributeCalculator.CollectRawEquipmentRatings(afterEquipment);

        comparison = new EquipmentComparisonDto(
            candidate.Id,
            character.Level,
            targetSlot,
            BuildValues(beforeRatings, afterRatings),
            BuildValues(beforeEffective, afterEffective));
        return true;
    }

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
            EquipmentType.Tool => EquipmentSlotType.Tool,
            EquipmentType.OneHanded when requested is EquipmentSlotType.MainHand or EquipmentSlotType.OffHand => requested.Value,
            EquipmentType.OneHanded when slots.GetValueOrDefault(EquipmentSlotType.MainHand)?.EquipmentInstance is null => EquipmentSlotType.MainHand,
            EquipmentType.OneHanded when slots.GetValueOrDefault(EquipmentSlotType.OffHand)?.EquipmentInstance is null => EquipmentSlotType.OffHand,
            EquipmentType.OneHanded => EquipmentSlotType.MainHand,
            _ => (EquipmentSlotType)(-1)
        };

        if (!slots.ContainsKey(target))
            return false;
        if (candidate.EquipmentBase.EquipmentType == EquipmentType.Tool)
            return requested is null or EquipmentSlotType.Tool;
        if (requested == EquipmentSlotType.Tool)
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
