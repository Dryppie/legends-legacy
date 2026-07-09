namespace Domain.Models.Soulstones.UpgradeDefinition;

public enum SoulstoneUpgradeEffectKind
{
    EssenceDropRateRelativeBps,
    EssencePityProgressionGainBps,
    DuplicateEssenceExtraMaterialChanceBps,
    FocusedMonsterEssenceDropRateRelativeBps,
    CombatExperienceGainBps,
    AreaCommitmentCombatExperienceGainBps,
    IdleCombatDefeatExperienceRetentionBps,
    GatheringYieldBps,
    GatheringExperienceGainBps,
    GatheringRareDropChanceRelativeBps,
    CraftingExperienceGainBps,
    TemperingNegativeOutcomeReductionBps,
    TemperingFailMaterialRefundChanceBps,
    BlueprintProgressionGainBps,
    SigilFragmentDropRateRelativeBps,
    DungeonRewardRetentionBps,
    DungeonRoomPreviewTier,
    DungeonRewardFocusTier,
    ArchivePresetSlotCount
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
