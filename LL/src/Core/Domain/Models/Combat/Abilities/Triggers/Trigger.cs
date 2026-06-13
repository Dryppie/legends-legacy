using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects;
using Domain.Models.Combat.Abilities.Effects.Trigger;

namespace Domain.Models.Combat.Abilities.Triggers;
public class Trigger
{
    public TriggerEvent Event { get; set; }
    public List<ITriggerFilter> Filters { get; set; } = [];
    public List<EffectDefinition> Actions { get; set; } = [];

    internal Trigger Clone()
    {
        return new Trigger()
        {
            Event = Event,
            Filters = [.. Filters.Select(f => f.Clone())],
            Actions = [.. Actions.Select(a => a.Clone())]
        };
    }
}