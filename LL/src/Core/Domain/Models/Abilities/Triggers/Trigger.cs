using Domain.Interfaces.Abilities;
using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.Effects.Trigger;

namespace Domain.Models.Abilities.Triggers;
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