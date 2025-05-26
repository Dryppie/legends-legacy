namespace Domain.Models.Professions.Crafting;
public class TemperingSession
{
    public int ArmorForgingExperience { get; set; }
    public int JewelryCraftingExperience { get; set; }
    public int WeaponSmithingExperience { get; set; }
    public int TotalExperience =>
        ArmorForgingExperience + JewelryCraftingExperience + WeaponSmithingExperience;
}