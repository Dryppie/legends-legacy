namespace API.LiveOps.Support;

public sealed record PlayerSupportSection<T>(
    bool IsAvailable,
    string Source,
    DateTimeOffset FetchedAtUtc,
    string? Message,
    T? Data);

public sealed record PlayerSupportSnapshotDto(
    Guid AccountId,
    Guid CharacterId,
    DateTimeOffset GeneratedAtUtc,
    PlayerSupportSection<AccountSupportSnapshotDto> Account,
    PlayerSupportSection<ActivitySupportSnapshotDto> Activity,
    PlayerSupportSection<EconomySupportSnapshotDto> Economy,
    PlayerSupportSection<GuildSupportSnapshotDto> Guild,
    PlayerSupportSection<MarketplaceSupportSnapshotDto> Marketplace,
    PlayerSupportSection<TransferHistorySupportSnapshotDto> Transfers,
    PlayerSupportSection<SynchronizationSupportSnapshotDto> Synchronization);

public sealed record AccountSupportSnapshotDto(
    DateTime AccountCreatedUtc,
    DateTime? LastSessionIssuedUtc,
    int ActiveSessionCount,
    string LoginActivityMessage,
    IReadOnlyList<AccountRestrictionHistoryDto> Restrictions);

public sealed record AccountRestrictionHistoryDto(
    Guid Id,
    string Type,
    string Status,
    string Reason,
    string CreatedBySubject,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    string? RevokedBySubject,
    DateTimeOffset? RevokedAt,
    string? RevocationReason);

public sealed record ActivitySupportSnapshotDto(
    string CurrentAction,
    string? ActionDetailType,
    DateTimeOffset? LastActionMutationAtUtc,
    DateTimeOffset? NextResolutionAtUtc,
    DateTimeOffset? BlockedUntilUtc,
    long? ScheduleGeneration,
    string ActivityMessage);

public sealed record EconomySupportSnapshotDto(
    long Cinders,
    long Soulstones,
    long FateEcho,
    long SigilFragments,
    long GuildFavor,
    long TowerTokens,
    int InventoryRowCount,
    long InventoryQuantity,
    int UnseenInventoryRows,
    IReadOnlyList<RecentInventoryAcquisitionDto> RecentAcquisitions,
    IReadOnlyList<RecentCompensationGrantDto> RecentCompensationGrants);

public sealed record RecentInventoryAcquisitionDto(
    Guid ItemInstanceId,
    string ItemBaseId,
    string ItemName,
    int Quantity,
    string AcquisitionSource,
    DateTimeOffset AcquiredAtUtc);

public sealed record RecentCompensationGrantDto(
    Guid OperationId,
    string ItemBaseId,
    string ItemName,
    int Quantity,
    string Reason,
    string RiskLevel,
    DateTimeOffset OccurredAtUtc);

public sealed record GuildSupportSnapshotDto(
    bool IsMember,
    Guid? GuildId,
    string? GuildName,
    string? GuildTag,
    string? Role,
    DateTimeOffset? JoinedAtUtc,
    int? GuildLevel,
    int? MemberCount);

public sealed record MarketplaceSupportSnapshotDto(
    int ActiveListingCount,
    int ActiveBuyOrderCount,
    IReadOnlyList<RecentMarketplaceTradeDto> RecentTrades);

public sealed record RecentMarketplaceTradeDto(
    Guid OrderId,
    string Direction,
    string ItemBaseId,
    string ItemName,
    int Quantity,
    long TotalPrice,
    DateTimeOffset PurchasedAtUtc);

public sealed record TransferHistorySupportSnapshotDto(
    int HistoryLimit,
    IReadOnlyList<PlayerTransferHistoryDto> Entries,
    string? NextCursor);

public sealed record TransferHistoryLookupResult(
    bool PlayerFound,
    bool CursorValid,
    PlayerSupportSection<TransferHistorySupportSnapshotDto>? Section);

public sealed record PlayerTransferHistoryDto(
    Guid TransferId,
    string Direction,
    string Kind,
    Guid SenderAccountId,
    Guid SenderCharacterId,
    string SenderCharacterName,
    Guid RecipientAccountId,
    Guid RecipientCharacterId,
    string RecipientCharacterName,
    string AssetId,
    string AssetName,
    Guid? SourceItemInstanceId,
    Guid? DestinationItemInstanceId,
    long Quantity,
    DateTimeOffset OccurredAtUtc);

public sealed record SynchronizationSupportSnapshotDto(
    int PendingDeliveries,
    int FailedDeliveries,
    DateTimeOffset? OldestPendingAtUtc,
    DateTimeOffset? LastOutboxEventAtUtc,
    IReadOnlyList<StateRevisionDto> Revisions,
    string PendingRewardMessage);

public sealed record StateRevisionDto(
    string Scope,
    long Revision,
    DateTimeOffset UpdatedAtUtc);
