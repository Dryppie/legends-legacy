namespace API.LiveOps.Support;

public sealed record EquipmentSupportSnapshotDto(
    int RowLimit,
    int EquipmentCount,
    IReadOnlyList<EquipmentSupportItemDto> Items)
{
    public EquipmentSupportDungeonRunDto? DungeonRun { get; init; }
}

public sealed record EquipmentSupportDungeonRunDto(
    Guid RunId, string DungeonId, string Name, string Status, int CurrentRoomIndex,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? CompletedAtUtc, DateTimeOffset? RewardsClaimedAtUtc,
    int RewardRowCount, IReadOnlyList<EquipmentSupportRunRewardDto> RewardRows);

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
