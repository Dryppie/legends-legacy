using Domain.Models.Attributes;
using Domain.Models.Combat.Abilities.V2;
using Domain.Models.Damages;

namespace Services.LL.Combat.V2;

public enum CombatTeamV2
{
    Friendly = 0,
    Hostile = 1
}

public sealed class CompiledAbilityV2
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public AbilitySpecKind Kind { get; init; }
    public int CooldownTicks { get; init; }
    public required IReadOnlyDictionary<AbilityTriggerEvent, IReadOnlyList<CompiledTriggerV2>> TriggersByEvent { get; init; }
    public required IReadOnlySet<string> Tags { get; init; }
}

public sealed class CompiledTriggerV2
{
    public AbilityTriggerEvent Event { get; init; }
    public int InternalCooldownTicks { get; init; }
    public required IReadOnlyList<CompiledConditionV2> Conditions { get; init; }
    public required IReadOnlyList<CompiledEffectV2> Effects { get; init; }
}

public sealed class CompiledEffectV2
{
    public required string Id { get; init; }
    public required string StatsSource { get; init; }
    public AbilityEffectOperation Operation { get; init; }
    public AbilityTargetSelectorV2 Target { get; init; }
    public int BaseValue { get; init; }
    public AttributeType? ScalingAttribute { get; init; }
    public float ScalingCoefficient { get; init; }
    public AttributeType? Attribute { get; init; }
    public string? StatusId { get; init; }
    public string? SummonId { get; init; }
    public AbilityResourceTypeV2 Resource { get; init; }
    public int DurationTicks { get; init; }
    public int IntervalTicks { get; init; }
    public int Uses { get; init; }
    public int ChancePercent { get; init; }
    public AttackType AttackType { get; init; }
    public DamageType DamageType { get; init; }
    public float LifeStealPercentage { get; init; }
    public required IReadOnlyList<CompiledConditionV2> Conditions { get; init; }
}

public sealed class CompiledConditionV2
{
    public AbilityConditionTypeV2 Type { get; init; }
    public AbilityConditionSubject Subject { get; init; }
    public string? StatusId { get; init; }
    public string? Tag { get; init; }
    public int Value { get; init; }
}

public sealed class CompiledStatusV2
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlySet<string> Tags { get; init; }
    public AbilityStatusStackingPolicy StackingPolicy { get; init; }
    public int MaxStacks { get; init; }
    public int DurationTicks { get; init; }
    public required IReadOnlyDictionary<AbilityTriggerEvent, IReadOnlyList<CompiledTriggerV2>> TriggersByEvent { get; init; }
}

public sealed class CompiledSummonV2
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ImagePath { get; init; }
    public int DurationTicks { get; init; }
    public int MaxActive { get; init; }
    public required IReadOnlySet<string> Tags { get; init; }
    public required IReadOnlyList<string> AbilityIds { get; init; }
    public required IReadOnlyList<CompiledSummonAttributeV2> Attributes { get; init; }
}

public sealed class CompiledSummonAttributeV2
{
    public AttributeType Attribute { get; init; }
    public int BaseValue { get; init; }
    public AttributeType? ScalingAttribute { get; init; }
    public float ScalingCoefficient { get; init; }
    public int MinimumValue { get; init; }
}

public sealed class RuntimeAbilityV2
{
    private readonly Dictionary<CompiledTriggerV2, int> _triggerCooldowns = [];
    private readonly Dictionary<string, int> _effectUses = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeAbilityV2(CompiledAbilityV2 definition)
    {
        Definition = definition;
        RemainingCooldownTicks = 0;
    }

    public CompiledAbilityV2 Definition { get; }
    public int RemainingCooldownTicks { get; private set; }
    public bool IsReady => RemainingCooldownTicks <= 0;

    public void StartCooldown() => RemainingCooldownTicks = Definition.CooldownTicks;

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

    public bool CanUseTrigger(CompiledTriggerV2 trigger) =>
        trigger.InternalCooldownTicks <= 0 || !_triggerCooldowns.ContainsKey(trigger);

    public void StartTriggerCooldown(CompiledTriggerV2 trigger)
    {
        if (trigger.InternalCooldownTicks > 0)
            _triggerCooldowns[trigger] = trigger.InternalCooldownTicks;
    }

    public bool CanUseEffect(CompiledEffectV2 effect) =>
        effect.Uses <= 0 || _effectUses.GetValueOrDefault(effect.Id) < effect.Uses;

    public void MarkEffectUsed(CompiledEffectV2 effect)
    {
        if (effect.Uses <= 0)
            return;

        _effectUses[effect.Id] = _effectUses.GetValueOrDefault(effect.Id) + 1;
    }
}

public sealed class RuntimeStatusV2
{
    private readonly Dictionary<CompiledTriggerV2, int> _triggerCooldowns = [];
    private readonly Dictionary<string, int> _effectUses = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeStatusV2(
        CompiledStatusV2 definition,
        RuntimeCombatantV2 source,
        RuntimeCombatantV2 owner,
        int stacks,
        string? statsSource = null)
    {
        Definition = definition;
        Source = source;
        Owner = owner;
        StatsSource = string.IsNullOrWhiteSpace(statsSource) ? definition.Name : statsSource;
        Stacks = Math.Clamp(stacks, 1, definition.MaxStacks);
        RemainingDurationTicks = definition.DurationTicks;
    }

