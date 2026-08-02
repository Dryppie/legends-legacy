namespace Domain.Models.Essences;

public class PlayerEssence
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string EssenceDefinitionId { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int CurrentXp { get; set; }
    public int AscensionTier { get; set; }
    public bool IsEvolved { get; set; }
    public DateTimeOffset? EvolutionUnlockedAt { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset AbsorbedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
