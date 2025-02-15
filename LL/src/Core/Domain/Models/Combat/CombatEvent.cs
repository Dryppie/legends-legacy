using Domain.Models.Attributes;

namespace Domain.Models.Combat;
public class CombatEvent
{
    public int Timestamp { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public AttributeType Attribute { get; set; }
    public int Magnitude { get; set; }
    public string Details { get; set; } = string.Empty;
    public SimpleCombatEntity? CombatEntity { get; set; }
}