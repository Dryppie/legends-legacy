using Domain.Interfaces.Abilities;
using Domain.Models.Abilities.Triggers;
using Domain.Models.Combat;

namespace Domain.Models.Abilities.Statuses;
public class StatusInstance
{
    public StatusDefinition Definition { get; }
    public CombatEntity Owner { get; }
    public CombatEntity Source { get; }

    private readonly IEffectDuration _duration;
    private readonly IUsage _usage;

    public StatusInstance(StatusDefinition definition, CombatEntity source, CombatEntity owner)
    {
        Definition = definition;
        Source = source;
        Owner = owner;

        _duration = definition.Duration;
        _usage = definition.Usage;
    }

    public void Tick()
    {
        _duration.DecrementDuration();
        _usage.Recharge();
    }
    public bool CanUse()
    {
        return _usage.CanUse();
    }
    public void ConsumeUse()
    {
        _usage.ConsumeUse();
    }

    public bool IsExpired => !_duration.IsActive() || !_usage.CanUse();

    public IEnumerable<Trigger> GetTriggers()
        => Definition.Triggers;
}
