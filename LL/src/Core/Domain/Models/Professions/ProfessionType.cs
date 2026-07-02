namespace Domain.Models.Professions;
public enum ProfessionType
{
    None = 0,

    // Crafting
    Crafting = 1,
    [Obsolete("Use Crafting.")]
    JewelryCrafting = 2,
    [Obsolete("Use Crafting.")]
    WeaponSmithing = 3,

    // Gathering
    Mining = 4,
    Woodcutting = 5,
    Fishing = 6,
    Skinning = 7,
}
