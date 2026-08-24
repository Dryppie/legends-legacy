using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;

namespace Services.LL.Combat.Engine;

public enum CombatTeam
{
    Friendly = 0,
    Hostile = 1
}

public sealed record CompiledAbilityCatalog(
    IReadOnlyDictionary<string, CompiledAbility> AbilitiesById,
    IReadOnlyDictionary<string, CompiledStatus> StatusesById,
    IReadOnlyDictionary<string, CompiledSummon> SummonsById);

public sealed class CompiledAbility
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public AbilitySpec? SourceSpec { get; init; }
    public AbilitySpecKind Kind { get; init; }
    public int CooldownTicks { get; init; }
    public int ThreatValue { get; init; }
    public float ThreatMultiplier { get; init; } = 1f;
    public required IReadOnlyList<CompiledCost> Costs { get; init; }
    public required IReadOnlyDictionary<AbilityTriggerEvent, IReadOnlyList<CompiledTrigger>> TriggersByEvent { get; init; }
    public required IReadOnlySet<string> Tags { get; init; }
}

public sealed class CompiledCost
{
    public AbilityResourceType Resource { get; init; }
    public int BaseValue { get; init; }
    public AttributeType? ScalingAttribute { get; init; }
    public float ScalingCoefficient { get; init; }
}

public sealed class CompiledTrigger
{
    public AbilityTriggerEvent Event { get; init; }
    public int ThreatValue { get; init; }
    public int ThreatInternalCooldownTicks { get; init; }
    public int InternalCooldownTicks { get; init; }
    public int InitialDelayTicks { get; init; }
    public int EveryNthOccurrence { get; init; } = 1;
    public required IReadOnlyList<CompiledCondition> Conditions { get; init; }
    public required IReadOnlyList<CompiledEffect> Effects { get; init; }
}

public sealed class CompiledEffect
{
    public required string Id { get; init; }
    public required string StatsSource { get; init; }
    public AbilityEffectOperation Operation { get; init; }
    public AbilityTargetSelector Target { get; init; }
    public int BaseValue { get; init; }
    public AttributeType? ScalingAttribute { get; init; }
    public AbilityConditionSubject ScalingAttributeSubject { get; init; }
    public float ScalingCoefficient { get; init; }
    public float MaximumScalingCoefficient { get; init; }
    public float EventMagnitudeCoefficient { get; init; }
    public StandardConditionType? ScalingCondition { get; init; }
    public float ConditionScalingCoefficient { get; init; }
    public string? ScalingStatusId { get; init; }
    public AbilityConditionSubject ScalingStatusSubject { get; init; }
    public AttributeType StatusScalingAttribute { get; init; } = AttributeType.Power;
    public float StatusScalingCoefficient { get; init; }
    public AttributeType? HealingScalingAttribute { get; init; }
    public float HealingScalingCoefficient { get; init; }
    public float MaximumHealingScalingCoefficient { get; init; }
    public AttributeType? Attribute { get; init; }
    public string? StatusId { get; init; }
    public StandardConditionType? Condition { get; init; }
    public StandardConditionType? AlternativeCondition { get; init; }
    public string? SummonId { get; init; }
    public bool CountAllOwnedSummons { get; init; }
    public int RepeatCount { get; init; } = 1;
    public int HealthStepPercent { get; init; }
    public string? RepeatPerOwnedSummonId { get; init; }
    public string? ScalingOwnedSummonId { get; init; }
    public float OwnedSummonScalingCoefficient { get; init; }
    public string? SummonGroupId { get; init; }
    public string? LinkedEffectId { get; init; }
    public double SummonPowerMultiplier { get; init; } = 1d;
    public double SummonHealthMultiplier { get; init; } = 1d;
    public AbilityResourceType Resource { get; init; }
    public int DurationTicks { get; init; }
    public bool RefreshDuration { get; init; }
    public int IntervalTicks { get; init; }
    public int Uses { get; init; }
    public bool OncePerTarget { get; init; }
    public bool GuaranteedConditionApplication { get; init; }
    public int StaggerPower { get; init; }
    public bool MaintainWhileConditionsMet { get; init; }
    public AbilityThreatFunctionBand? MaintainedThreatBand { get; init; }
    public float MaintainedThreatPerSecond { get; init; }
    public int LivingNonSummonedAllyDamagePercent { get; init; }
    public int SubsequentTargetDamagePercent { get; init; } = 100;
    public int ChancePercent { get; init; }
    public AttackType AttackType { get; init; }
    public DamageType DamageType { get; init; }
    public CritEligibility CritEligibility { get; init; }
    public float CritChanceBonus { get; init; }
    public float ArmorPenetrationBonus { get; init; }
    public float LifeStealPercentage { get; init; }
    public StandardConditionType? LifeStealTargetCondition { get; init; }
    public decimal ProcCoefficient { get; init; }
    public AbilitySpecKind AbilityKind { get; init; }
    public required IReadOnlySet<string> AbilityTags { get; init; }
    public required IReadOnlySet<string> Tags { get; init; }
    public required IReadOnlyList<CompiledCondition> Conditions { get; init; }
}

public sealed class CompiledCondition
{
    public AbilityConditionType Type { get; init; }
    public AbilityConditionSubject Subject { get; init; }
    public string? StatusId { get; init; }
    public StandardConditionType? Condition { get; init; }
    public DamageType DamageType { get; init; }
    public AttackType AttackType { get; init; }
    public string? Tag { get; init; }
    public int Value { get; init; }
}

