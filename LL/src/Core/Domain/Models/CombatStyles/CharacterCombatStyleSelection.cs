using Domain.Models.Entities.Characters;

namespace Domain.Models.CombatStyles;

/// <summary>The character's selected Combat Style and choices for every battle.</summary>
public sealed class CharacterCombatStyleSelection
{
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public string? CombatStyleId { get; set; }
    public string? RefinementId { get; set; }
    public string[] UpgradeIds { get; set; } = [];
    public string? MasteredUpgradeId { get; set; }
    public Guid? FocusPlayerEssenceId { get; set; }
}

public sealed record CombatStyleSelectionRequest(
    string? CombatStyleId,
    string? RefinementId,
    IReadOnlyList<string> UpgradeIds,
    Guid? FocusPlayerEssenceId,
    bool RestoreRememberedChoices = false,
    string? MasteredUpgradeId = null);
