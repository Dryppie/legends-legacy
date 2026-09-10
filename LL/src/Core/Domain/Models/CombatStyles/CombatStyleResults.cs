namespace Domain.Models.CombatStyles;

public sealed record CombatStyleOperationResult(bool Succeeded, string Message);
public sealed record CombatStyleEntry(CombatStyleDefinition Definition, int Level, long CurrentXp,
    long XpRequired, int UpgradeSlots, string? RefinementId, IReadOnlyList<string> UpgradeIds,
    string? MasteredUpgradeId = null);
public sealed record ChanneledEssenceOption(Guid PlayerEssenceId, string EssenceDefinitionId, string Name,
    string AbilityId, int CooldownTicks, bool IsEligible, IReadOnlyList<string> EligibleEffectIds);
public sealed record CombatStylePreviewFact(string Label, string Value, string? Condition = null);
public sealed record CombatStyleOverview(string ContentVersion, IReadOnlyList<CombatStyleEntry> Styles,
    CombatStyleSelectionRequest Selection, CombatStyleSnapshot? EffectiveStyle, string? ValidationIssue,
    IReadOnlyList<CombatStylePreviewFact> PreviewFacts);
