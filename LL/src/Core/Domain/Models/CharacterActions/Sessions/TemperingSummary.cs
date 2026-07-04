namespace Domain.Models.CharacterActions.Sessions;
public class TemperingSummary
{
    public int TotalItemsCrafted { get; set; }
    public int Masterpieces { get; set; }
    public int LevelingItems { get; set; }
    public int CursedOutcomes { get; set; }
    public int QualityIncreases { get; set; }
    public int TotalActions { get; set; }
    public int TotalSoulstones { get; set; } = 0;
    public int CraftingExperience { get; set; }
    public int TotalExperience => CraftingExperience;
}
