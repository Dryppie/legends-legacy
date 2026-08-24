using Domain.Models.Attributes;
using Domain.Models.Administration;
using Domain.Models.Achievements;
using Domain.Models.BackgroundJobs;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Economy;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Missions;
using Domain.Models.Guilds.Shop;
using Domain.Models.Inventories;
using Domain.Models.LootHistory;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.MarketPlaces;
using Domain.Models.Outbox;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Prophecies;
using Domain.Models.Quests;
using Domain.Models.Quests.Events;
using Domain.Models.Raids;
using Domain.Models.RegionBosses;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Domain.Models.Soulstones;
using Domain.Models.Synchronization;
using Domain.Models.Transfers;
using Domain.Models.Users;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common.Interfaces;
public interface IDbContext
{
    DbSet<AdminAction> AdminActions { get; }
    DbSet<AccountRestriction> AccountRestrictions { get; }
    DbSet<AccountRiskSnapshot> AccountRiskSnapshots { get; }
    DbSet<AccountRiskHistory> AccountRiskHistory { get; }
    DbSet<AccountRiskInvestigation> AccountRiskInvestigations { get; }
    DbSet<AccountRiskNote> AccountRiskNotes { get; }
    DbSet<AchievementDefinition> AchievementDefinitions { get; }
    DbSet<AchievementEventLedger> AchievementEventLedgers { get; }
    DbSet<PlayerAchievementProgress> PlayerAchievementProgresses { get; }
    DbSet<TitleDefinition> TitleDefinitions { get; }
    DbSet<PlayerTitleUnlock> PlayerTitleUnlocks { get; }
    DbSet<BackgroundJobExecution> BackgroundJobExecutions { get; }
    DbSet<Area> Areas { get; }
    DbSet<EntityAttribute> EntityAttributes { get; }
    //DbSet<Building> Buildings { get; }
    DbSet<CharacterArenaProfile> CharacterArenaProfiles { get; }
    DbSet<ArenaTicketStatus> ArenaTicketStatus { get; }
    DbSet<ColosseumMatchResult> ColosseumMatches { get; }
    DbSet<ArenaDefenseSnapshot> ArenaDefenseSnapshots { get; }
    DbSet<ChampionMarketPurchase> ChampionMarketPurchases { get; }
    DbSet<TournamentDefinition> TournamentDefinitions { get; }
    DbSet<TournamentInstance> ArenaTournaments { get; }
    DbSet<TournamentTeam> TournamentTeams { get; }
    DbSet<TournamentTeamApplication> TournamentTeamApplications { get; }
    DbSet<TournamentTeamInvite> TournamentTeamInvites { get; }
    DbSet<TournamentParticipant> TournamentParticipants { get; }
    DbSet<TournamentCombatSnapshot> TournamentCombatSnapshots { get; }
    DbSet<TournamentCombatReplay> TournamentCombatReplays { get; }
    DbSet<TournamentCombatReplayArtifact> TournamentCombatReplayArtifacts { get; }
    DbSet<TournamentRound> TournamentRounds { get; }
    DbSet<TournamentMatch> TournamentMatches { get; }
    DbSet<TournamentRewardGrant> TournamentRewardGrants { get; }
    DbSet<Character> Characters { get; }
    DbSet<CharacterSoulstoneUpgrade> CharacterSoulstoneUpgrades { get; }
    DbSet<CharacterQuestProgress> CharacterQuestProgresses { get; }
    DbSet<CharacterQuestObjectiveProgress> CharacterQuestObjectiveProgresses { get; }
    DbSet<QuestEventLedger> QuestEventLedgers { get; }
    DbSet<EventQuestInstance> EventQuestInstances { get; }
    DbSet<EventQuestObjectiveProgress> EventQuestObjectiveProgresses { get; }
    DbSet<EventQuestCharacterContribution> EventQuestCharacterContributions { get; }
    DbSet<EventQuestEventLedger> EventQuestEventLedgers { get; }
    DbSet<EventQuestRewardClaim> EventQuestRewardClaims { get; }
    DbSet<EventQuestMilestoneClaim> EventQuestMilestoneClaims { get; }
    DbSet<Creature> Creatures { get; }
    //DbSet<Echo> Echoes { get; }
    DbSet<Entity> Entities { get; }
    DbSet<EquipmentSlot> EquipmentSlots { get; }
    DbSet<EssenceItemBase> EssenceItems { get; }
    DbSet<PlayerEssence> PlayerEssences { get; }
    DbSet<EssenceLoadout> EssenceLoadouts { get; }
    DbSet<EssenceLoadoutSlot> EssenceLoadoutSlots { get; }
    DbSet<CreatureResonance> CreatureResonances { get; }
    DbSet<CharacterCreatureArchiveEntry> CharacterCreatureArchiveEntries { get; }

