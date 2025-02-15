namespace Domain.Models.Regions.Areas;
public class Area
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<AreaCreature> Creatures { get; set; } = [];
    public List<float> SpawnProbabilities { get; set; } = [];
}
