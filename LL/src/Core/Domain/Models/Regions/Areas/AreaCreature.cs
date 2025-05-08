namespace Domain.Models.Regions.Areas;
public class AreaCreature
{
    public string AreaId { get; set; } = string.Empty;
    public Guid CreatureId { get; set; }
    /// <summary>
    /// Chance of this select creature to spawn
    /// </summary>
    public float WeightedSpawnRate { get; set; }
}
