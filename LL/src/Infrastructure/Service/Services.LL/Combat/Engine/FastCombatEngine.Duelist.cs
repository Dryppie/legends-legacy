using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.CombatStyles;
using Domain.Models.Damages;

namespace Services.LL.Combat.Engine;

public sealed partial class FastCombatEngine
{
    private static bool IsDuelistDirectEffect(CompiledEffect effect) =>
        effect.Operation is AbilityEffectOperation.Damage or AbilityEffectOperation.ConsumeConditionStacks
        && !IsPeriodicEffect(effect) && effect.AttackType != AttackType.DamageOverTime
        && !effect.Tags.Contains("Damage.Secondary") && effect.EventMagnitudeCoefficient == 0;

    private DuelistActionContext? BeginDuelistAction(RuntimeCombatant actor, bool basicAttack = false)
    {
        if (!_combatStyles.TryGetValue(actor, out var style) || style.Duelist is not { } state)
            return null;
        if (state.Opponent is { IsAlive: false })
            ResetDuelistRead(style);
        return new(style, state, basicAttack);
    }

    private void SelectDuelistOpponent(DuelistActionContext? action, CompiledEffect effect,
        RuntimeCombatant source, ReadOnlySpan<RuntimeCombatant> targets)
    {
        if (action is null || action.Opponent is not null || !ReferenceEquals(action.Style.Owner, source)
            || !source.IsAlive || !IsDuelistDirectEffect(effect))
            return;
        RuntimeCombatant? selected = null;
        foreach (var target in targets)
        {
            if (!target.IsAlive || AreAbilityAllies(source, target) || !CanAbilityAffectTarget(source, target))
                continue;
            selected ??= target;
            if (ReferenceEquals(action.State.Opponent, target))
            {
                selected = target;
                break;
            }
        }
        if (selected is not null)
            ChooseDuelistOpponent(action, selected);
    }

    private void ChooseDuelistOpponent(DuelistActionContext action, RuntimeCombatant target)
    {
        if (!ReferenceEquals(action.State.Opponent, target))
        {
            ResetDuelistRead(action.Style);
            action.State.Opponent = target;
            LogDuelistRead(action.Style);
        }
        action.Opponent = target;
        action.ReadRevision = action.State.Revision;
    }

    private int PrepareDuelistDamage(DuelistActionContext? action, RuntimeCombatant source,
        RuntimeCombatant target, int damage)
    {
        if (action is null || !ReferenceEquals(action.Style.Owner, source)
            || !ReferenceEquals(action.Opponent, target) || action.ReadRevision != action.State.Revision)
            return damage;
        var tuning = action.Style.Configuration.Tuning.Duelist!;
        if (!action.BasicAttack && !action.OpeningSpent && action.State.Read >= tuning.ReadRequired)
        {
            var configuration = action.Style.Configuration;
            action.OpeningMultiplier = tuning.Multiplier(configuration.Level);
            if (configuration.HasUpgrade(CombatStyleIds.MeasuredStrikes) && action.State.BasicAttacks > 0)
                action.OpeningMultiplier += configuration.HasMasteredUpgrade(CombatStyleIds.MeasuredStrikes)
                    && action.State.BasicAttacks >= 2 ? tuning.MasteredMeasuredStrikesBonus : tuning.UpgradeBonus;
            if (configuration.HasUpgrade(CombatStyleIds.KnowYourEnemy)
                && (action.State.HasSpentOpening || configuration.HasMasteredUpgrade(CombatStyleIds.KnowYourEnemy)))
                action.OpeningMultiplier += tuning.UpgradeBonus;
            var threshold = configuration.HasMasteredUpgrade(CombatStyleIds.FinishingTouch)
                ? tuning.MasteredFinishingTouchHealthThreshold : tuning.FinishingTouchHealthThreshold;
            if (configuration.HasUpgrade(CombatStyleIds.FinishingTouch)
                && target.Health / Math.Max(1, target.GetAttribute(AttributeType.MaxHealth)) <= threshold + 1e-7)
                action.OpeningMultiplier += tuning.UpgradeBonus;
            action.OpeningSpent = true;
            action.State.Read = 0;
            action.State.BasicAttacks = 0;
            action.State.HasSpentOpening = true;
            Log(source, target, "Duelist Opening", EventType.Buff, (int)Math.Round(action.OpeningMultiplier * 100),
                $"{source.Name} exploited an Opening against {target.Name} at {action.OpeningMultiplier:P0} damage.", "Combat Style: Duelist");
            LogDuelistRead(action.Style);
        }
        return Math.Max(0, (int)Math.Round(damage * action.OpeningMultiplier));
    }

