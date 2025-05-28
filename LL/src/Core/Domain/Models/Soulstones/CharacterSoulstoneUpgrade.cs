using Domain.Models.Entities.Characters;

namespace Domain.Models.Soulstones;
public class CharacterSoulstoneUpgrade
{
    public Guid CharacterId { get; init; }
    public string SoulstoneUpgradeDefinitionId { get; init; } = string.Empty;

    public int Level { get; set; }

    public Character Character { get; init; } = null!;
}
