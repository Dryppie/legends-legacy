using Domain.Models.CombatStyles;

namespace Application.UseCases.CombatStyles.Dtos;

/// <summary>Current effective-style response, separate from persisted battle snapshot serialization.</summary>
public sealed record CombatStyleSnapshotDto(string CombatStyleId, CombatStyleKind Kind,
    string ContentVersion, int Level, string? RefinementId, IReadOnlyList<string> UpgradeIds,
    string? MasteredUpgradeId, Guid? ChanneledPlayerEssenceId, string? ChanneledEssenceDefinitionId,
    CombatStyleTuningDto Tuning, CombatStyleMilestoneTuning MilestoneTuning);