    public CompiledStatusV2 Definition { get; }
    public RuntimeCombatantV2 Source { get; }
    public RuntimeCombatantV2 Owner { get; }
    public string StatsSource { get; }
    public int Stacks { get; private set; }
    public int RemainingDurationTicks { get; private set; }
    public bool IsExpired => Definition.DurationTicks > 0 && RemainingDurationTicks <= 0;

    public void AddStacks(int amount)
    {
        Stacks = Math.Clamp(Stacks + amount, 0, Definition.MaxStacks);
        if (Definition.DurationTicks > 0)
            RemainingDurationTicks = Definition.DurationTicks;
    }

    public void Refresh(int stacks)
    {
        Stacks = Math.Clamp(Math.Max(Stacks, stacks), 1, Definition.MaxStacks);
        if (Definition.DurationTicks > 0)
            RemainingDurationTicks = Definition.DurationTicks;
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

    public bool CanUseTrigger(CompiledTriggerV2 trigger) =>
        trigger.InternalCooldownTicks <= 0 || !_triggerCooldowns.ContainsKey(trigger);

    public void StartTriggerCooldown(CompiledTriggerV2 trigger)
    {
        if (trigger.InternalCooldownTicks > 0)
            _triggerCooldowns[trigger] = trigger.InternalCooldownTicks;
    }

    public bool CanUseEffect(CompiledEffectV2 effect) =>
        effect.Uses <= 0 || _effectUses.GetValueOrDefault(effect.Id) < effect.Uses;

    public void MarkEffectUsed(CompiledEffectV2 effect)
    {
        if (effect.Uses <= 0)
            return;

        _effectUses[effect.Id] = _effectUses.GetValueOrDefault(effect.Id) + 1;
    }
}

public sealed class RuntimeEffectV2
{
    public RuntimeEffectV2(CompiledEffectV2 definition, RuntimeCombatantV2 source, RuntimeCombatantV2 target, string? statsSource = null)
    {
        Definition = definition;
        Source = source;
        Target = target;
        StatsSource = string.IsNullOrWhiteSpace(statsSource) ? definition.StatsSource : statsSource;
        RemainingDurationTicks = definition.DurationTicks;
        TicksUntilInterval = definition.IntervalTicks;
        RemainingUses = definition.Uses <= 0 ? int.MaxValue : definition.Uses;
    }

    public CompiledEffectV2 Definition { get; }
    public RuntimeCombatantV2 Source { get; }
    public RuntimeCombatantV2 Target { get; }
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

public sealed class RuntimeCombatantV2
{
    public RuntimeCombatantV2(
        string id,
        string name,
        CombatTeamV2 team,
        IDictionary<AttributeType, float> attributes,
        IEnumerable<CompiledAbilityV2> abilities,
        IEnumerable<string>? tags = null,
        string imagePath = "",
        bool isSummoned = false,
        int summonDurationTicks = 0,
        RuntimeCombatantV2? summonOwner = null)
    {
        Id = id;
        Name = name;
        Team = team;
        Attributes = new Dictionary<AttributeType, float>(attributes);
        Health = GetAttribute(AttributeType.MaxHealth);
        Tags = new HashSet<string>(tags ?? [], StringComparer.OrdinalIgnoreCase);
        Abilities = abilities.Select(x => new RuntimeAbilityV2(x)).ToList();
        ImagePath = imagePath;
        IsSummoned = isSummoned;
        RemainingSummonDurationTicks = summonDurationTicks;
        SummonOwner = summonOwner;
        RebuildTriggerIndex();
    }

    public string Id { get; }
    public string Name { get; }
    public string ImagePath { get; }
    public CombatTeamV2 Team { get; }
    public Dictionary<AttributeType, float> Attributes { get; }
    public HashSet<string> Tags { get; }
    public List<RuntimeAbilityV2> Abilities { get; }
    public List<RuntimeStatusV2> Statuses { get; } = [];
    public List<RuntimeEffectV2> ActiveEffects { get; } = [];
    public Dictionary<AbilityTriggerEvent, List<RuntimeAbilityV2>> AbilityTriggersByEvent { get; private set; } = [];
    public float Health { get; private set; }
    public float Barrier { get; private set; }
    public bool IsSummoned { get; }
    public int RemainingSummonDurationTicks { get; private set; }
    public RuntimeCombatantV2? SummonOwner { get; }
    public bool IsAlive => Health > 0;

    public float GetAttribute(AttributeType attributeType) =>
        Attributes.GetValueOrDefault(attributeType);

    public void AdjustAttribute(AttributeType attributeType, float amount) =>
        Attributes[attributeType] = Attributes.GetValueOrDefault(attributeType) + amount;

    public void AdjustHealth(float amount) =>
        Health = Math.Clamp(Health + amount, 0, GetAttribute(AttributeType.MaxHealth));

    public void SetHealth(float value) =>
        Health = Math.Clamp(value, 0, GetAttribute(AttributeType.MaxHealth));

    public bool TickSummonDuration()
    {
        if (!IsSummoned || RemainingSummonDurationTicks <= 0 || !IsAlive)
            return false;

        RemainingSummonDurationTicks--;
        return RemainingSummonDurationTicks <= 0;
    }

    public void AdjustBarrier(float amount) =>
        Barrier = Math.Max(0, Barrier + amount);

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

    public void RebuildTriggerIndex()
    {
        AbilityTriggersByEvent = Abilities
            .SelectMany(ability => ability.Definition.TriggersByEvent.Keys.Select(triggerEvent => (triggerEvent, ability)))
            .GroupBy(x => x.triggerEvent)
            .ToDictionary(x => x.Key, x => x.Select(item => item.ability).ToList());
    }
}
