using Domain.Models.Abilities.Effects;
using Domain.Models.Abilities.ResourceCosts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Abilities;
[NotMapped]
public class AbilityDefinition
{
    public string Id { get; set; } = string.Empty; // Unique identifier
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AbilityType Type { get; set; } // Active or Passive
    public int Cooldown { get; set; }
    public int RemainingTimeUntilUse { get; set; }
    /// <summary>
    /// How much of a resource will be deducted upon use
    /// </summary>
    public int Cost { get; set; }
    /// <summary>
    /// What resource will be deducted from upon use
    /// </summary>
    public ResourceType ResourceTypeCost { get; set; }
    public string ActivationLog { get; set; } = "{Actor} used {Ability} on {Target}.";

    public List<EffectDefinition> Effects { get; set; } = [];
}