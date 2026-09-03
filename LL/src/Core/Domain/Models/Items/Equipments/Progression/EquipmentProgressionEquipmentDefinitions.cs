using System.Collections.Frozen;
using Domain.Models.Attributes;
using Domain.Models.Professions.Crafting.V2;

namespace Domain.Models.Items.Equipments.Progression;

// Equipment identity categories deliberately retain the shared persisted rarity values.
public enum EquipmentRarity
{
    Common = (int)Rarity.Common,
    Rare = (int)Rarity.Rare,
    Legendary = (int)Rarity.Legendary
}

public sealed class EquipmentArchetype
{
    public EquipmentArchetype(
        string id,
        string itemBaseId,
        EquipmentType equipmentType,
        EquipmentBehaviorDefinition behavior,
        IReadOnlyDictionary<AttributeType, double> statWeights,
        IReadOnlyDictionary<AttributeType, double>? overflowWeights = null,
        int minimumTier = 1,
        int maximumTier = EquipmentTierBudgetCurve.MaximumSupportedTier)
    {
        Id = EquipmentValidation.Id(id);
        ItemBaseId = EquipmentValidation.Id(itemBaseId);
        if (!Enum.IsDefined(equipmentType) || equipmentType == EquipmentType.Tool)
            throw new ArgumentOutOfRangeException(nameof(equipmentType));
        ArgumentNullException.ThrowIfNull(behavior);
        var expectedHandedness = equipmentType is EquipmentType.OneHanded or EquipmentType.TwoHanded or EquipmentType.OffHand
            ? equipmentType.ToString() : string.Empty;
        if (!string.Equals(behavior.Handedness, expectedHandedness, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Archetype handedness must match its equipment type.", nameof(behavior));
        EquipmentValidation.PositiveFinite(behavior.BasicAttackIntervalMultiplier);
        EquipmentValidation.PositiveFinite(behavior.BasicAttackDamageMultiplier);
        if (minimumTier < 1 || maximumTier < minimumTier || maximumTier > EquipmentTierBudgetCurve.MaximumSupportedTier)
            throw new ArgumentOutOfRangeException(nameof(maximumTier));

        EquipmentType = equipmentType;
        Behavior = behavior;
        StatWeights = EquipmentValidation.Weights(statWeights);
        OverflowWeights = EquipmentValidation.Weights(overflowWeights ?? new Dictionary<AttributeType, double>(), allowEmpty: true);
        MinimumTier = minimumTier;
        MaximumTier = maximumTier;
    }

    public string Id { get; }
    public string ItemBaseId { get; }
    public EquipmentType EquipmentType { get; }
    public EquipmentBehaviorDefinition Behavior { get; }
    public IReadOnlyDictionary<AttributeType, double> StatWeights { get; }
    public IReadOnlyDictionary<AttributeType, double> OverflowWeights { get; }
    public int MinimumTier { get; }
    public int MaximumTier { get; }
}

public sealed class EquipmentStyle
{
    public EquipmentStyle(
        string id,
        IEnumerable<string> compatibleArchetypeIds,
        IReadOnlyDictionary<AttributeType, double> statWeights,
        string? equipmentSetId = null)
    {
        Id = EquipmentValidation.Id(id);
        ArgumentNullException.ThrowIfNull(compatibleArchetypeIds);
        CompatibleArchetypeIds = compatibleArchetypeIds.Select(EquipmentValidation.Id)
            .ToFrozenSet(StringComparer.Ordinal);
        if (CompatibleArchetypeIds.Count == 0)
            throw new ArgumentException("A style must have a compatible archetype.", nameof(compatibleArchetypeIds));
        StatWeights = EquipmentValidation.Weights(statWeights);
        EquipmentSetId = equipmentSetId is null ? null : EquipmentValidation.Id(equipmentSetId);
    }

    public string Id { get; }
    public IReadOnlySet<string> CompatibleArchetypeIds { get; }
    public IReadOnlyDictionary<AttributeType, double> StatWeights { get; }
    public string? EquipmentSetId { get; }
}

public sealed class EquipmentDefinition
{
    public EquipmentDefinition(
        string id,
        string name,
        string archetypeId,
        EquipmentRarity rarity,
        string? nativeStyleId = null,
        long randomDiscoveryBaseScrap = 0)
    {
        Id = EquipmentValidation.Id(id);
        Name = EquipmentValidation.Id(name);
        ArchetypeId = EquipmentValidation.Id(archetypeId);
        if (!Enum.IsDefined(rarity))
            throw new ArgumentOutOfRangeException(nameof(rarity));
        if (randomDiscoveryBaseScrap < 0)
            throw new ArgumentOutOfRangeException(nameof(randomDiscoveryBaseScrap));
        Rarity = rarity;
        NativeStyleId = nativeStyleId is null ? null : EquipmentValidation.Id(nativeStyleId);
        RandomDiscoveryBaseScrap = randomDiscoveryBaseScrap;
    }

    public string Id { get; }
    public string Name { get; }
    public string ArchetypeId { get; }
    public EquipmentRarity Rarity { get; }
    public string? NativeStyleId { get; }
    public long RandomDiscoveryBaseScrap { get; }
}

internal static class EquipmentValidation
{
    public static string Id(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value != value.Trim())
            throw new ArgumentException("Equipment identifiers must not have surrounding whitespace.", nameof(value));
        return value;
    }

    public static void PositiveFinite(double value)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Equipment values must be finite and positive.");
    }

    public static IReadOnlyDictionary<AttributeType, double> Weights(
        IReadOnlyDictionary<AttributeType, double> weights,
        bool allowEmpty = false)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (!allowEmpty && weights.Count == 0)
            throw new ArgumentException("Equipment stat weights cannot be empty.", nameof(weights));
        foreach (var (attribute, weight) in weights)
        {
            if (!EquipmentStatBudgetCatalog.IsKnown(attribute))
                throw new ArgumentException($"Unknown equipment attribute '{attribute}'.", nameof(weights));
            PositiveFinite(weight);
        }
        if (weights.Count == 0)
            return FrozenDictionary<AttributeType, double>.Empty;
        var total = weights.Values.Sum();
        PositiveFinite(total);
        return weights.ToFrozenDictionary(entry => entry.Key, entry => entry.Value / total);
    }
}
