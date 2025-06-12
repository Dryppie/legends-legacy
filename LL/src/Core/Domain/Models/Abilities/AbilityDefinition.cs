using Domain.Interfaces.Abilities;
using Domain.Models.Abilities.Effects.Actions;
using Domain.Models.Abilities.Effects.Conditions;
using Domain.Models.Abilities.Effects.Usages;
using Domain.Models.Abilities.ResourceCosts;
using Domain.Models.Abilities.Triggers;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Abilities;
[NotMapped]
public class AbilityDefinition
{
    public string Id { get; set; } = string.Empty; // Unique identifier
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    private IUsage? _usage;
    public IUsage Usage
    {
        get => _usage ??= new UnlimitedUsage();
        set => _usage = value;
    }
    private ICondition? _condition;
    public ICondition Condition
    {
        get => _condition ??= new NoCondition();
        set => _condition = value;
    }
    public AbilityType Type { get; set; } // Active or Passive
    public int Cooldown { get; set; }
    public ResourceCost? Cost { get; set; }
    public List<Trigger> Triggers { get; set; } = [];
    // If it's a summon ability, don't say who the ability is used on.
    public string ActivationLog => Triggers.All(e => e.Actions.All(a => a.Action is SummonAction)) ? "{Actor} used {Ability}." :  "{Actor} used {Ability} on {Target}.";

    public AbilityDefinition Clone()
    {
        // Create a new instance
        var copy = new AbilityDefinition
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Type = Type,
            Cooldown = Cooldown,
            Cost = Cost,
            Triggers = [.. Triggers.Select(t => t.Clone())],
            Usage = Usage.Clone(),
            Condition = Condition.Clone(),
        };

        return copy;
    }
}