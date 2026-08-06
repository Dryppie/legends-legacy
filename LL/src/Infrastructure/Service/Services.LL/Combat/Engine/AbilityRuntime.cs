using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities;
using Domain.Models.Damages;

namespace Services.LL.Combat.Engine;

public enum CombatTeam
{
    Friendly = 0,
    Hostile = 1
}

public sealed class CompiledAbility
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public AbilitySpecKind Kind { get; init; }
    public int CooldownTicks { get; init; }
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
    public float ScalingCoefficient { get; init; }
    public float MaximumScalingCoefficient { get; init; }
    public float EventMagnitudeCoefficient { get; init; }
    public StandardConditionType? ScalingCondition { get; init; }
    public float ConditionScalingCoefficient { get; init; }
    public string? ScalingStatusId { get; init; }
    public float StatusScalingCoefficient { get; init; }
    public AttributeType? Attribute { get; init; }
    public string? StatusId { get; init; }
    public StandardConditionType? Condition { get; init; }
    public StandardConditionType? AlternativeCondition { get; init; }
    public string? SummonId { get; init; }
    public double SummonPowerMultiplier { get; init; } = 1d;
    public double SummonHealthMultiplier { get; init; } = 1d;
    public AbilityResourceType Resource { get; init; }
    public int DurationTicks { get; init; }
    public int IntervalTicks { get; init; }
    public int Uses { get; init; }
    public int ChancePercent { get; init; }
    public AttackType AttackType { get; init; }
    public DamageType DamageType { get; init; }
    public CritEligibility CritEligibility { get; init; }
    public float CritChanceBonus { get; init; }
    public float ArmorPenetrationBonus { get; init; }
    public float LifeStealPercentage { get; init; }
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
    public required IReadOnlyDictionary<AbilityTriggerEvent, IReadOnlyList<CompiledTrigger>> TriggersByEvent { get; init; }
}

public sealed class CompiledSummon
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ImagePath { get; init; }
    public int DurationTicks { get; init; }
    public int MaxActive { get; init; }
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
    private readonly Dictionary<CompiledTrigger, int> _triggerOccurrences = [];
    private readonly HashSet<CompiledTrigger> _activeTriggers = [];
    private readonly Dictionary<string, int> _effectUses = new(StringComparer.OrdinalIgnoreCase);

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

        foreach (var trigger in _triggerCooldowns.Keys.ToList())
        {
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

    public bool CanUseEffect(CompiledEffect effect) =>
        effect.Uses <= 0 || _effectUses.GetValueOrDefault(effect.Id) < effect.Uses;

    public void MarkEffectUsed(CompiledEffect effect)
    {
        if (effect.Uses <= 0)
            return;

        _effectUses[effect.Id] = _effectUses.GetValueOrDefault(effect.Id) + 1;
    }

}

public sealed class RuntimeStatus
{
    private readonly Dictionary<CompiledTrigger, int> _triggerCooldowns = [];
    private readonly Dictionary<CompiledTrigger, int> _triggerOccurrences = [];
    private readonly HashSet<CompiledTrigger> _activeTriggers = [];
    private readonly Dictionary<string, int> _effectUses = new(StringComparer.OrdinalIgnoreCase);
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
    public bool IsExpired => _durationTicks > 0 && RemainingDurationTicks <= 0;

    public void AddStacks(int amount)
    {
        Stacks = Math.Clamp(Stacks + amount, 0, Definition.MaxStacks);
        if (_durationTicks > 0)
            RemainingDurationTicks = _durationTicks;
    }

    public void Refresh(int stacks)
    {
        Stacks = Math.Clamp(Math.Max(Stacks, stacks), 1, Definition.MaxStacks);
        if (_durationTicks > 0)
            RemainingDurationTicks = _durationTicks;
    }