    DbSet<DungeonRun> DungeonRuns { get; }
    DbSet<RunReward> RunRewards { get; }
    DbSet<DungeonCompletionRecord> DungeonCompletionRecords { get; }
    DbSet<CharacterDungeonMastery> CharacterDungeonMasteries { get; }

    // Effects
    //DbSet<Modifier> Modifiers { get; }

    // Player Actions
    DbSet<CharacterAction> CharacterActions { get; }
    DbSet<ActionDetails> ActionDetails { get; }
    DbSet<CraftingQueueItem> CraftingQueueItems { get; }

    //DbSet<Equipment> Equipments { get; }
    DbSet<Guild> Guilds { get; }
    DbSet<GuildInvite> GuildInvites { get; }
    DbSet<GuildMember> GuildMembers { get; }
    DbSet<GuildBuilding> GuildBuildings { get; }
    DbSet<GuildActivityLog> GuildActivityLogs { get; }
    DbSet<GuildMissionOption> GuildMissionOptions { get; }
    DbSet<GuildMissionInstance> GuildMissionInstances { get; }
    DbSet<GuildMissionContribution> GuildMissionContributions { get; }
    DbSet<PersonalGuildOrder> PersonalGuildOrders { get; }
    DbSet<GuildMemberContributionPeriod> GuildMemberContributionPeriods { get; }
    DbSet<GuildContributionLedger> GuildContributionLedgers { get; }
    DbSet<GuildShopPurchase> GuildShopPurchases { get; }
    DbSet<GuildRolePermission> GuildRolePermissions { get; }
    DbSet<GuildVaultItem> GuildVaultItems { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<LootHistoryEntry> LootHistoryEntries { get; }
    DbSet<PlayerTransferRecord> PlayerTransferHistory { get; }
    DbSet<EconomyLedgerEntry> EconomyLedger { get; }
    DbSet<ItemBase> ItemBases { get; }
    DbSet<ItemInstance> ItemInstances { get; }
    DbSet<MarketPlaceListing> MarketPlaceListings { get; }
    DbSet<MarketPlaceBuyOrder> MarketPlaceBuyOrders { get; }
    DbSet<MarketPlaceOrder> MarketPlaceOrders { get; }
    DbSet<GameEventOutboxMessage> GameEventOutboxMessages { get; }
    DbSet<GameEventOutboxDelivery> GameEventOutboxDeliveries { get; }
    DbSet<StateSyncRevision> StateSyncRevisions { get; }
    //DbSet<Party> Parties { get; }
    //DbSet<PartyMember> PartyMembers { get; }
    DbSet<Profession> Professions { get; }
    //DbSet<Quest> Quests { get; }
    //DbSet<QuestStage> QuestStages { get; }
    //DbSet<Stat> Stats { get; }
    //DbSet<Title> Titles { get; }
    //DbSet<Town> Towns { get; }
    //DbSet<TownBuilding> TownBuildings { get; }
    DbSet<CharacterRecipeUnlock> CharacterRecipeUnlocks { get; }
    DbSet<CharacterRecipeMastery> CharacterRecipeMasteries { get; }
    DbSet<ProphecyDefinition> ProphecyDefinitions { get; }
    DbSet<PlayerProphecyInstance> PlayerProphecyInstances { get; }
    DbSet<WeeklyRevelationProgress> WeeklyRevelationProgress { get; }
    DbSet<DailyProphecyRerollState> DailyProphecyRerollStates { get; }
    DbSet<Region> Regions { get; }

    DbSet<RaidRun> RaidRuns { get; }
    DbSet<RaidSignup> RaidSignups { get; }
    DbSet<RaidLaneResult> RaidLaneResults { get; }
    DbSet<RaidPlayback> RaidPlaybacks { get; }
    DbSet<RaidPlaybackArtifact> RaidPlaybackArtifacts { get; }
    DbSet<RaidParticipantResult> RaidParticipantResults { get; }
    DbSet<RaidRewardClaim> RaidRewardClaims { get; }
    DbSet<RaidTrophyPurchase> RaidTrophyPurchases { get; }

    DbSet<RegionBossEvent> RegionBossEvents { get; }
    DbSet<RegionBossSignup> RegionBossSignups { get; }
    DbSet<RegionBossRun> RegionBossRuns { get; }
    DbSet<RegionBossParticipantResult> RegionBossParticipantResults { get; }
    DbSet<RegionBossPlayback> RegionBossPlaybacks { get; }
    DbSet<RegionBossPlaybackArtifact> RegionBossPlaybackArtifacts { get; }
    DbSet<RegionBossRewardGrant> RegionBossRewardGrants { get; }

    DbSet<TowerFloorProgress> TowerFloorProgresses { get; }
    DbSet<TowerRally> TowerRallies { get; }
    DbSet<TowerRallyParticipant> TowerRallyParticipants { get; }
    DbSet<TowerRallyApplication> TowerRallyApplications { get; }
    DbSet<TowerAttempt> TowerAttempts { get; }
    DbSet<TowerCombatPlayback> TowerCombatPlaybacks { get; }
    DbSet<TowerCombatPlaybackArtifact> TowerCombatPlaybackArtifacts { get; }
    Task<TowerRally?> GetWorldTowerRallyWithSnapshotsAsync(
        Guid rallyId,
        string serverId,
        CancellationToken ct = default);
    DbSet<TowerContribution> TowerContributions { get; }
    DbSet<TowerEchoClear> TowerEchoClears { get; }
    DbSet<ServerUnlock> ServerUnlocks { get; }

    // Snapshots

    DbSet<CharacterSnapshot> CharacterSnapshots { get; }
    DbSet<EquippedEssenceSnapshot> EquippedEssenceSnapshots { get; }

    DbSet<AppUser> Users { get; }
    DbSet<ExternalLogin> ExternalLogins { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Execute raw sql. Never use string interpolation to embed values as this can cause sql injection
    /// Instead parse extra args as sqlParams
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="token"></param>
    /// <param name="sqlParams"></param>
    /// <returns></returns>
    Task<int> ExecuteSqlRawAsync(string sql, CancellationToken token = default, params object[] sqlParams);

    /// <summary>
    /// Exposes EF Core's Entry method to allow property state manipulation.
    /// </summary>
    EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Clears tracked entities after a completed unit of work so a subsequent
    /// transaction can reload concurrency-protected state from the database.
    /// </summary>
    void ClearTrackedEntities();

    IExecutionStrategy CreateExecutionStrategy();
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task<T> ExecuteWithCharacterLockAsync<T>(
        Guid characterId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default) => operation(ct);
    Task AcquireCharacterCommandLockAsync(Guid characterId, CancellationToken ct = default);
    Task AcquireStateSyncScopeLockAsync(string scopeKey, CancellationToken ct = default) => Task.CompletedTask;
    Task AcquireWorldTowerFloorLockAsync(string serverId, int floorNumber, CancellationToken ct = default);
    Task AcquireRaidRunLockAsync(Guid raidRunId, CancellationToken ct = default) => Task.CompletedTask;
    Task AcquireRaidBossLockAsync(string raidBossId, CancellationToken ct = default) => Task.CompletedTask;
    Task AcquireRegionBossScheduleLockAsync(CancellationToken ct = default) => Task.CompletedTask;
    Task AcquireRegionBossEventLockAsync(Guid eventId, CancellationToken ct = default) => Task.CompletedTask;
    Task AcquireRegionBossRunLockAsync(Guid runId, CancellationToken ct = default) => Task.CompletedTask;
    Task AcquireRegionBossRewardGrantLockAsync(Guid grantId, CancellationToken ct = default) => Task.CompletedTask;
    Task<IReadOnlyList<Guid>> ClaimWorldTowerSimulationsAsync(
        string owner,
        DateTimeOffset now,
        DateTimeOffset leaseUntil,
        int limit,
        CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> ClaimWorldTowerPlaybackFinalizationsAsync(
        string owner,
        DateTimeOffset now,
        DateTimeOffset leaseUntil,
        int limit,
        CancellationToken ct = default);
    Task<bool> RenewWorldTowerSimulationLeaseAsync(
        Guid attemptId,
        string owner,
        DateTimeOffset leaseUntil,
        CancellationToken ct = default);
    Task ReleaseWorldTowerPlaybackFinalizationAsync(
        Guid attemptId,
        string owner,
        CancellationToken ct = default);
    IDbContextTransaction? CurrentTransaction { get; }
    bool HasChanges { get; }
    long SaveChangesVersion => 0;
}
