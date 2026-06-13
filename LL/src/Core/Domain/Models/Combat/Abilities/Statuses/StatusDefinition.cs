using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.Duration;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Combat.Abilities.Triggers;

namespace Domain.Models.Combat.Abilities.Statuses;
public class StatusDefinition
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;

    public bool IsStackable { get; set; } = false;

    private IEffectDuration? _duration;
    public IEffectDuration Duration
    {
        get => _duration ??= new NoDuration();
        set => _duration = value;
    }
    private IUsage? _usage;
    public IUsage Usage
    {
        get => _usage ??= new UnlimitedUsage();
        set => _usage = value;
    }

    public List<Trigger> Triggers { get; set; } = [];

    public StatusDefinition Clone()
    {
        return new StatusDefinition()
        {
            Id = Id,
            Name = Name,
            IsStackable = IsStackable,
            Duration = Duration.Clone(),
            Usage = Usage.Clone(),
            Triggers = [.. Triggers.Select(t => t.Clone())]
        };
    }
}