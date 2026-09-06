using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Attributes;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>Frozen award/combat descriptor. Loading it never consults current content.</summary>
public sealed class EquipmentData
{
    [JsonConstructor]
    public EquipmentData(EquipmentStateSnapshot state, string itemBaseId, string displayName,
        EquipmentRarity rarity, EquipmentType equipmentType, EquipmentBehaviorDefinition behavior,
        IReadOnlyDictionary<AttributeType, float> stats, string? equipmentSetId,
        IReadOnlyDictionary<AttributeType, float>? baseStats = null)
    {
        EquipmentState = EquipmentState.Restore(state);
        State = EquipmentState.ToSnapshot();
        ItemBaseId = EquipmentValidation.Id(itemBaseId);
        DisplayName = EquipmentValidation.Id(displayName);
        if (!Enum.IsDefined(rarity) || !Enum.IsDefined(equipmentType))
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
        if (baseStats is not null && baseStats.Any(x =>
                !EquipmentStatBudgetCatalog.IsKnown(x.Key) || !float.IsFinite(x.Value) || x.Value < 0
                || x.Value > stats.GetValueOrDefault(x.Key)))
            throw new InvalidOperationException("Invalid frozen equipment base stats.");
        BaseStats = baseStats?.ToFrozenDictionary();
        EquipmentSetId = equipmentSetId is null ? null : EquipmentValidation.Id(equipmentSetId);
    }