public sealed class CompiledStatus
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlySet<string> Tags { get; init; }
    public AbilityStatusStackingPolicy StackingPolicy { get; init; }
    public int MaxStacks { get; init; }
    public int DurationTicks { get; init; }
    public bool LockAtMaxStacks { get; init; }
    public float SourceDamageTakenPercentPerStack { get; init; }
    public required IReadOnlyDictionary<AbilityTriggerEvent, IReadOnlyList<CompiledTrigger>> TriggersByEvent { get; init; }
}

public sealed class CompiledSummon
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ImagePath { get; init; }
    public int DurationTicks { get; init; }
    public int MaxActive { get; init; }
    public bool CanBasicAttack { get; init; }
    public float ThreatMultiplier { get; init; } = 0.25f;
    public required IReadOnlySet<string> Tags { get; init; }
    public required IReadOnlyList<string> AbilityIds { get; init; }
    public required IReadOnlyList<CompiledSummonAttribute> Attributes { get; init; }
}

public sealed class CompiledSummonAttribute
{
    public AttributeType Attribute { get; init; }
    public int BaseValue { get; init; }
    public AttributeType? ScalingAttribute { get; init; }
    public float ScalingCoefficient { get; init; }
    public int MinimumValue { get; init; }
}

public sealed class RuntimeAbility
{
    private readonly Dictionary<CompiledTrigger, int> _triggerCooldowns = [];
    private readonly Dictionary<CompiledTrigger, int> _triggerThreatCooldowns = [];
    private readonly List<CompiledTrigger> _triggerCooldownTickBuffer = [];
    private readonly Dictionary<CompiledTrigger, int> _triggerOccurrences = [];
    private readonly HashSet<CompiledTrigger> _activeTriggers = [];
    private readonly Dictionary<string, int> _effectUses = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _effectTargets = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeAbility(CompiledAbility definition)
    {
        Definition = definition;
        RemainingCooldownTicks = 0;
    }

    public CompiledAbility Definition { get; }
    public int RemainingCooldownTicks { get; private set; }
    public bool IsReady => RemainingCooldownTicks <= 0;

    public void StartInitialCooldown(float cooldownReductionPercent) =>
        RemainingCooldownTicks = AttributeCombatRules.CalculateCooldownTicks(
            Definition.CooldownTicks,
            cooldownReductionPercent);

    public void StartCooldown(float cooldownReductionPercent, int additionalTicks = 0) =>
        RemainingCooldownTicks = AttributeCombatRules.CalculateCooldownTicks(
            Definition.CooldownTicks + additionalTicks,
            cooldownReductionPercent);

    public void ReduceCooldown(int ticks)
    {
        if (ticks <= 0 || RemainingCooldownTicks <= 0)
            return;

        RemainingCooldownTicks = Math.Max(0, RemainingCooldownTicks - ticks);
    }

    public void Tick()
    {
        if (RemainingCooldownTicks > 0)
            RemainingCooldownTicks--;

        _triggerCooldownTickBuffer.Clear();
        foreach (var trigger in _triggerCooldowns.Keys)
            _triggerCooldownTickBuffer.Add(trigger);

        for (var index = 0; index < _triggerCooldownTickBuffer.Count; index++)
        {
            var trigger = _triggerCooldownTickBuffer[index];
            if (_triggerCooldowns[trigger] <= 1)
                _triggerCooldowns.Remove(trigger);
            else
                _triggerCooldowns[trigger]--;
        }

        _triggerCooldownTickBuffer.Clear();
        foreach (var trigger in _triggerThreatCooldowns.Keys)
            _triggerCooldownTickBuffer.Add(trigger);

        for (var index = 0; index < _triggerCooldownTickBuffer.Count; index++)
        {
            var trigger = _triggerCooldownTickBuffer[index];
            if (_triggerThreatCooldowns[trigger] <= 1)
                _triggerThreatCooldowns.Remove(trigger);
            else
                _triggerThreatCooldowns[trigger]--;
        }
    }

    public bool CanUseTrigger(CompiledTrigger trigger, int currentTick)
    {
        if (_activeTriggers.Contains(trigger) || currentTick < trigger.InitialDelayTicks)
            return false;

        var occurrence = _triggerOccurrences.GetValueOrDefault(trigger) + 1;
        _triggerOccurrences[trigger] = occurrence;
        if (occurrence % Math.Max(1, trigger.EveryNthOccurrence) != 0)
            return false;

        return trigger.InternalCooldownTicks <= 0 || !_triggerCooldowns.ContainsKey(trigger);
    }

    public void StartTriggerCooldown(CompiledTrigger trigger)
    {
        if (trigger.InternalCooldownTicks > 0)
            _triggerCooldowns[trigger] = trigger.InternalCooldownTicks;
    }

    public bool CanGenerateThreat(CompiledTrigger trigger) =>
        trigger.ThreatInternalCooldownTicks <= 0 || !_triggerThreatCooldowns.ContainsKey(trigger);

    public void StartThreatCooldown(CompiledTrigger trigger)
    {
        if (trigger.ThreatInternalCooldownTicks > 0)
            _triggerThreatCooldowns[trigger] = trigger.ThreatInternalCooldownTicks;
    }

    public void BeginTriggerExecution(CompiledTrigger trigger) => _activeTriggers.Add(trigger);

    public void EndTriggerExecution(CompiledTrigger trigger) => _activeTriggers.Remove(trigger);