    private void CompleteDuelistAction(DuelistActionContext? action, IReadOnlyList<RuntimeCombatant> combatants)
    {
        if (action is null || !action.Style.Owner.IsAlive || action.Life != action.State.Life)
            return;
        var tuning = action.Style.Configuration.Tuning.Duelist!;
        if (action.OpeningSpent && tuning.GuardCharges > 0)
            ApplyCondition(action.Style.Owner, action.Style.Owner, StandardConditionType.Guard,
                tuning.GuardCharges, 0, combatants, "Combat Style: Guarded Thrust", false, false, 0,
                publishApplication: false);

        if (action.Opponent is not { IsAlive: true } || action.ReadRevision != action.State.Revision)
            return;
        if (action.OpeningSpent)
        {
            if (tuning.ReturnedRead > 0)
            {
                action.State.Read = tuning.ReturnedRead;
                LogDuelistRead(action.Style);
            }
            return;
        }
        if (!action.LandedDamage)
            return;
        if (action.BasicAttack)
            action.State.BasicAttacks = Math.Min(2, action.State.BasicAttacks + 1);
        var gain = 1;
        if (action.State.FirstImpressionAvailable)
        {
            gain += tuning.FirstImpressionRead;
            action.State.FirstImpressionAvailable = false;
            Log(action.Style.Owner, action.Opponent, "First Impression", EventType.Buff, tuning.FirstImpressionRead,
                $"{action.Style.Owner.Name} gained {tuning.FirstImpressionRead} extra Read.", "Combat Style: First Impression");
        }
        action.State.Read = Math.Min(tuning.ReadRequired, action.State.Read + gain);
        LogDuelistRead(action.Style);
    }

    private void ResetDuelistRead(CombatStyleEncounterState style)
    {
        var state = style.Duelist!;
        state.Revision++;
        state.Opponent = null;
        state.Read = 0;
        state.BasicAttacks = 0;
        state.HasSpentOpening = false;
    }

    private void ClearDuelistOnDeath(RuntimeCombatant dead)
    {
        foreach (var style in _combatStyles.Values)
        {
            if (style.Duelist is not { } state)
                continue;
            if (ReferenceEquals(style.Owner, dead))
            {
                state.Life++;
                ResetDuelistRead(style);
                LogDuelistRead(style);
            }
            else if (ReferenceEquals(state.Opponent, dead))
            {
                ResetDuelistRead(style);
                LogDuelistRead(style);
            }
        }
    }

    private void LogDuelistRead(CombatStyleEncounterState style)
    {
        var state = style.Duelist!;
        var cap = style.Configuration.Tuning.Duelist!.ReadRequired;
        var description = state.Opponent is { } opponent
            ? $"{opponent.Name}: {(state.Read >= cap ? "Opening ready" : $"Read: {state.Read}/{cap}")}."
            : $"Read: 0/{cap}.";
        Log(style.Owner, state.Opponent, "Duelist Read", EventType.Buff, state.Read, description, "Combat Style: Duelist");
    }

    private sealed class DuelistEncounterState
    {
        public RuntimeCombatant? Opponent { get; set; }
        public int Read { get; set; }
        public int BasicAttacks { get; set; }
        public bool HasSpentOpening { get; set; }
        public bool FirstImpressionAvailable { get; set; }
        public int Revision { get; set; }
        public int Life { get; set; }
    }

    private sealed class DuelistActionContext(CombatStyleEncounterState style, DuelistEncounterState state, bool basicAttack)
    {
        public CombatStyleEncounterState Style { get; } = style;
        public DuelistEncounterState State { get; } = state;
        public bool BasicAttack { get; } = basicAttack;
        public int Life { get; } = state.Life;
        public int ReadRevision { get; set; }
        public RuntimeCombatant? Opponent { get; set; }
        public bool LandedDamage { get; set; }
        public bool OpeningSpent { get; set; }
        public double OpeningMultiplier { get; set; } = 1;
    }
}
