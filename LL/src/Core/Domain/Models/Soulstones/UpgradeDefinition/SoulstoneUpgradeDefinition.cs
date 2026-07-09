namespace Domain.Models.Soulstones.UpgradeDefinition;

public sealed record SoulstoneUpgradeDefinition(
    string Id,
    SoulstoneUpgradeBranch Branch,
    string DisplayName,
    string Description,
    int MaxRank,
    IReadOnlyList<int> CostsByRank,
    IReadOnlyList<SoulstoneUpgradeEffect> Effects,
    IReadOnlyList<string> AppliesTo,
    IReadOnlyList<string> DoesNotApplyTo,
    bool Enabled = true,
    int SortOrder = 0,
    string? IconKey = null,
    IReadOnlyList<string>? RequiresUpgradeIds = null,
    IReadOnlyList<SoulstoneRegionRankCap>? RegionRankCaps = null,
    string? FrontendHint = null,
    bool IsConvenienceUpgrade = false);