    public bool CanUseEffect(CompiledEffect effect, RuntimeCombatant? target = null) =>
        (effect.Uses <= 0 || _effectUses.GetValueOrDefault(effect.Id) < effect.Uses)
        && (!effect.OncePerTarget
            || target is null
            || !_effectTargets.TryGetValue(effect.Id, out var targets)
            || !targets.Contains(target.Id));

    public void MarkEffectUsed(CompiledEffect effect, RuntimeCombatant target)
    {
        if (effect.Uses > 0)
            _effectUses[effect.Id] = _effectUses.GetValueOrDefault(effect.Id) + 1;

        if (effect.OncePerTarget)
        {
            if (!_effectTargets.TryGetValue(effect.Id, out var targets))
                _effectTargets[effect.Id] = targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            targets.Add(target.Id);
        }
    }

}

public sealed class RuntimeStatus
{
    private readonly Dictionary<CompiledTrigger, int> _triggerCooldowns = [];
    private readonly List<CompiledTrigger> _triggerCooldownTickBuffer = [];
    private readonly Dictionary<CompiledTrigger, int> _triggerOccurrences = [];
    private readonly HashSet<CompiledTrigger> _activeTriggers = [];
    private readonly Dictionary<string, int> _effectUses = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _effectTargets = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _durationTicks;

    public RuntimeStatus(
        CompiledStatus definition,
        RuntimeCombatant source,
        RuntimeCombatant owner,
        int stacks,
        string? statsSource = null,
        int? durationTicks = null)
    {
        Definition = definition;
        Source = source;
        Owner = owner;
        StatsSource = string.IsNullOrWhiteSpace(statsSource) ? definition.Name : statsSource;
        Stacks = Math.Clamp(stacks, 1, definition.MaxStacks);
        HasReachedLockedMaximum = definition.LockAtMaxStacks && Stacks >= definition.MaxStacks;
        _durationTicks = Math.Max(0, durationTicks ?? definition.DurationTicks);
        RemainingDurationTicks = _durationTicks;
    }

    public CompiledStatus Definition { get; }
    public RuntimeCombatant Source { get; }
    public RuntimeCombatant Owner { get; }
    public string StatsSource { get; }
    public int Stacks { get; private set; }
    public int DurationTicks => _durationTicks;
    public int RemainingDurationTicks { get; private set; }
    public bool IsExpired => !IsRemovalLocked && _durationTicks > 0 && RemainingDurationTicks <= 0;
    public bool HasReachedLockedMaximum { get; private set; }
    public bool IsRemovalLocked => Definition.LockAtMaxStacks && HasReachedLockedMaximum;

    public void AddStacks(int amount)
    {
        if (amount < 0 && IsRemovalLocked)
            return;

        Stacks = Math.Clamp(Stacks + amount, 0, Definition.MaxStacks);
        HasReachedLockedMaximum |= Definition.LockAtMaxStacks && Stacks >= Definition.MaxStacks;
        if (_durationTicks > 0)
            RemainingDurationTicks = _durationTicks;
    }

    public void Refresh(int stacks)
    {
        Stacks = Math.Clamp(Math.Max(Stacks, stacks), 1, Definition.MaxStacks);
        HasReachedLockedMaximum |= Definition.LockAtMaxStacks && Stacks >= Definition.MaxStacks;
        if (_durationTicks > 0)
            RemainingDurationTicks = _durationTicks;
    }

    public void Tick()
    {
        if (RemainingDurationTicks > 0)
            RemainingDurationTicks--;

        _triggerCooldownTickBuffer.Clear();
        foreach (var trigger in _triggerCooldowns.Keys)
            _triggerCooldownTickBuffer.Add(trigger);

        for (var index = 0; index < _triggerCooldownTickBuffer.Count; index++)
        {
            var trigger = _triggerCooldownTickBuffer[index];
            if (_triggerCooldowns[trigger] <= 1)
                _triggerCooldowns.Remove(trigger);
            else
                _triggerCooldowns[trigger]--;
        }
    }

    public bool CanUseTrigger(CompiledTrigger trigger, int currentTick)
    {
        if (_activeTriggers.Contains(trigger) || currentTick < trigger.InitialDelayTicks)
            return false;

        var occurrence = _triggerOccurrences.GetValueOrDefault(trigger) + 1;
        _triggerOccurrences[trigger] = occurrence;
        if (occurrence % Math.Max(1, trigger.EveryNthOccurrence) != 0)
            return false;

        return trigger.InternalCooldownTicks <= 0 || !_triggerCooldowns.ContainsKey(trigger);
    }

    public void StartTriggerCooldown(CompiledTrigger trigger)
    {
        if (trigger.InternalCooldownTicks > 0)
            _triggerCooldowns[trigger] = trigger.InternalCooldownTicks;
    }

    public void BeginTriggerExecution(CompiledTrigger trigger) => _activeTriggers.Add(trigger);

    public void EndTriggerExecution(CompiledTrigger trigger) => _activeTriggers.Remove(trigger);

    public bool CanUseEffect(CompiledEffect effect, RuntimeCombatant? target = null) =>
        (effect.Uses <= 0 || _effectUses.GetValueOrDefault(effect.Id) < effect.Uses)
        && (!effect.OncePerTarget
            || target is null
            || !_effectTargets.TryGetValue(effect.Id, out var targets)
            || !targets.Contains(target.Id));

    public void MarkEffectUsed(CompiledEffect effect, RuntimeCombatant target)
    {
        if (effect.Uses > 0)
            _effectUses[effect.Id] = _effectUses.GetValueOrDefault(effect.Id) + 1;

        if (effect.OncePerTarget)
        {
            if (!_effectTargets.TryGetValue(effect.Id, out var targets))
                _effectTargets[effect.Id] = targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            targets.Add(target.Id);
        }
    }
}