    public void Tick()
    {
        if (RemainingDurationTicks > 0)
            RemainingDurationTicks--;

        foreach (var trigger in _triggerCooldowns.Keys.ToList())
        {
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

    public bool CanUseEffect(CompiledEffect effect) =>
        effect.Uses <= 0 || _effectUses.GetValueOrDefault(effect.Id) < effect.Uses;

    public void MarkEffectUsed(CompiledEffect effect)
    {
        if (effect.Uses <= 0)
            return;

        _effectUses[effect.Id] = _effectUses.GetValueOrDefault(effect.Id) + 1;
    }
}

public sealed class RuntimeEffect
{
    public RuntimeEffect(
        CompiledEffect definition,
        RuntimeCombatant source,
        RuntimeCombatant target,
        string? statsSource = null,
        double durationMultiplier = 1d)
    {
        Definition = definition;
        Source = source;
        Target = target;
        StatsSource = string.IsNullOrWhiteSpace(statsSource) ? definition.StatsSource : statsSource;
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
    public RuntimeBarrierContribution(RuntimeCombatant? source, float amount, long applicationOrder)
    {
        Source = source;
        Remaining = Math.Max(0, amount);
        ApplicationOrder = applicationOrder;
    }

    public RuntimeCombatant? Source { get; }
    public float Remaining { get; private set; }
    public long ApplicationOrder { get; }

    public float Consume(float amount)
    {
        var consumed = Math.Min(Remaining, Math.Max(0, amount));
        Remaining -= consumed;
        return consumed;
    }
}

public sealed record RuntimeBarrierConsumptionEntry(
    RuntimeCombatant? Source,
    float Amount,
    long ApplicationOrder);

public sealed record RuntimeBarrierConsumption(
    float Total,
    IReadOnlyList<RuntimeBarrierConsumptionEntry> Contributions);

public sealed class RuntimeCombatant
{
    private float _threat;
    private float _regenerationRatePercent;
    private int _regenerationIntervalModifierTicks;
    private float _healingReceivedPercent;
    private readonly Dictionary<DamageType, float> _damageDealtPercent = [];
    private readonly Dictionary<DamageType, float> _damageTakenPercent = [];
    private readonly Dictionary<StandardConditionType, float> _damageTakenFromConditionPercent = [];
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
        double basicAttackIntervalMultiplier = 1d,
        double basicAttackDamageMultiplier = 1d,
        AttackType basicAttackType = AttackType.Melee,
        DamageType basicAttackDamageType = DamageType.Physical)
    {
        Id = id;
        Name = name;
        Team = team;
        Attributes = new Dictionary<AttributeType, float>(attributes);
        Health = GetAttribute(AttributeType.MaxHealth);
        Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase);
        Abilities = abilities.Select(x => new RuntimeAbility(x)).ToList();
        ImagePath = imagePath;
        IsSummoned = isSummoned;
        RemainingSummonDurationTicks = summonDurationTicks;
        SummonOwner = summonOwner;
        BasicAttackIntervalMultiplier = Math.Max(0.1d, basicAttackIntervalMultiplier);
        BasicAttackDamageMultiplier = Math.Max(0.1d, basicAttackDamageMultiplier);
        BasicAttackType = basicAttackType;
        BasicAttackDamageType = basicAttackDamageType;
        RebuildTriggerIndex();
    }

    public string Id { get; }
    public string Name { get; }
    public string ImagePath { get; }
    public CombatTeam Team { get; }
    public Dictionary<AttributeType, float> Attributes { get; }
    public HashSet<string> Tags { get; }
    public List<RuntimeAbility> Abilities { get; }
    public List<RuntimeStatus> Statuses { get; } = [];
    public List<RuntimeCondition> Conditions { get; } = [];
    public List<RuntimeEffect> ActiveEffects { get; } = [];
    public List<RuntimeBarrierContribution> BarrierContributions { get; } = [];
    public Dictionary<AbilityTriggerEvent, List<RuntimeAbility>> AbilityTriggersByEvent { get; private set; } = [];
    public float Health { get; private set; }
    public float Barrier => BarrierContributions.Sum(x => x.Remaining);
    public float Threat => Math.Max(0, _threat);
    public float RegenerationRatePercent => _regenerationRatePercent;
    public int RegenerationIntervalModifierTicks => _regenerationIntervalModifierTicks;
    public float HealingReceivedPercent => _healingReceivedPercent;
    public bool IsSummoned { get; }
    public int RemainingSummonDurationTicks { get; private set; }
    public RuntimeCombatant? SummonOwner { get; }
    public double BasicAttackIntervalMultiplier { get; }
    public double BasicAttackDamageMultiplier { get; }
    public AttackType BasicAttackType { get; }
    public DamageType BasicAttackDamageType { get; }
    public bool IsAlive => Health > 0;

    public float GetAttribute(AttributeType attributeType) =>
        Attributes.GetValueOrDefault(attributeType);

    public void AdjustAttribute(AttributeType attributeType, float amount)
    {
        var oldMaxHealth = GetAttribute(AttributeType.MaxHealth);
        Attributes[attributeType] = Attributes.GetValueOrDefault(attributeType) + amount;

        if (attributeType == AttributeType.MaxHealth)
            SyncHealthAfterMaxHealthChange(oldMaxHealth, GetAttribute(AttributeType.MaxHealth));
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
        foreach (var contribution in BarrierContributions
                     .OrderByDescending(x => x.ApplicationOrder)
                     .ToList())
        {
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

    public float GrantBarrier(RuntimeCombatant? source, float amount, long applicationOrder = 0)
    {
        var cap = Math.Max(0, GetAttribute(AttributeType.MaxHealth) * 2.5f);
        var accepted = Math.Min(Math.Max(0, amount), Math.Max(0, cap - Barrier));
        if (accepted > 0)
            BarrierContributions.Add(new RuntimeBarrierContribution(source, accepted, applicationOrder));

        return accepted;
    }

    public RuntimeBarrierConsumption ConsumeBarrierWithSources(float amount)
    {
        var remaining = Math.Max(0, amount);
        var consumed = 0f;
        var consumedContributions = new List<RuntimeBarrierConsumptionEntry>();
        foreach (var contribution in BarrierContributions
                     .OrderBy(x => x.ApplicationOrder)
                     .ToList())
        {
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
                    contribution.ApplicationOrder));
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

    public void AdjustThreat(float amount) =>
        _threat += amount;

    public void AdjustRegenerationRate(float percentagePoints) =>
        _regenerationRatePercent += percentagePoints;

    public void AdjustRegenerationInterval(int ticks) =>
        _regenerationIntervalModifierTicks += ticks;

    public void AdjustHealingReceived(float percentagePoints) =>
        _healingReceivedPercent += percentagePoints;

    public void AdjustDamageDealt(DamageType damageType, float percentagePoints) =>
        _damageDealtPercent[damageType] = _damageDealtPercent.GetValueOrDefault(damageType) + percentagePoints;

    public void AdjustDamageTaken(DamageType damageType, float percentagePoints) =>
        _damageTakenPercent[damageType] = _damageTakenPercent.GetValueOrDefault(damageType) + percentagePoints;

    public void AdjustDamageTakenFromCondition(StandardConditionType condition, float percentagePoints) =>
        _damageTakenFromConditionPercent[condition] =
            _damageTakenFromConditionPercent.GetValueOrDefault(condition) + percentagePoints;

    public float GetDamageDealtPercent(DamageType damageType) =>
        _damageDealtPercent.GetValueOrDefault(DamageType.None)
        + _damageDealtPercent.GetValueOrDefault(damageType);

    public float GetDamageTakenPercent(DamageType damageType, RuntimeCombatant source)
    {
        var total = _damageTakenPercent.GetValueOrDefault(DamageType.None)
                    + _damageTakenPercent.GetValueOrDefault(damageType);
        total += _damageTakenFromConditionPercent
            .Where(entry => source.HasCondition(entry.Key))
            .Sum(entry => entry.Value);
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

    public int GetStatusStacks(string statusId) =>
        Statuses.Where(x => x.Definition.Id.Equals(statusId, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Stacks);

    public bool HasCondition(StandardConditionType type) =>
        Conditions.Any(x => x.Type == type && x.Value > 0);

    public int GetConditionStacks(StandardConditionType type) =>
        Conditions.Where(x => x.Type == type).Sum(x => x.Value);

    public void RebuildTriggerIndex()
    {
        AbilityTriggersByEvent = Abilities
            .SelectMany(ability => ability.Definition.TriggersByEvent.Keys.Select(triggerEvent => (triggerEvent, ability)))
            .GroupBy(x => x.triggerEvent)
            .ToDictionary(x => x.Key, x => x.Select(item => item.ability).ToList());
    }
}
