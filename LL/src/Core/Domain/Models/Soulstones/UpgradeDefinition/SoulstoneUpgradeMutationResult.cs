namespace Domain.Models.Soulstones.UpgradeDefinition;

public sealed record SoulstoneUpgradeMutationResult(
    IReadOnlyList<SoulstoneUpgradeView> Upgrades,
    long Soulstones,
    int RefundedSoulstones = 0);