public sealed class RuntimeEffect
{
    public RuntimeEffect(
        CompiledEffect definition,
        RuntimeCombatant source,
        RuntimeCombatant target,
        string? statsSource = null,
        double durationMultiplier = 1d,
        string? activationId = null,
        int? appliedModifierValue = null)
    {
        Definition = definition;
        Source = source;
        Target = target;
        StatsSource = string.IsNullOrWhiteSpace(statsSource) ? definition.StatsSource : statsSource;
        ActivationId = activationId;
        AppliedModifierValue = appliedModifierValue;
        RemainingDurationTicks = definition.DurationTicks <= 0
            ? definition.DurationTicks
            : Math.Max(1, (int)Math.Ceiling(definition.DurationTicks * Math.Max(0, durationMultiplier)));
        TicksUntilInterval = definition.IntervalTicks;
        RemainingUses = definition.Uses <= 0 ? int.MaxValue : definition.Uses;
    }

    public CompiledEffect Definition { get; }
    public RuntimeCombatant Source { get; }
    public RuntimeCombatant Target { get; }
    public string StatsSource { get; }
    public string? ActivationId { get; }
    public int? AppliedModifierValue { get; }
    public int RemainingDurationTicks { get; private set; }
    public int TicksUntilInterval { get; private set; }
    public int RemainingUses { get; private set; }
    public bool IsExpired => RemainingDurationTicks <= 0 || RemainingUses <= 0 || !Target.IsAlive;

    public bool Tick()
    {
        if (!Target.IsAlive)
            return false;

        if (RemainingDurationTicks > 0)
            RemainingDurationTicks--;

        if (Definition.IntervalTicks <= 0)
            return false;

        if (TicksUntilInterval > 0)
            TicksUntilInterval--;

        if (TicksUntilInterval > 0 || RemainingUses <= 0)
            return false;

        TicksUntilInterval = Definition.IntervalTicks;
        RemainingUses--;
        return true;
    }
}

public sealed class RuntimeCondition
{
    public RuntimeCondition(
        StandardConditionType type,
        RuntimeCombatant source,
        RuntimeCombatant owner,
        int value,
        int durationTicks,
        float powerSnapshot,
        long applicationOrder,
        string statsSource,
        int intervalTicks = 0)
    {
        Type = type;
        Source = source;
        Owner = owner;
        Value = Math.Max(0, value);
        DurationTicks = Math.Max(0, durationTicks);
        RemainingDurationTicks = DurationTicks;
        PowerSnapshot = Math.Max(0, powerSnapshot);
        ApplicationOrder = applicationOrder;
        StatsSource = statsSource;
        IntervalTicks = Math.Max(0, intervalTicks);
        TicksUntilInterval = IntervalTicks;
    }

    public StandardConditionType Type { get; }
    public RuntimeCombatant Source { get; }
    public RuntimeCombatant Owner { get; }
    public int Value { get; private set; }
    public int DurationTicks { get; private set; }
    public int RemainingDurationTicks { get; private set; }
    public float PowerSnapshot { get; }
    public long ApplicationOrder { get; }
    public string StatsSource { get; }
    public int IntervalTicks { get; }
    public int TicksUntilInterval { get; private set; }
    public bool IsExpired => DurationTicks > 0 && RemainingDurationTicks <= 0;

    public void AddValue(int amount, int maximum = int.MaxValue) =>
        Value = (int)Math.Clamp((long)Value + amount, 0, maximum);

    public void ReplaceValue(int value) =>
        Value = Math.Max(0, value);

    public void RefreshDuration(int durationTicks)
    {
        DurationTicks = Math.Max(0, durationTicks);
        RemainingDurationTicks = DurationTicks;
    }

    public bool Tick()
    {
        if (RemainingDurationTicks > 0)
            RemainingDurationTicks--;

        if (IntervalTicks <= 0)
            return false;

        if (TicksUntilInterval > 0)
            TicksUntilInterval--;

        if (TicksUntilInterval > 0)
            return false;

        TicksUntilInterval = IntervalTicks;
        return true;
    }
}

public sealed class RuntimeBarrierContribution
{
    public RuntimeBarrierContribution(
        RuntimeCombatant? source,
        float amount,
        long applicationOrder,
        string? effectId = null,
        int durationTicks = 0,
        string? activationId = null,
        string? linkedEffectId = null)
    {
        Source = source;
        Remaining = Math.Max(0, amount);
        ApplicationOrder = applicationOrder;
        EffectId = effectId;
        RemainingDurationTicks = Math.Max(0, durationTicks);
        ActivationId = activationId;
        LinkedEffectId = linkedEffectId;
    }

    public RuntimeCombatant? Source { get; }
    public float Remaining { get; private set; }
    public long ApplicationOrder { get; }
    public string? EffectId { get; }
    public int RemainingDurationTicks { get; private set; }
    public string? ActivationId { get; }
    public string? LinkedEffectId { get; }
    public bool IsTimed => RemainingDurationTicks > 0;

    public float Consume(float amount)
    {
        var consumed = Math.Min(Remaining, Math.Max(0, amount));
        Remaining -= consumed;
        return consumed;
    }

    public bool TickDuration()
    {
        if (RemainingDurationTicks <= 0 || Remaining <= 0)
            return false;

        RemainingDurationTicks--;
        return RemainingDurationTicks <= 0;
    }
}

