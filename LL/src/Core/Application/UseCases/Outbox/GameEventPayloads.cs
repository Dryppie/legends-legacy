using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Combat;
using Domain.Models.Items;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Inventories.Dtos;
using System.Text.Json;

namespace Application.UseCases.Outbox;

public sealed record EquipmentChangedPayload(Guid CharacterId);

public sealed record EssenceAbsorbedPayload(
    Guid CharacterId,
    string EssenceDefinitionId,
    int UniqueEssenceCount,
    IReadOnlyCollection<string> CompletedCollectionKeys);

public sealed record EssenceLoadoutChangedPayload(
    Guid CharacterId,
    IReadOnlyCollection<Guid> AttunedPlayerEssenceIds,
    int EquippedEssenceCount,
    bool HasCompatibleEssenceTrio = false);

public sealed record EssenceFocusSetPayload(
    Guid CharacterId,
    string CreatureDefinitionId);

public sealed record FocusedCreatureEssenceReceivedPayload(
    Guid CharacterId,
    string CreatureDefinitionId,
    string EssenceDefinitionId);

public sealed record EssenceAscendedPayload(
    Guid CharacterId,
    int AscensionTier,
    int AscendedToTierCount);

public sealed record EquipmentCraftedPayload(
    Guid CharacterId,
    IReadOnlyCollection<OutboxEquipmentItemPayload> CraftedItems,
    int CraftingMasteryLevel);

public sealed record EquipmentTemperedPayload(
    Guid CharacterId,
    TemperingSummary Summary,
    IReadOnlyCollection<OutboxEquipmentItemPayload> CompletedItems);

public sealed record BlueprintUnlockedPayload(Guid CharacterId);

public sealed record IdleCombatEncounterCompletedPayload(
    Guid CharacterId,
    string AreaId,
    bool WonEncounter,
    int MonstersDefeated,
    IReadOnlyCollection<string> DefeatedCreatureFamilyKeys,
    int PlayerDefeats,
    int? LowestWinningHealthPercent,
    int ActionCount,
    string? EquippedGatheringType,
    int? WinningEncounterCount = null);

public sealed record CharacterCreatedPayload(Guid CharacterId);

/// <summary>
/// Carries everything the realtime level-up broadcast needs so it can be sent after the
/// originating transaction commits. The trailing values are optional so outbox rows queued by
/// an older build still deserialize.
/// </summary>
public sealed record CharacterLevelReachedPayload(
    Guid CharacterId,
    int Level,
    long Experience = 0,
    long ExperienceUntilNextLevel = 0,
    int UnlockedEssenceSlots = 0);

public sealed record DungeonRunStartedPayload(Guid CharacterId);

public sealed record DungeonRunCompletedPayload(
    Guid CharacterId,
    string DungeonDefinitionId,
    bool CompletedWithoutDefeat,
    bool CompletedWithoutRetreat,
    bool CompletedWithoutWeapon,
    IReadOnlyCollection<string> DefeatedBossKeys);

public sealed record ColosseumBattleCompletedPayload(
    Guid CharacterId,
    Guid OpponentCharacterId,
    BattleOutcome Outcome,
    int CharacterRatingBefore,
    int OpponentRatingBefore);

public sealed record TournamentBattleCompletedPayload(
    Guid CharacterId,
    Guid TournamentId,
    Guid MatchId);

public sealed record TournamentChatAnnouncementPayload(
    Guid TournamentId,
    Guid MessageId,
    string Body,
    string TargetUrl,
    DateTimeOffset SentAt);

public sealed record WorldTowerChatAnnouncementPayload(
    Guid RallyId,
    Guid MessageId,
    string Body,
    string TargetUrl,
    DateTimeOffset SentAt);

public sealed record RaidChatAnnouncementPayload(
    Guid RaidRunId,
    Guid MessageId,
    string Body,
    string TargetUrl,
    DateTimeOffset SentAt);

public sealed record RegionBossChatAnnouncementPayload(
    Guid RegionBossEventId,
    Guid MessageId,
    string Body,
    string TargetUrl,
    DateTimeOffset SentAt);

public sealed record RaidChatChannelSnapshotPayload(
    Guid RaidRunId,
    long Revision,
    bool IsOpen,
    IReadOnlyCollection<Guid> MemberCharacterIds,
    DateTimeOffset UpdatedAt,
    RaidChatLifecycleMessagePayload? LifecycleMessage = null);

public sealed record RaidChatLifecycleMessagePayload(
    Guid MessageId,
    string Body,
    DateTimeOffset SentAt);

public sealed record EventQuestChatAnnouncementPayload(
    string EventQuestId,
    Guid MessageId,
    string Body,
    string TargetUrl,
    DateTimeOffset SentAt);

public sealed record ProphecyCompletedPayload(
    Guid CharacterId,
    Guid ProphecyId,
    string Scope);

public sealed record PlayerTransferChatMessagePayload(
    Guid TransferId,
    Guid MessageId,
    Guid TargetCharacterId,
    string Body,
    DateTimeOffset SentAt);

public sealed record GuildChatMessagePayload(
    Guid GuildId,
    Guid ActorCharacterId,
    string ActorName,
    string Body,
    Guid MessageId,
    DateTimeOffset SentAt);

public sealed record GuildVaultChatMessagePayload(
    Guid GuildId,
    Guid ActorCharacterId,
    string ActorName,
    string Body,
    EquipmentInstanceDto Equipment,
    Guid MessageId,
    DateTimeOffset SentAt);

public sealed record GuildMissionSelectedPayload(
    Guid GuildId,
    Guid? ActorCharacterId = null,
    bool InitiatorHandled = false);
public sealed record GuildMissionProgressedPayload(
    Guid GuildId,
    Guid? ActorCharacterId = null);

public sealed record RealtimeAudiencePayload(
    string Kind,
    Guid? TargetId,
    IReadOnlyList<Guid>? CharacterIds);

public sealed record RealtimeDeliveryRequestedPayload(
    RealtimeAudiencePayload Audience,
    string EventName,
    JsonElement Payload,
    string Sender);

public sealed record InventoryItemsGrantedPayload(
    Guid GrantId,
    Guid CharacterId,
    IReadOnlyList<InventoryItemDto> Items,
    string Source,
    string? Location);

public sealed record OutboxEquipmentItemPayload(
    string ItemBaseId,
    int Tier,
    Rarity Rarity,
    ItemQuality Quality,
    int? Potential,
    string? BaseRecipeId,
    string? BlueprintId,
    IReadOnlyCollection<string> AffinityTags,
    bool IsMasterpiece);