    public EquipmentStateSnapshot State { get; }
    [JsonIgnore] public EquipmentState EquipmentState { get; }
    public string ItemBaseId { get; }
    public string DisplayName { get; }
    public EquipmentRarity Rarity { get; }
    public ItemQuality Quality => State.Quality;
    public double AttributeRollMultiplier => State.AttributeRollMultiplier;
    public EquipmentType EquipmentType { get; }
    public EquipmentBehaviorDefinition Behavior { get; }
    public IReadOnlyDictionary<AttributeType, float> Stats { get; }
    public string? EquipmentSetId { get; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<AttributeType, float>? BaseStats { get; }

    public static EquipmentData Create(EquipmentState state, EquipmentEvaluator evaluator)
    {
        var evaluated = evaluator.Evaluate(state);
        return new(state.ToSnapshot(), evaluated.Archetype.ItemBaseId, evaluator.GetDisplayName(state),
            evaluated.Definition.Rarity, evaluated.Archetype.EquipmentType, evaluated.Archetype.Behavior,
            evaluated.Stats, evaluated.EquipmentSetId,
            state.AdditiveVariantBonus ? evaluator.Evaluate(state.DefinitionId, state.Tier, state.Rank,
                null, state.Quality, state.AttributeRollMultiplier).Stats : null);
    }

    /// <summary>Replaces only the variant contribution, retaining the frozen base across switches.</summary>
    public EquipmentData ApplyVariant(EquipmentEvaluator evaluator, string styleId)
    {
        var nextState = EquipmentState.ApplyVariant(evaluator, styleId);
        var baseline = evaluator.Evaluate(State.DefinitionId, State.Tier, State.Rank,
            null, Quality, AttributeRollMultiplier);
        var previous = BaseStats is null ? evaluator.Evaluate(State.DefinitionId, State.Tier, State.Rank,
            State.ActiveStyleId, Quality, AttributeRollMultiplier, State.AdditiveVariantBonus) : null;
        var next = evaluator.Evaluate(nextState.DefinitionId, nextState.Tier, nextState.Rank,
            styleId, Quality, AttributeRollMultiplier);

        // Older descriptors contain totals only. Reconstruct once using the available
        // allocation; never replace their entire stat roll with a current evaluation.
        var baseStats = BaseStats ?? Stats.Keys.Union(baseline.Stats.Keys).ToDictionary(
            attribute => attribute,
            attribute => (float)AttributeValueQuantizer.Quantize(attribute, Math.Max(0d,
                (double)Stats.GetValueOrDefault(attribute)
                - previous!.Stats.GetValueOrDefault(attribute) + baseline.Stats.GetValueOrDefault(attribute))))
            .Where(stat => stat.Value > 0).ToDictionary(stat => stat.Key, stat => stat.Value);
        var stats = baseStats.Keys.Union(next.Stats.Keys).ToDictionary(attribute => attribute, attribute =>
        {
            var baseValue = baseStats.GetValueOrDefault(attribute);
            var bonus = Math.Max(0d, (double)next.Stats.GetValueOrDefault(attribute)
                - baseline.Stats.GetValueOrDefault(attribute));
            return bonus == 0 ? baseValue : (float)Math.Max(baseValue, Math.Min(
                AttributeValueQuantizer.Quantize(attribute, baseValue + bonus),
                EquipmentStatBudgetCatalog.Get(attribute).PerItemHardCap));
        }).Where(stat => stat.Value > 0).ToDictionary(stat => stat.Key, stat => stat.Value);
        return new(nextState.ToSnapshot(), ItemBaseId, evaluator.GetDisplayName(nextState),
            Rarity, EquipmentType, Behavior, stats, next.EquipmentSetId, baseStats);
    }

    public EquipmentData BindForPersonalUse() => new(
        EquipmentState.BindForPersonalUse().ToSnapshot(), ItemBaseId, DisplayName, Rarity,
        EquipmentType, Behavior, Stats, EquipmentSetId, BaseStats);

    public EquipmentData TransferToCharacter(Guid expectedOwnerId, Guid recipientId) => new(
        EquipmentState.TransferToCharacter(expectedOwnerId, recipientId).ToSnapshot(), ItemBaseId, DisplayName, Rarity,
        EquipmentType, Behavior, Stats, EquipmentSetId, BaseStats);

    internal bool MatchesEvaluation(EquipmentData evaluated) =>
        State == evaluated.State && ItemBaseId == evaluated.ItemBaseId
        && DisplayName == evaluated.DisplayName && Rarity == evaluated.Rarity
        && EquipmentType == evaluated.EquipmentType && EquipmentSetId == evaluated.EquipmentSetId
        && JsonSerializer.Serialize(Behavior) == JsonSerializer.Serialize(evaluated.Behavior)
        && Stats.OrderBy(x => x.Key).SequenceEqual(evaluated.Stats.OrderBy(x => x.Key))
        && (BaseStats is null || evaluated.BaseStats is not null
            && BaseStats.OrderBy(x => x.Key).SequenceEqual(evaluated.BaseStats.OrderBy(x => x.Key)));

    /// <summary>
    /// Advances a frozen descriptor whose original authored content is no longer
    /// available. Existing stat proportions are retained instead of rerolling the
    /// item against current definitions.
    /// </summary>
    public EquipmentData ReinforceFrozen(EquipmentBalance balance)
    {
        ArgumentNullException.ThrowIfNull(balance);
        if (State.BalanceVersion != balance.Version)
            throw new InvalidOperationException("Equipment needs its original balance version before it can be reinforced.");
        if (State.Ownership.Kind == EquipmentOwnershipKind.GuildOwned)
            throw new InvalidOperationException("Guild equipment must retain guild ownership.");
        if (State.Rank >= EquipmentBalance.MaximumRank)
            throw new InvalidOperationException($"Equipment is already at rank {EquipmentBalance.MaximumRank}.");

        var currentRankScale = 1d + State.Rank * balance.RankBudgetIncrement;
        var nextRankScale = 1d + (State.Rank + 1) * balance.RankBudgetIncrement;
        var scale = nextRankScale / currentRankScale;
        var nextStats = Stats.ToDictionary(
            stat => stat.Key,
            stat => Math.Min(
                (float)AttributeValueQuantizer.Quantize(stat.Key, stat.Value * scale),
                EquipmentStatBudgetCatalog.Get(stat.Key).PerItemHardCap));
        if (nextStats.Any(stat => stat.Value < Stats.GetValueOrDefault(stat.Key))
            || !nextStats.Any(stat => stat.Value > Stats.GetValueOrDefault(stat.Key)))
            throw new InvalidOperationException("The next rank does not provide a representable improvement.");

        var nextState = EquipmentState.Restore(State with
        {
            Rank = State.Rank + 1,
            Ownership = new EquipmentOwnership(
                EquipmentOwnershipKind.BoundPersonal,
                State.Ownership.OwnerId)
        });
        return new EquipmentData(
            nextState.ToSnapshot(),
            ItemBaseId,
            DisplayName,
            Rarity,
            EquipmentType,
            Behavior,
            nextStats,
            EquipmentSetId,
            BaseStats?.ToDictionary(stat => stat.Key, stat => Math.Min(
                (float)AttributeValueQuantizer.Quantize(stat.Key, stat.Value * scale),
                nextStats.GetValueOrDefault(stat.Key))));
    }

    public EquipmentData DonateToGuild(Guid expectedOwnerId, Guid guildId)
    {
        if (State.Ownership.OwnerId != expectedOwnerId)
            throw new InvalidOperationException("This equipment is not owned by the donor.");
        return new(EquipmentState.DonateToGuild(guildId).ToSnapshot(), ItemBaseId, DisplayName, Rarity,
            EquipmentType, Behavior, Stats, EquipmentSetId, BaseStats);
    }

    public string Serialize() => JsonSerializer.Serialize(this);
    public static EquipmentData Deserialize(string json) =>
        JsonSerializer.Deserialize<EquipmentData>(json)
        ?? throw new InvalidOperationException("Missing Equipment progression equipment descriptor.");
}
