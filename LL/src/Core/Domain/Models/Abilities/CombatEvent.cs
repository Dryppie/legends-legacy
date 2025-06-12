using Domain.Models.Abilities.Effects.Trigger;
using Domain.Models.Combat;

namespace Domain.Models.Abilities;
public class CombatEvent
{
    public TriggerEvent Type { get; set; }
    public CombatEntity? Source { get; set; }
    public CombatEntity? Target { get; set; }
    public int CurrentTime { get; set; } = 0;
    // Optional event context
    public string? AbilityId { get; set; }
    public string? StatusId { get; set; }
}
