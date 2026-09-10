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
    // Retained for persisted legacy rows; current saves clear this and battles derive the Channeled Essence from slot order.
    public Guid? ChanneledPlayerEssenceId { get; set; }
}

public sealed record CombatStyleSelectionRequest(
    string? CombatStyleId,
    string? RefinementId,
    IReadOnlyList<string> UpgradeIds,
    Guid? ChanneledPlayerEssenceId = null,
    bool RestoreRememberedChoices = false,
    string? MasteredUpgradeId = null);
