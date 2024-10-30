using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;
using Domain.Models.Entities;

namespace Domain.Models.Abilities.Effects;
public class EffectContext
{
    public Entity Owner { get; set; }   // Entity with the effect
    public Entity Target { get; set; }   // Other entity involved
    public TriggerEvent TriggerEvent { get; set; }
    public EventType EffectType { get; set; }
    public AttackOutcome AttackOutcome { get; set; }
    public int Magnitude { get; set; }
    public bool IsFlatAmount { get; set; }
    public int TimeStamp { get; set; }
    public string Details { get; set; } = string.Empty;
}