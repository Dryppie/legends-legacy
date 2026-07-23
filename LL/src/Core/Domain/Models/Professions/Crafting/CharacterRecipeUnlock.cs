namespace Domain.Models.Professions.Crafting;

public class CharacterRecipeUnlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public string BlueprintId { get; set; } = string.Empty;
    public DateTimeOffset UnlockedAt { get; set; } = DateTimeOffset.UtcNow;
}
