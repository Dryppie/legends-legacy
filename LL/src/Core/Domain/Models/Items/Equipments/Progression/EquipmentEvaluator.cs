using System.Collections.Frozen;
using Domain.Models.Attributes;
using Domain.Models.Professions.Crafting.V2;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>One deterministic stat path for every Equipment progression acquisition route.</summary>
public sealed class EquipmentEvaluator
{
    private readonly IReadOnlyDictionary<string, EquipmentArchetype> _archetypes;
    private readonly IReadOnlyDictionary<string, EquipmentStyle> _styles;
    private readonly IReadOnlyDictionary<string, EquipmentDefinition> _definitions;

    public EquipmentEvaluator(
        EquipmentBalance balance,
        IEnumerable<EquipmentArchetype> archetypes,
        IEnumerable<EquipmentStyle> styles,
        IEnumerable<EquipmentDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(balance);
        Balance = balance;
        // ToDictionary deliberately rejects duplicate definitions instead of picking a winner.
        _archetypes = archetypes.ToDictionary(x => x.Id, StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal);
        _styles = styles.ToDictionary(x => x.Id, StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal);
        _definitions = definitions.ToDictionary(x => x.Id, StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal);
        foreach (var style in _styles.Values)
            foreach (var id in style.CompatibleArchetypeIds)
                if (!_archetypes.ContainsKey(id))
                    throw new ArgumentException($"Style '{style.Id}' references unknown archetype '{id}'.");
        foreach (var definition in _definitions.Values)
        {
            if (!_archetypes.ContainsKey(definition.ArchetypeId))
                throw new ArgumentException($"Unknown archetype '{definition.ArchetypeId}'.");
            ResolveStyle(definition.ArchetypeId, definition.NativeStyleId);
        }
    }

    public EquipmentBalance Balance { get; }

    public IReadOnlyList<EquipmentDefinition> Definitions => _definitions.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
    public EquipmentArchetype GetArchetype(string id) => _archetypes.TryGetValue(id, out var archetype)
        ? archetype : throw new ArgumentException($"Unknown equipment archetype '{id}'.", nameof(id));

    public EquipmentDefinition GetDefinition(string id) => _definitions.TryGetValue(id, out var definition)
        ? definition : throw new ArgumentException($"Unknown equipment definition '{id}'.", nameof(id));

    public EquipmentEvaluation Evaluate(EquipmentState item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.BalanceVersion != Balance.Version)
            throw new InvalidOperationException("Equipment must be evaluated with its recorded balance version.");
        return Evaluate(item.DefinitionId, item.Tier, item.Rank, item.ActiveStyleId);
    }

    public EquipmentEvaluation Evaluate(string definitionId, int tier, int rank, string? activeStyleId)
    {
        var definition = GetDefinition(definitionId);
        var archetype = _archetypes[definition.ArchetypeId];
        if (tier < archetype.MinimumTier || tier > archetype.MaximumTier)
            throw new ArgumentOutOfRangeException(nameof(tier));
        if (rank < 0 || rank > EquipmentBalance.MaximumRank)
            throw new ArgumentOutOfRangeException(nameof(rank));
        var style = ResolveStyle(archetype.Id, activeStyleId);
        var weights = archetype.StatWeights.ToDictionary(x => x.Key, x => x.Value * (style is null ? 1d : 1d - Balance.StyleBudgetShare));
        if (style is not null)
            foreach (var (attribute, weight) in style.StatWeights)
                weights[attribute] = weights.GetValueOrDefault(attribute) + weight * Balance.StyleBudgetShare;

        var baselineBudget = Balance.GetBaselineBudget(tier, archetype.EquipmentType);
        var targetBudget = baselineBudget * (1d + rank * Balance.RankBudgetIncrement);
        EquipmentValidation.PositiveFinite(targetBudget);
        var allocation = EquipmentBudgetAllocator.AllocateConstrained(
            tier, targetBudget, weights, [], archetype.OverflowWeights);
        if (allocation.UnspentBudget > Math.Max(0.000001d, targetBudget * 0.00000001d))
            throw new InvalidOperationException($"Equipment '{definitionId}' cannot spend its budget. Author overflow weights or revise its stat profile.");

        var stats = allocation.AddedPoints.OrderBy(x => x.Key).ToDictionary(
            x => x.Key,
            x => AttributeValueQuantizer.Quantize(x.Key, (float)x.Value));
        if (stats.Values.Any(value => !float.IsFinite(value) || value < 0) || !stats.Values.Any(value => value > 0))
            throw new InvalidOperationException("Equipment has no representable usable stats.");
        foreach (var (attribute, amount) in stats)
            if (amount > EquipmentStatBudgetCatalog.Get(attribute).PerItemHardCap)
                throw new InvalidOperationException($"Quantized equipment exceeds the cap for '{attribute}'.");

        return new EquipmentEvaluation(
            definition, archetype, tier, rank, Balance.Version, style?.Id, style?.EquipmentSetId,
            baselineBudget, targetBudget, stats.ToFrozenDictionary());
    }

    private EquipmentStyle? ResolveStyle(string archetypeId, string? styleId)
    {
        if (styleId is null)
            return null;
        if (!_styles.TryGetValue(styleId, out var style) || !style.CompatibleArchetypeIds.Contains(archetypeId))
            throw new ArgumentException($"Style '{styleId}' is unknown or incompatible with '{archetypeId}'.", nameof(styleId));
        return style;
    }
}

public sealed record EquipmentEvaluation(
    EquipmentDefinition Definition,
    EquipmentArchetype Archetype,
    int Tier,
    int Rank,
    int BalanceVersion,
    string? ActiveStyleId,
    string? EquipmentSetId,
    double BaselineBudget,
    double TargetBudget,
    IReadOnlyDictionary<AttributeType, float> Stats);
