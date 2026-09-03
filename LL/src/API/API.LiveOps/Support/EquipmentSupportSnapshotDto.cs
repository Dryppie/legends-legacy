namespace API.LiveOps.Support;

public sealed record EquipmentSupportSnapshotDto(
    int RowLimit,
    int EquipmentCount,
    int PendingRewardCount,
    bool ProgressTruncated,
    IReadOnlyList<EquipmentSupportItemDto> Items,
    IReadOnlyList<EquipmentSupportPendingRewardDto> PendingRewards,
    IReadOnlyList<EquipmentSupportProtectionDto> Protection,
    IReadOnlyList<EquipmentSupportOrdinaryDto> Ordinary)
{
    public EquipmentSupportDungeonRunDto? DungeonRun { get; init; }
}

public sealed record EquipmentSupportDungeonRunDto(
    Guid RunId, string DungeonId, string Name, string Status, int CurrentRoomIndex,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc, DateTimeOffset? RewardsClaimedAtUtc,
    EquipmentSupportCommitmentDto? Commitment, EquipmentSupportReceiptDto? Receipt,
    int RewardRowCount, IReadOnlyList<EquipmentSupportRunRewardDto> RewardRows);

public sealed record EquipmentSupportCommitmentDto(
    Guid CharacterId, Guid RunId, string DungeonId, string PoolId, int Difficulty,
    double MatchingChance, int GuaranteeCompletions,
    EquipmentSupportItemDto? Target);

public sealed record EquipmentSupportReceiptDto(
    Guid RunId, string PoolId, DateTimeOffset SecuredAtUtc, DateTimeOffset? ClaimedAtUtc,
    int PreviousProgress, int Progress, EquipmentSupportItemDto? Equipment);

public sealed record EquipmentSupportRunRewardDto(
    Guid RewardRowId, string ItemBaseId, string Name, string ItemType, int Quantity, string Source,
    EquipmentSupportItemDto? Equipment);

public sealed record EquipmentSupportItemDto(
    Guid InstanceId, string ItemBaseId, string Name, IReadOnlyList<string> Locations,
    EquipmentSupportDescriptorDto? Progression);

public sealed record EquipmentSupportDescriptorDto(
    string DefinitionId, string ArchetypeId, int Tier, int Rank, int BalanceVersion,
    string Rarity, string? NativeStyleId, string? ActiveStyleId,
    string Ownership, Guid OwnerId, string AwardKind, string SourceId, string AwardId);
public sealed record EquipmentSupportPendingRewardDto(Guid RunId, string PoolId, DateTimeOffset SecuredAtUtc,
    EquipmentSupportItemDto? Equipment);
public sealed record EquipmentSupportProtectionDto(string PoolId, string? TargetDefinitionId, int CompletionsWithoutMatch, long Revision);
public sealed record EquipmentSupportOrdinaryDto(string PoolId, bool HasEnteredRegion, string? TargetDefinitionId,
    int PlainVictories, int? RequiredPlainVictories, string? SigilFamilyId, int SigilVictories,
    int? RequiredSigilVictories, long Revision, DateTimeOffset? LastEncounterAtUtc);
