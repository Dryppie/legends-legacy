using System.Text.Json;
using Application.Interfaces.Services.LL.Essences;
using Common.Randomness;
using Domain.Helpers;
using Domain.Models.Entities.Characters;
using Domain.Models.Essences;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Items.Equipments.Slots;

namespace Services.LL.PowerRatings;

public sealed record EquipmentReferenceEquipmentSelection(
    EquipmentSlotType Slot, string DefinitionId, string? ActiveStyleId = null, bool UseNativeStyle = true);

public sealed record EquipmentReferenceBuildDefinition(
    string Id, int CharacterLevel, int Tier, int Rank,
    IReadOnlyList<EquipmentReferenceEquipmentSelection> Equipment, IReadOnlyList<string> EssenceIds,
    ItemQuality Quality = ItemQuality.Standard,
    double AttributeRollMultiplier = 1d);

public sealed record EquipmentReferenceBuild(
    EquipmentReferenceBuildDefinition Definition, Character Character,
    IReadOnlyList<EquipmentInstance> Equipment, IReadOnlyList<PlayerEssence> EquippedEssences,
    CombatRatingBreakdown Rating, int EquipmentBalanceVersion);

/// <summary>
/// Detached analysis builds using the same evaluator and frozen descriptors as live equipment.
/// No crafting rolls, payments, persistence, grants, or unsupported tier projection occur here.
/// </summary>
public sealed class EquipmentReferenceBuildFactory(
    EquipmentCatalog catalog,
    IEssenceDefinitionRepository essenceDefinitions,
    IEssenceCombatLoadoutResolver essenceLoadouts)
{
    public EquipmentReferenceBuild Create(EquipmentReferenceBuildDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Id);
        ArgumentNullException.ThrowIfNull(definition.Equipment);
        ArgumentNullException.ThrowIfNull(definition.EssenceIds);
        if (definition.CharacterLevel is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(definition), "Reference character levels must be 1-100.");
        if (definition.CharacterLevel < EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(definition.Tier))
            throw new ArgumentException("The reference level cannot equip this tier.", nameof(definition));
        var selections = definition.Equipment.OrderBy(x => x.Slot).ToArray();
        if (selections.Length is < 7 or > 8 || selections.Select(x => x.Slot).Distinct().Count() != selections.Length)
            throw new ArgumentException("Reference builds require a complete loadout with distinct slots.", nameof(definition));
        var essenceIds = definition.EssenceIds.ToArray();
        if (essenceIds.Length > EssenceSlotProgression.GetUnlockedSlotCount(definition.CharacterLevel))
            throw new ArgumentException("The reference level has not unlocked enough Essence slots.", nameof(definition));
        var essenceContent = essenceIds.Select(id => essenceDefinitions.GetById(id)
            ?? throw new ArgumentException($"Unknown reference Essence '{id}'.", nameof(definition))).ToArray();
        if (essenceContent.Select(x => x.SourceMonsterId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != essenceIds.Length)
            throw new ArgumentException("Reference Essences must come from distinct monster families.", nameof(definition));

        definition = definition with { Equipment = Array.AsReadOnly(selections), EssenceIds = Array.AsReadOnly(essenceIds) };
        var identity = JsonSerializer.Serialize(definition);
        var character = new Character
        {
            Id = StableRandom.Guid(EquipmentKeys.ReferenceCharacterIdentity, identity),
            Name = definition.Id,
            Level = definition.CharacterLevel
        };
        character.BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributesForLevel(character.Id, character.Level)
            .OrderBy(x => x.AttributeType).ToList();
        var equipment = selections.Select(selection =>
        {
            var itemId = StableRandom.Guid(EquipmentKeys.ReferenceEquipmentIdentity, identity, selection.Slot.ToString());
            var state = EquipmentState.Award(itemId, catalog.Evaluator, selection.DefinitionId,
                definition.Tier, definition.Rank,
                new(EquipmentAwardKind.Administrative, "offline-reference-build", definition.Id),
                new(EquipmentOwnershipKind.BoundPersonal, character.Id),
                definition.Quality,
                definition.AttributeRollMultiplier);
            state = EquipmentState.Restore(state.ToSnapshot() with
            {
                ActiveStyleId = selection.ActiveStyleId ?? (selection.UseNativeStyle ? state.NativeStyleId : null)
            });
            var data = EquipmentData.Create(state, catalog.Evaluator);
            if (!MatchesSlot(selection.Slot, data.EquipmentType))
                throw new ArgumentException($"'{selection.DefinitionId}' does not fit '{selection.Slot}'.", nameof(definition));
            var itemBase = catalog.GetEquipmentBase(data.ItemBaseId);
            var item = new EquipmentInstance { Id = itemId, ItemBaseId = data.ItemBaseId, ItemBase = itemBase };
            item.ApplyProgressionData(data);
            character.EquipmentSlots.Add(new EquipmentSlot
            {
                EntityId = character.Id, Entity = character, EquipmentSlotType = selection.Slot,
                EquipmentInstanceId = item.Id, EquipmentInstance = item
            });
            return item;
        }).ToArray();
        var main = character.EquipmentSlots.SingleOrDefault(x => x.EquipmentSlotType == EquipmentSlotType.MainHand)
            ?? throw new ArgumentException("Reference builds require a main-hand weapon.", nameof(definition));
        if (main.EquipmentInstance!.ProgressionData!.EquipmentType == EquipmentType.TwoHanded)
        {
            if (selections.Any(x => x.Slot == EquipmentSlotType.OffHand))
                throw new ArgumentException("A two-handed weapon occupies both hands.", nameof(definition));
            character.EquipmentSlots.Add(new EquipmentSlot
            {
                EntityId = character.Id, Entity = character, EquipmentSlotType = EquipmentSlotType.OffHand,
                EquipmentInstanceId = main.EquipmentInstanceId, EquipmentInstance = main.EquipmentInstance
            });
        }
        if (character.EquipmentSlots.Count != 8)
            throw new ArgumentException("Reference builds require all eight combat slots.", nameof(definition));
        var essences = essenceContent.Select((content, index) => new PlayerEssence
        {
            Id = StableRandom.Guid(EquipmentKeys.ReferenceEssenceIdentity, identity, index.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            CharacterId = character.Id, EssenceDefinitionId = content.Id, Level = 1,
            AbsorbedAt = DateTimeOffset.UnixEpoch, UpdatedAt = DateTimeOffset.UnixEpoch
        }).ToArray();
        var sources = essences.Select(essence => new CombatRatingModifierSource(definition.Tier,
            essenceLoadouts.Resolve(character.Id, [essence]).AttributeModifiers)).ToArray();
        return new(definition, character, Array.AsReadOnly(equipment), Array.AsReadOnly(essences),
            CombatRatingCalculator.Calculate(character.BaseAttributes, equipment, sources, character.Level),
            catalog.Evaluator.Balance.Version);
    }

    private static bool MatchesSlot(EquipmentSlotType slot, EquipmentType type) => slot switch
    {
        EquipmentSlotType.MainHand => type is EquipmentType.OneHanded or EquipmentType.TwoHanded,
        EquipmentSlotType.OffHand => type is EquipmentType.OneHanded or EquipmentType.OffHand,
        EquipmentSlotType.Head => type == EquipmentType.Head,
        EquipmentSlotType.Chest => type == EquipmentType.Chest,
        EquipmentSlotType.Legs => type == EquipmentType.Legs,
        EquipmentSlotType.Ring => type == EquipmentType.Ring,
        EquipmentSlotType.Necklace => type == EquipmentType.Necklace,
        EquipmentSlotType.Relic => type == EquipmentType.Relic,
        _ => false
    };
}
