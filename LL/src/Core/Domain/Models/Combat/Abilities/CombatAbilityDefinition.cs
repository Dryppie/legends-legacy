using Domain.Interfaces.Combat.Abilities;
using Domain.Models.Combat.Abilities.Effects.Actions;
using Domain.Models.Combat.Abilities.Effects.Conditions;
using Domain.Models.Combat.Abilities.Effects.Usages;
using Domain.Models.Combat.Abilities.ResourceCosts;
using Domain.Models.Combat.Abilities.Triggers;
using Domain.Models.Damages;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Combat.Abilities;
[NotMapped]
public class CombatAbilityDefinition
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
    public CombatAbilityType Type { get; set; } // Active or Passive
    public int Cooldown { get; set; }
    public List<Trigger> Triggers { get; set; } = [];
    // If it's a summon ability, don't say who the ability is used on.
    public string ActivationLog => Triggers.All(e => e.Actions.All(a => a.Action is SummonAction)) ? "{Actor} used {Ability}." :  "{Actor} used {Ability} on {Target}.";
    // For the frontend only
    public IReadOnlyCollection<AttackType> AttackTypes { get; set; } = [];
    public IReadOnlyCollection<DamageType> DamageTypes { get; set; } = [];
    public IReadOnlyCollection<EffectTag> EffectTags { get; set; } = [];

    public CombatAbilityDefinition Clone()
    {
        // Create a new instance
        var copy = new CombatAbilityDefinition
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Type = Type,
            Cooldown = Cooldown,
            Triggers = [.. Triggers.Select(t => t.Clone())],
            Usage = Usage.Clone(),
            Condition = Condition.Clone(),
        };

        return copy;
    }
}