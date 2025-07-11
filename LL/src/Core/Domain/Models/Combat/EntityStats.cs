namespace Domain.Models.Combat;
public class EntityStats
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty; // Optional, but helpful for UI
    public List<AbilityStats> Abilities { get; set; } = [];
}
