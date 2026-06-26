namespace Domain.Models.Professions.Crafting;

public class CharacterRecipeMastery
{
    public Guid CharacterId { get; set; }
    public string RecipeId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
