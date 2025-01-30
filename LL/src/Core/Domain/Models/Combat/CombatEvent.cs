using Domain.Models.Attributes;

namespace Domain.Models.Combat;
public class CombatEvent
{
    public int Timestamp { get; set; }
    public Guid ActorId { get; set; }
    public Guid TargetId { get; set; }
    public EventType EventType { get; set; }
    public AttributeType Attribute { get; set; }
    public int Magnitude { get; set; }
    public string Details { get; set; } = string.Empty;
    public SimpleCombatEntity? CombatEntity { get; set; }
}