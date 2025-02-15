namespace Domain.Models.Regions.Areas;
public class AreaCreature
{
    public string AreaId { get; set; } = string.Empty;
    public Guid CreatureId { get; set; }
    public float WeightedSpawnRate { get; set; }
}
