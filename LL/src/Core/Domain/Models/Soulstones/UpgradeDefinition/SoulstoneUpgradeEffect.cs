namespace Domain.Models.Soulstones.UpgradeDefinition;

public enum SoulstoneUpgradeEffectKind
{
    EssenceDropRateRelativeBps,
    EssencePityProgressionGainBps,
    DuplicateEssenceExtraMaterialChanceBps,
    FocusedMonsterEssenceDropRateRelativeBps,
    CombatExperienceGainBps,
    IdleCombatDefeatExperienceRetentionBps,
    DungeonSigilDropRateRelativeBps,
    DungeonRewardRetentionBps
}

public enum SoulstoneUpgradeEffectUnit
{
    RelativeBasisPoints,
    AdditiveMultiplierBasisPoints,
    ChanceBasisPoints,
    PercentagePointBasisPoints,
    FlatValue,
    Unlock
}

public sealed record SoulstoneUpgradeEffect(
    SoulstoneUpgradeEffectKind Kind,
    SoulstoneUpgradeEffectUnit Unit,
    IReadOnlyList<int> ValuesByRank);