public sealed record RuntimeMaintainedModifier(
    CompiledEffect Definition,
    RuntimeCombatant Source,
    RuntimeCombatant Target,
    string StatsSource,
    int AppliedModifierValue);

public sealed class RuntimeCover
{
    public RuntimeCover(
        RuntimeCombatant guardian,
        int percent,
        float budget,
        int durationTicks,
        long applicationOrder,
        string statsSource)
    {
        Guardian = guardian;
        Percent = Math.Clamp(percent, 1, 100);
        BudgetRemaining = Math.Max(0, budget);
        RemainingDurationTicks = Math.Max(0, durationTicks);
        IsTimed = durationTicks > 0;
        ApplicationOrder = applicationOrder;
        StatsSource = statsSource;
    }

    public RuntimeCombatant Guardian { get; }
    public int Percent { get; }
    public float BudgetRemaining { get; private set; }
    public int RemainingDurationTicks { get; private set; }
    public bool IsTimed { get; }
    public long ApplicationOrder { get; }
    public string StatsSource { get; }
    public bool IsActive => Guardian.IsAlive
        && BudgetRemaining > 0
        && (!IsTimed || RemainingDurationTicks > 0);

    public float ConsumeBudget(float amount)
    {
        var consumed = Math.Min(BudgetRemaining, Math.Max(0, amount));
        BudgetRemaining -= consumed;
        return consumed;
    }

    public void Tick()
    {
        if (IsTimed && RemainingDurationTicks > 0)
            RemainingDurationTicks--;
    }
}

public sealed record RuntimeBarrierConsumptionEntry(
    RuntimeCombatant? Source,
    float Amount,
    long ApplicationOrder,
    string? EffectId,
    string? ActivationId,
    string? LinkedEffectId,
    bool IsDepleted);

public sealed record RuntimeBarrierConsumption(
    float Total,
    IReadOnlyList<RuntimeBarrierConsumptionEntry> Contributions);

public enum RuntimeStaggerTransition
{
    None = 0,
    Recovered = 1
}

public sealed class RuntimeStaggerState
{
    private readonly BossStaggerDefinition _definition;
    private readonly int _participantCount;

    public RuntimeStaggerState(BossStaggerDefinition definition, int participantCount)
    {
        _definition = definition;
        _participantCount = Math.Max(1, participantCount);
        Max = definition.CalculateThreshold(_participantCount, 0);
    }

    public int Current { get; private set; }
    public int Max { get; private set; }
    public int StaggeredRemainingTicks { get; private set; }
    public int RecoveryRemainingTicks { get; private set; }
    public int BreakCount { get; private set; }
    public bool IsStaggered => StaggeredRemainingTicks > 0;
    public bool IsRecovering => RecoveryRemainingTicks > 0;
    public int DamageTakenBonusPercent => IsStaggered
        ? Math.Max(0, _definition.DamageTakenBonusPercent)
        : 0;
    public bool CanBreak => !_definition.MaximumBreaks.HasValue
        || BreakCount < _definition.MaximumBreaks.Value;
    public bool CanAcceptContribution => CanBreak && !IsStaggered && !IsRecovering;

    public int Apply(int amount, out bool broke)
    {
        broke = false;
        if (amount <= 0 || !CanAcceptContribution)
            return 0;

        var applied = Math.Min(amount, Math.Max(0, Max - Current));
        Current += applied;
        if (Current < Max)
            return applied;

        BreakCount++;
        StaggeredRemainingTicks = Math.Max(1, _definition.BreakDurationTicks);
        RecoveryRemainingTicks = 0;
        broke = true;
        return applied;
    }

    public RuntimeStaggerTransition Tick()
    {
        if (StaggeredRemainingTicks > 0)
        {
            StaggeredRemainingTicks--;
            if (StaggeredRemainingTicks > 0)
                return RuntimeStaggerTransition.None;

            Current = 0;
            Max = _definition.CalculateThreshold(_participantCount, BreakCount);
            RecoveryRemainingTicks = Math.Max(0, _definition.RecoveryDurationTicks);
            return RuntimeStaggerTransition.Recovered;
        }

        if (RecoveryRemainingTicks > 0)
            RecoveryRemainingTicks--;

        return RuntimeStaggerTransition.None;
    }
}

public sealed class RuntimeCombatant
{
    public const float BaseThreat = EntityBaseAttributeHelper.BaseThreat;

    private static readonly RuntimeBarrierConsumption EmptyBarrierConsumption =
        new(0, Array.Empty<RuntimeBarrierConsumptionEntry>());

    private float _threat;
    private int _lastThreatUpdateTick;
    private float _regenerationRatePercent;
    private int _regenerationIntervalModifierTicks;
    private float _healingReceivedPercent;
    private readonly Dictionary<DamageType, float> _damageDealtPercent = [];
    private readonly Dictionary<int, float> _damageDealtToLowHealthPercent = [];
    private readonly Dictionary<DamageType, float> _damageTakenPercent = [];
    private readonly Dictionary<StandardConditionType, float> _damageTakenFromConditionPercent = [];
    private readonly Dictionary<string, (AttributeType Attribute, float Amount)> _synchronizedAttributeContributions =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RuntimeBarrierContribution> _barrierOrderingBuffer = [];
    private float _nextBasicAttackDamagePercent;
    private float _nextBasicAttackArmorPenetration;

