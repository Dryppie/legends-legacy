using Domain.Models.Items;

namespace Domain.Models.Items.Equipments.Tools;

public static class ToolInstanceNaming
{
    public static string GetDisplayName(string baseName, Rarity rarity)
    {
        var trimmedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? "Tool"
            : baseName.Trim();

        return rarity switch
        {
            Rarity.Common => $"Plain {trimmedBaseName}",
            Rarity.Uncommon => $"Sturdy {trimmedBaseName}",
            Rarity.Rare => $"Proven {trimmedBaseName}",
            Rarity.Epic => $"Exquisite {trimmedBaseName}",
            Rarity.Unique => $"Fabled {trimmedBaseName}",
            Rarity.Legendary => $"Mythic {trimmedBaseName}",
            Rarity.Legacy => $"Eternal {trimmedBaseName}",
            _ => trimmedBaseName
        };
    }
}
