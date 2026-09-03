using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;
using Domain.Models.Professions.Crafting.V2;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>Frozen award/combat descriptor. Loading it never consults current content.</summary>
public sealed class EquipmentData
{
    [JsonConstructor]
    public EquipmentData(EquipmentStateSnapshot state, string itemBaseId, string displayName,
        EquipmentRarity rarity, EquipmentType equipmentType, EquipmentBehaviorDefinition behavior,
        IReadOnlyDictionary<AttributeType, float> stats, string? equipmentSetId)
    {
        EquipmentState = EquipmentState.Restore(state);
        State = EquipmentState.ToSnapshot();
        ItemBaseId = EquipmentValidation.Id(itemBaseId);
        DisplayName = EquipmentValidation.Id(displayName);
        if (!Enum.IsDefined(rarity) || !Enum.IsDefined(equipmentType) || equipmentType == EquipmentType.Tool)
            throw new InvalidOperationException("Invalid frozen equipment identity.");
        ArgumentNullException.ThrowIfNull(behavior);
        var expectedHandedness = equipmentType is EquipmentType.OneHanded or EquipmentType.TwoHanded or EquipmentType.OffHand
            ? equipmentType.ToString() : string.Empty;
        if (!string.Equals(behavior.Handedness, expectedHandedness, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Frozen equipment handedness does not match its slot type.");
        EquipmentValidation.PositiveFinite(behavior.BasicAttackIntervalMultiplier);
        EquipmentValidation.PositiveFinite(behavior.BasicAttackDamageMultiplier);
        ArgumentNullException.ThrowIfNull(stats);
        if (stats.Count == 0 || stats.Any(x => !EquipmentStatBudgetCatalog.IsKnown(x.Key) || !float.IsFinite(x.Value) || x.Value < 0)
            || !stats.Values.Any(x => x > 0))
            throw new InvalidOperationException("Invalid frozen equipment stats.");
        Rarity = rarity;
        EquipmentType = equipmentType;
        Behavior = behavior;
        Stats = stats.ToFrozenDictionary();
        EquipmentSetId = equipmentSetId is null ? null : EquipmentValidation.Id(equipmentSetId);
    }

    public EquipmentStateSnapshot State { get; }
    [JsonIgnore] public EquipmentState EquipmentState { get; }
    public string ItemBaseId { get; }
    public string DisplayName { get; }
    public EquipmentRarity Rarity { get; }
    public EquipmentType EquipmentType { get; }
    public EquipmentBehaviorDefinition Behavior { get; }
    public IReadOnlyDictionary<AttributeType, float> Stats { get; }
    public string? EquipmentSetId { get; }

    public static EquipmentData Create(EquipmentState state, EquipmentEvaluator evaluator)
    {
        var evaluated = evaluator.Evaluate(state);
        return new(state.ToSnapshot(), evaluated.Archetype.ItemBaseId, evaluated.Definition.Name,
            evaluated.Definition.Rarity, evaluated.Archetype.EquipmentType, evaluated.Archetype.Behavior,
            evaluated.Stats, evaluated.EquipmentSetId);
    }

    public EquipmentData BindForPersonalUse() => new(
        EquipmentState.BindForPersonalUse().ToSnapshot(), ItemBaseId, DisplayName, Rarity,
        EquipmentType, Behavior, Stats, EquipmentSetId);

    public EquipmentData TransferToCharacter(Guid expectedOwnerId, Guid recipientId) => new(
        EquipmentState.TransferToCharacter(expectedOwnerId, recipientId).ToSnapshot(), ItemBaseId, DisplayName, Rarity,
        EquipmentType, Behavior, Stats, EquipmentSetId);

    public EquipmentData DonateToGuild(Guid expectedOwnerId, Guid guildId)
    {
        if (State.Ownership.OwnerId != expectedOwnerId)
            throw new InvalidOperationException("This equipment is not owned by the donor.");
        return new(EquipmentState.DonateToGuild(guildId).ToSnapshot(), ItemBaseId, DisplayName, Rarity,
            EquipmentType, Behavior, Stats, EquipmentSetId);
    }

    public string Serialize() => JsonSerializer.Serialize(this);
    public static EquipmentData Deserialize(string json) =>
        JsonSerializer.Deserialize<EquipmentData>(json)
        ?? throw new InvalidOperationException("Missing Equipment progression equipment descriptor.");
}
