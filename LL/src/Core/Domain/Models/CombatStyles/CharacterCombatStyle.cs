using Domain.Models.Entities.Characters;

namespace Domain.Models.CombatStyles;

public sealed class CharacterCombatStyle
{
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public string CombatStyleId { get; set; } = string.Empty;
    public int Level { get; set; }
    public long CurrentXp { get; set; }
    public string? RefinementId { get; set; }
    public string[] UpgradeIds { get; set; } = [];
    public string? MasteredUpgradeId { get; set; }
    public Guid? FocusPlayerEssenceId { get; set; }
}
