namespace Domain.Models.Soulstones.UpgradeDefinition;

public sealed record SoulstoneUpgradeView(
    string Id,
    SoulstoneUpgradeBranch Branch,
    string DisplayName,
    string Description,
    int CurrentRank,
    int MaxRank,
    string CurrentEffectText,
    string? NextEffectText,
    int? NextCost,
    bool CanPurchase,
    string? DisabledReason,
    IReadOnlyList<string> AppliesTo,
    IReadOnlyList<string> DoesNotApplyTo,
    int RefundValue,
    int SortOrder,
    string? FrontendHint);
