namespace Domain.Models.CharacterActions.Sessions;
public class TemperingSummary
{
    public int TotalItemsCrafted { get; set; }
    public int Masterpieces { get; set; }
    public int LevelingItems { get; set; }
    public int TotalActions { get; set; }
    public int ArmorForgingExperience { get; set; }
    public int JewelryCraftingExperience { get; set; }
    public int WeaponSmithingExperience { get; set; }
    public int TotalExperience =>
        ArmorForgingExperience + JewelryCraftingExperience + WeaponSmithingExperience;
}