    public RuntimeCombatant(
        string id,
        string name,
        CombatTeam team,
        IDictionary<AttributeType, float> attributes,
        IEnumerable<CompiledAbility> abilities,
        IEnumerable<string>? tags = null,
        string imagePath = "",
        bool isSummoned = false,
        int summonDurationTicks = 0,
        RuntimeCombatant? summonOwner = null,
        bool canBasicAttack = true,
        string? summonGroupId = null,
        string? summonGroupInstanceId = null,
        double basicAttackIntervalMultiplier = 1d,
        double basicAttackDamageMultiplier = 1d,
        AttackType basicAttackType = AttackType.Melee,
        DamageType basicAttackDamageType = DamageType.Physical,
        float threatMultiplier = 1f,
        int? partyNumber = null,
        BossStaggerDefinition? staggerDefinition = null,
        int staggerParticipantCount = 1)
    {
        Id = id;
        Name = name;
        Team = team;
        PartyNumber = partyNumber;
        Attributes = new Dictionary<AttributeType, float>(attributes);
        Attributes.TryAdd(AttributeType.Threat, BaseThreat);
        InitialAttributes = new Dictionary<AttributeType, float>(Attributes);
        Health = GetAttribute(AttributeType.MaxHealth);
        Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase);
        Abilities = abilities.Select(x => new RuntimeAbility(x)).ToList();
        ImagePath = imagePath;
        IsSummoned = isSummoned;
        ThreatMultiplier = Math.Max(0, threatMultiplier);
        _threat = GetBaseThreat();
        RemainingSummonDurationTicks = summonDurationTicks;
        SummonOwner = summonOwner;
        CanBasicAttack = canBasicAttack;
        SummonGroupId = summonGroupId;
        SummonGroupInstanceId = summonGroupInstanceId;
        BasicAttackIntervalMultiplier = Math.Max(0.1d, basicAttackIntervalMultiplier);
        BasicAttackDamageMultiplier = Math.Max(0.1d, basicAttackDamageMultiplier);
        BasicAttackType = basicAttackType;
        BasicAttackDamageType = basicAttackDamageType;
        Stagger = staggerDefinition is { Enabled: true }
            ? new RuntimeStaggerState(staggerDefinition, staggerParticipantCount)
            : null;
        RebuildTriggerIndex();
    }

    public string Id { get; }
    public string Name { get; }
    public string ImagePath { get; }
    public CombatTeam Team { get; }
    public int? PartyNumber { get; }
    public Dictionary<AttributeType, float> Attributes { get; }
    public IReadOnlyDictionary<AttributeType, float> InitialAttributes { get; }
    public HashSet<string> Tags { get; }
    public List<RuntimeAbility> Abilities { get; }
    public List<RuntimeStatus> Statuses { get; } = [];
    public List<RuntimeCondition> Conditions { get; } = [];
    public List<RuntimeEffect> ActiveEffects { get; } = [];
    public List<RuntimeMaintainedModifier> MaintainedModifiers { get; } = [];
    public List<RuntimeBarrierContribution> BarrierContributions { get; } = [];
    public List<RuntimeCover> Covers { get; } = [];
    public Dictionary<AbilityTriggerEvent, List<RuntimeAbility>> AbilityTriggersByEvent { get; private set; } = [];
    public float Health { get; private set; }
    public float Barrier
    {
        get
        {
            var total = 0f;
            for (var index = 0; index < BarrierContributions.Count; index++)
                total += BarrierContributions[index].Remaining;

            return total;
        }
    }
    public float Threat => Math.Max(0, _threat);
    public float ThreatMultiplier { get; }
    public float RegenerationRatePercent => _regenerationRatePercent;
    public int RegenerationIntervalModifierTicks => _regenerationIntervalModifierTicks;
    public float HealingReceivedPercent => _healingReceivedPercent;
    public bool IsSummoned { get; }
    public int RemainingSummonDurationTicks { get; private set; }
    public RuntimeCombatant? SummonOwner { get; }
    public bool CanBasicAttack { get; }
    public string? SummonGroupId { get; }
    public string? SummonGroupInstanceId { get; }
    public double BasicAttackIntervalMultiplier { get; }
    public double BasicAttackDamageMultiplier { get; }
    public AttackType BasicAttackType { get; }
    public DamageType BasicAttackDamageType { get; }
    public RuntimeStaggerState? Stagger { get; }
    public bool IsAlive => Health > 0;

    public float GetAttribute(AttributeType attributeType) =>
        Attributes.GetValueOrDefault(attributeType);

    public float GetInitialAttribute(AttributeType attributeType) =>
        InitialAttributes.GetValueOrDefault(attributeType);

    public void AdjustAttribute(AttributeType attributeType, float amount)
    {
        var oldMaxHealth = GetAttribute(AttributeType.MaxHealth);
        var oldBaseThreat = attributeType == AttributeType.Threat ? GetBaseThreat() : 0;
        Attributes[attributeType] = Attributes.GetValueOrDefault(attributeType) + amount;

        if (attributeType == AttributeType.MaxHealth)
            SyncHealthAfterMaxHealthChange(oldMaxHealth, GetAttribute(AttributeType.MaxHealth));

        if (attributeType == AttributeType.Threat)
        {
            Attributes[attributeType] = Math.Max(0, Attributes[attributeType]);
            _threat = Math.Max(0, _threat + GetBaseThreat() - oldBaseThreat);
        }
    }

    public float SynchronizeAttributeContribution(
        string contributionId,
        AttributeType attributeType,
        float desiredAmount)
    {
        if (_synchronizedAttributeContributions.TryGetValue(contributionId, out var existing)
            && existing.Attribute != attributeType)
        {
            throw new InvalidOperationException(
                $"Synchronized contribution '{contributionId}' changed attribute from '{existing.Attribute}' to '{attributeType}'.");
        }

        var currentAmount = existing.Amount;
        var delta = desiredAmount - currentAmount;
        if (Math.Abs(delta) > float.Epsilon)
            AdjustAttribute(attributeType, delta);

        if (Math.Abs(desiredAmount) <= float.Epsilon)
            _synchronizedAttributeContributions.Remove(contributionId);
        else
            _synchronizedAttributeContributions[contributionId] = (attributeType, desiredAmount);

        return delta;
    }

    public void AdjustHealth(float amount) =>
        Health = Math.Clamp(Health + amount, 0, GetAttribute(AttributeType.MaxHealth));

    public void SetHealth(float value) =>
        Health = Math.Clamp(value, 0, GetAttribute(AttributeType.MaxHealth));

    private void SyncHealthAfterMaxHealthChange(float oldMaxHealth, float newMaxHealth)
    {
        TrimBarrierToCap();

        if (oldMaxHealth <= 0)
        {
            SetHealth(newMaxHealth);
            return;
        }

        if (Health <= 0)
        {
            SetHealth(0);
            return;
        }

        if (newMaxHealth > oldMaxHealth)
        {
            AdjustHealth(newMaxHealth - oldMaxHealth);
            return;
        }

        SetHealth(Health);
    }

    private void TrimBarrierToCap()
    {
        var excess = Math.Max(0, Barrier - GetAttribute(AttributeType.MaxHealth) * 2.5f);
        _barrierOrderingBuffer.Clear();
        _barrierOrderingBuffer.AddRange(BarrierContributions);
        _barrierOrderingBuffer.Sort(static (left, right) =>
            right.ApplicationOrder.CompareTo(left.ApplicationOrder));

        for (var index = 0; index < _barrierOrderingBuffer.Count; index++)
        {
            var contribution = _barrierOrderingBuffer[index];
            if (excess <= 0)
                break;

            excess -= contribution.Consume(excess);
            if (contribution.Remaining <= 0)
                BarrierContributions.Remove(contribution);
        }
    }

    public bool TickSummonDuration()
    {
        if (!IsSummoned || RemainingSummonDurationTicks <= 0 || !IsAlive)
            return false;

        RemainingSummonDurationTicks--;
        return RemainingSummonDurationTicks <= 0;
    }

    public float GrantBarrier(
        RuntimeCombatant? source,
        float amount,
        long applicationOrder = 0,
        string? effectId = null,
        int durationTicks = 0,
        string? activationId = null,
        string? linkedEffectId = null)
    {
        var cap = Math.Max(0, GetAttribute(AttributeType.MaxHealth) * 2.5f);
        var accepted = Math.Min(Math.Max(0, amount), Math.Max(0, cap - Barrier));
        if (accepted > 0)
            BarrierContributions.Add(
                new RuntimeBarrierContribution(
                    source,
                    accepted,
                    applicationOrder,
                    effectId,
                    durationTicks,
                    activationId,
                    linkedEffectId));

        return accepted;
    }

    public RuntimeBarrierConsumption ConsumeBarrierWithSources(float amount)
    {
        var remaining = Math.Max(0, amount);
        if (remaining <= 0 || BarrierContributions.Count == 0)
            return EmptyBarrierConsumption;

        var consumed = 0f;
        var consumedContributions = new List<RuntimeBarrierConsumptionEntry>();
        _barrierOrderingBuffer.Clear();
        _barrierOrderingBuffer.AddRange(BarrierContributions);
        _barrierOrderingBuffer.Sort(static (left, right) =>
            left.ApplicationOrder.CompareTo(right.ApplicationOrder));

        for (var index = 0; index < _barrierOrderingBuffer.Count; index++)
        {
            var contribution = _barrierOrderingBuffer[index];
            if (remaining <= 0)
                break;

            var fromContribution = contribution.Consume(remaining);
            consumed += fromContribution;
            remaining -= fromContribution;
            if (fromContribution > 0)
            {
                consumedContributions.Add(new RuntimeBarrierConsumptionEntry(
                    contribution.Source,
                    fromContribution,
                    contribution.ApplicationOrder,
                    contribution.EffectId,
                    contribution.ActivationId,
                    contribution.LinkedEffectId,
                    contribution.Remaining <= 0));
            }

            if (contribution.Remaining <= 0)
                BarrierContributions.Remove(contribution);
        }

        return new RuntimeBarrierConsumption(consumed, consumedContributions);
    }

    public float ConsumeBarrier(float amount) =>
        ConsumeBarrierWithSources(amount).Total;

    public void AdjustBarrier(float amount)
    {
        if (amount >= 0)
            GrantBarrier(null, amount);
        else
            ConsumeBarrier(-amount);
    }

    public float GetThreat(int currentTick, double decayPerTick)
    {
        DecayThreat(currentTick, decayPerTick);
        return Threat;
    }

    public void AdjustThreat(float amount) =>
        _threat = Math.Max(0, _threat + amount);

    public void AdjustThreat(float amount, int currentTick, double decayPerTick)
    {
        DecayThreat(currentTick, decayPerTick);
        _threat = Math.Max(0, _threat + amount * ThreatMultiplier);
    }

    private void DecayThreat(int currentTick, double decayPerTick)
    {
        var elapsedTicks = Math.Max(0, currentTick - _lastThreatUpdateTick);
        if (elapsedTicks <= 0)
            return;

        var baseThreat = GetBaseThreat();
        if (decayPerTick > 0 && _threat != baseThreat)
        {
            _threat = (float)(baseThreat
                + (_threat - baseThreat) * Math.Pow(1d - decayPerTick, elapsedTicks));
            _threat = Math.Max(0, _threat);
        }

        _lastThreatUpdateTick = currentTick;
    }

    private float GetBaseThreat() =>
        Math.Max(0, GetAttribute(AttributeType.Threat) * ThreatMultiplier);

    public void AdjustRegenerationRate(float percentagePoints) =>
        _regenerationRatePercent += percentagePoints;

    public void AdjustRegenerationInterval(int ticks) =>
        _regenerationIntervalModifierTicks += ticks;

    public void AdjustHealingReceived(float percentagePoints) =>
        _healingReceivedPercent += percentagePoints;

    public void AdjustDamageDealt(DamageType damageType, float percentagePoints) =>
        _damageDealtPercent[damageType] = _damageDealtPercent.GetValueOrDefault(damageType) + percentagePoints;

    public void AdjustDamageDealtToLowHealth(int healthThresholdPercent, float percentagePoints) =>
        _damageDealtToLowHealthPercent[healthThresholdPercent] =
            _damageDealtToLowHealthPercent.GetValueOrDefault(healthThresholdPercent) + percentagePoints;

    public void AdjustDamageTaken(DamageType damageType, float percentagePoints) =>
        _damageTakenPercent[damageType] = _damageTakenPercent.GetValueOrDefault(damageType) + percentagePoints;

    public void AdjustDamageTakenFromCondition(StandardConditionType condition, float percentagePoints) =>
        _damageTakenFromConditionPercent[condition] =
            _damageTakenFromConditionPercent.GetValueOrDefault(condition) + percentagePoints;

    public float GetDamageDealtPercent(DamageType damageType) =>
        _damageDealtPercent.GetValueOrDefault(DamageType.None)
        + _damageDealtPercent.GetValueOrDefault(damageType);

    public float GetDamageDealtToLowHealthPercent(RuntimeCombatant target)
    {
        var maxHealth = target.GetAttribute(AttributeType.MaxHealth);
        if (maxHealth <= 0)
            return 0;

        return _damageDealtToLowHealthPercent
            .Where(entry => target.Health * 100 <= maxHealth * entry.Key + float.Epsilon)
            .Sum(entry => entry.Value);
    }

    public float GetDamageTakenPercent(DamageType damageType, RuntimeCombatant source)
    {
        var total = _damageTakenPercent.GetValueOrDefault(DamageType.None)
                    + _damageTakenPercent.GetValueOrDefault(damageType);
        total += Stagger?.DamageTakenBonusPercent ?? 0;

        foreach (var entry in _damageTakenFromConditionPercent)
        {
            if (source.HasCondition(entry.Key))
                total += entry.Value;
        }

        for (var index = 0; index < Statuses.Count; index++)
        {
            var status = Statuses[index];
            if (ReferenceEquals(status.Source, source))
                total += status.Stacks * status.Definition.SourceDamageTakenPercentPerStack;
        }

        return total;
    }

    public void ModifyNextBasicAttackDamage(float percentagePoints) =>
        _nextBasicAttackDamagePercent += percentagePoints;

    public void ModifyNextBasicAttackArmorPenetration(float percentagePoints) =>
        _nextBasicAttackArmorPenetration += percentagePoints;

    public (float DamagePercent, float ArmorPenetration) ConsumeNextBasicAttackModifiers()
    {
        var result = (_nextBasicAttackDamagePercent, _nextBasicAttackArmorPenetration);
        _nextBasicAttackDamagePercent = 0;
        _nextBasicAttackArmorPenetration = 0;
        return result;
    }

    public void ReduceAbilityCooldowns(int ticks)
    {
        foreach (var ability in Abilities)
            ability.ReduceCooldown(ticks);
    }

    public void Tick()
    {
        foreach (var ability in Abilities)
            ability.Tick();

        foreach (var status in Statuses)
            status.Tick();
    }

    public int GetStatusStacks(string statusId)
    {
        var stacks = 0;
        for (var index = 0; index < Statuses.Count; index++)
        {
            var status = Statuses[index];
            if (status.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase))
                stacks += status.Stacks;
        }

        return stacks;
    }

    public bool HasCondition(StandardConditionType type)
    {
        if (type == StandardConditionType.Cover)
            return Covers.Any(cover => cover.IsActive);

        for (var index = 0; index < Conditions.Count; index++)
        {
            var condition = Conditions[index];
            if (condition.Type == type && condition.Value > 0)
                return true;
        }

        return false;
    }

    public int GetConditionStacks(StandardConditionType type)
    {
        if (type == StandardConditionType.Cover)
            return Covers.Count(cover => cover.IsActive);

        var stacks = 0;
        for (var index = 0; index < Conditions.Count; index++)
        {
            var condition = Conditions[index];
            if (condition.Type == type)
                stacks += condition.Value;
        }

        return stacks;
    }

    public void RebuildTriggerIndex()
    {
        var index = new Dictionary<AbilityTriggerEvent, List<RuntimeAbility>>();
        for (var abilityIndex = 0; abilityIndex < Abilities.Count; abilityIndex++)
        {
            var ability = Abilities[abilityIndex];
            foreach (var triggerEvent in ability.Definition.TriggersByEvent.Keys)
            {
                if (!index.TryGetValue(triggerEvent, out var listeners))
                {
                    listeners = [];
                    index.Add(triggerEvent, listeners);
                }

                listeners.Add(ability);
            }
        }

        AbilityTriggersByEvent = index;
    }
}
