namespace Domain.Models.Combat;
public class CombatLogItem
{
    public string Source { get; set; } = string.Empty;
    public int Timestamp { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public int Magnitude { get; set; }
    public string Details { get; set; } = string.Empty;
    public SimpleCombatEntity? CombatEntity { get; set; }
}