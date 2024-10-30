using Domain.Models.Abilities.Effects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Abilities;
[NotMapped]
public class Ability
{
    public string Id { get; set; } = string.Empty; // Unique identifier
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AbilityType Type { get; set; } // Active or Passive
    public int Cooldown { get; set; }
    public int RemainingTimeUntilUse { get; set; }
    public int Cost { get; set; } // e.g., mana cost
    public string ActivationLog { get; set; } = "{Actor} used {Ability} on {Target}.";

    public List<Effect> Effects { get; set; } = [];
}