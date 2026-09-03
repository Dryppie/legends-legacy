namespace Domain.Models.Regions.Areas;
public class Area
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int LevelRequirement { get; set; }
    public int DifficultyTier { get; set; }
    public string? RequiredActiveQuestId { get; set; }
    public string? RequiredCompletedQuestId { get; set; }
    public int? RequiredTowerFloor { get; set; }
    public bool HideWhenLocked { get; set; }
    public ICollection<AreaCreature> Creatures { get; set; } = [];
    /// <summary>
    /// Chance of spawning n. of creatures 
    /// </summary>
    public List<float> SpawnProbabilities { get; set; } = [];
}
