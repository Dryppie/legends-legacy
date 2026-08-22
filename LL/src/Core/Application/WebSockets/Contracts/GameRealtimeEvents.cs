using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Inventories.Dtos;
using Application.UseCases.MarketPlaces.Dtos.Responses;
using Application.UseCases.Quests.Dtos;
using Application.UseCases.WorldTower.Dtos;

namespace Application.WebSockets.Contracts;

public abstract record GameRealtimeEvent;

public sealed record LootReceived(
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Source,
    string? Location,
    Guid? GrantId = null) : GameRealtimeEvent;

public sealed record StateInvalidated(
    Guid? CharacterId,
    string Scope,
    long Revision,
    string Reason) : GameRealtimeEvent;

public sealed record StateInvalidations(
    Guid CharacterId,
    IReadOnlyDictionary<string, long> Revisions,
    string Reason) : GameRealtimeEvent;

public sealed record AccountAccessChanged(
    Guid AccountId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record CharacterLevelUp(
    Guid CharacterId,
    int Level,
    long Experience,
    long ExperienceUntilNextLevel,
    int UnlockedEssenceSlots) : GameRealtimeEvent;

public sealed record MarketplaceChanged(
    MarketplaceChangeSetDto Changes) : GameRealtimeEvent;

public sealed record GuildApplication(
    Guid GuildId,
    Guid PlayerId) : GameRealtimeEvent;

public sealed record GuildInviteReceived(
    Guid GuildId,
    Guid CharacterId) : GameRealtimeEvent;

public sealed record GuildInviteRejected(
    Guid GuildId,
    Guid CharacterId) : GameRealtimeEvent;

public sealed record GuildApplicationRejected(
    Guid GuildId,
    Guid CharacterId) : GameRealtimeEvent;

public sealed record GuildBuildingsChanged(
    Guid GuildId,
    string BuildingId,
    Guid? ActorCharacterId = null,
    bool InitiatorHandled = false) : GameRealtimeEvent;

public sealed record GuildMissionsChanged(
    Guid GuildId,
    Guid? ActorCharacterId = null,
    bool InitiatorHandled = false) : GameRealtimeEvent;

public sealed record GuildStateChanged(
    Guid GuildId,
    Guid? ActorCharacterId = null,
    bool InitiatorHandled = false) : GameRealtimeEvent;

public sealed record GuildVaultChatMessage(
    Guid GuildId,
    Guid MessageId,
    Guid ActorCharacterId,
    string ActorName,
    string Action,
    EquipmentInstanceDto Equipment,
    DateTimeOffset SentAt) : GameRealtimeEvent;

public sealed record GuildMembershipChanged(
    Guid GuildId,
    Guid CharacterId,
    Guid? ActorCharacterId = null,
    bool InitiatorHandled = false) : GameRealtimeEvent;

public sealed record GuildDisbanded(
    Guid GuildId,
    Guid? ActorCharacterId = null,
    bool InitiatorHandled = false) : GameRealtimeEvent;

public sealed record GuildDirectoryChanged(
    string Reason,
    Guid? ActorCharacterId = null) : GameRealtimeEvent;

public sealed record QuestJournalChanged(
    QuestJournalDto Journal,
    long StateVersion) : GameRealtimeEvent;

public sealed record EventQuestChanged(
    string EventQuestId,
    DateTimeOffset UpdatedAt) : GameRealtimeEvent;

public sealed record ArenaBattleCompleted(
    Guid CharacterId,
    Guid EnemyId,
    string Outcome,
    int CharacterRatingBefore,
    int CharacterRatingAfter,
    int EnemyRatingBefore,
    int EnemyRatingAfter) : GameRealtimeEvent;

public sealed record ProphecyProgressed(
    Guid CharacterId,
    Guid ProphecyId,
    string Title,
    string Scope,
    string SlotType,
    string Status,
    int CurrentValue,
    int TargetValue,
    int AmountGained,
    bool Completed) : GameRealtimeEvent;

public sealed record AchievementUnlocked(
    Guid? CharacterId,
    string AchievementKey,
    string AchievementName,
    int Points,
    string? TitleKey,
    string? TitleName,
    string Message,
    bool IsGlobal) : GameRealtimeEvent;

public sealed record PlayerTransfer(
    Guid TransferId,
    Guid MessageId,
    Guid CharacterId,
    string Message) : GameRealtimeEvent;

public sealed record StateSyncCheckpoint(
    Guid CharacterId,
    IReadOnlyDictionary<string, long> Revisions,
    DateTimeOffset ServerTimeUtc);

public sealed record TournamentGroundsUpdated(
    Guid TournamentId,
    long StateVersion,
    int TournamentNumber,
    string TournamentName,
    string Event,
    string Status,
    int RegisteredParticipantCount,
    int MinParticipants,
    int MaxParticipants,
    bool HasBracket,
    int? CurrentRoundNumber,
    DateTimeOffset? NextActionAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record WorldTowerRallyUpdated(
    Guid RallyId,
    int FloorNumber,
    string Event,
    string Status,
    int ParticipantCount,
    int RequiredSlots,
    int PendingApplicationCount,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record RaidUpdated(
    Guid RaidRunId,
    string RaidBossId,
    string Event,
    string Status,
    int SignupCount,
    long Version,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record RaidDirectoryUpdated(
    Guid RaidRunId,
    string RaidBossId,
    string Event,
    string Status,
    int SignupCount,
    long Version,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record RegionBossUpdated(
    Guid EventId,
    string RegionBossDefinitionId,
    string Event,
    string Status,
    DateTimeOffset OccurredAtUtc) : GameRealtimeEvent;

public sealed record WorldTowerCombatFrameUpdated(
    Guid AttemptId,
    Guid RallyId,
    DateTimeOffset PlaybackStartedAt,
    int TicksPerSecond,
    int TicksPerFrame,
    TowerCombatFrameDto Frame) : GameRealtimeEvent;
