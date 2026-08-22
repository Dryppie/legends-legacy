using Application.Common.Interfaces;
using Domain.Models.Achievements;
using Domain.Models.Administration;
using Domain.Models.Attributes;
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
using Domain.Models.Entities.NPCs;
using Domain.Models.Essences;
using Domain.Models.Guilds;
using Domain.Models.Guilds.Buildings;
using Domain.Models.Guilds.Missions;
using Domain.Models.Guilds.Shop;
using Domain.Models.Inventories;
using Domain.Models.LootHistory;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
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
using Domain.Models.Professions.Crafting.V2;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;

namespace Persistence.LL;
public class LLDbContext(DbContextOptions<LLDbContext> options) : DbContext(options), IDbContext
{
    private long _saveChangesVersion;

    public Task<TowerRally?> GetWorldTowerRallyWithSnapshotsAsync(
        Guid rallyId,
        string serverId,
        CancellationToken ct = default) =>
        TowerRallies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Participants)
                .ThenInclude(x => x.CharacterSnapshot)
                    .ThenInclude(x => x.BaseAttributes)
            .Include(x => x.Participants)
                .ThenInclude(x => x.CharacterSnapshot)
                    .ThenInclude(x => x.Equipment)
                        .ThenInclude(x => x.InstanceModifiers)
            .Include(x => x.Participants)
                .ThenInclude(x => x.CharacterSnapshot)
                    .ThenInclude(x => x.EquippedEssences)
            .SingleOrDefaultAsync(x => x.Id == rallyId && x.ServerId == serverId, ct);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        MigrateTrackedEquipment();
        NormalizeIdentityFields();
        EnforceAppendOnlyAdminActions();
        EnforceAppendOnlyEconomyLedger();
        EnforceAppendOnlyRiskEvidence();
        var affectedRows = await base.SaveChangesAsync(cancellationToken);
        _saveChangesVersion++;
        return affectedRows;
    }

    private void EnforceAppendOnlyAdminActions()
    {
        if (ChangeTracker.Entries<AdminAction>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Administration audit entries are append-only and cannot be modified or deleted.");
        }
    }

    private void MigrateTrackedEquipment()
    {
        foreach (var entry in ChangeTracker.Entries<EquipmentInstance>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            EquipmentStatModelMigrator.MigrateToCurrent(entry.Entity);
        }

        foreach (var entry in ChangeTracker.Entries<EquipmentSnapshot>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            EquipmentStatModelMigrator.MigrateToCurrent(entry.Entity);
        }
    }

    private void EnforceAppendOnlyEconomyLedger()
    {
        if (ChangeTracker.Entries<EconomyLedgerEntry>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Economy ledger entries are append-only and cannot be modified or deleted.");
        }
    }

    private void EnforceAppendOnlyRiskEvidence()
    {
        if (ChangeTracker.Entries<AccountRiskHistory>()
                .Any(x => x.State is EntityState.Modified or EntityState.Deleted) ||
            ChangeTracker.Entries<AccountRiskNote>()
                .Any(x => x.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Account-risk history and investigation notes are append-only.");
        }
    }

    private void NormalizeIdentityFields()
    {
        foreach (var entry in ChangeTracker.Entries<AppUser>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.NormalizeIdentityFields();
            }
        }

        foreach (var entry in ChangeTracker.Entries<Character>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.NormalizeName();
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> ExecuteSqlRawAsync(string sql, CancellationToken token = default, params object[] sqlParams)
    {
        return await Database.ExecuteSqlRawAsync(sql, sqlParams, token);
    }

    public EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class
        => Entry(entity);

    public IExecutionStrategy CreateExecutionStrategy()
        => Database.CreateExecutionStrategy();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => Database.BeginTransactionAsync(ct);

    public async Task<T> ExecuteWithCharacterLockAsync<T>(
        Guid characterId,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return await operation(ct);
        }

        if (Database.CurrentTransaction is not null)
        {
            await AcquireCharacterCommandLockAsync(characterId, ct);
            return await operation(ct);
        }

        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(ct);
            try
            {
                await AcquireCharacterCommandLockAsync(characterId, ct);
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task AcquireCharacterCommandLockAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        if (Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "A character command advisory lock requires an active transaction.");
        }

        var lockId = BitConverter.ToInt64(characterId.ToByteArray(), 0);
        await ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            ct,
            lockId);
    }

    public async Task AcquireStateSyncScopeLockAsync(
        string scopeKey,
        CancellationToken ct = default)
    {
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        if (Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "A state synchronization advisory lock requires an active transaction.");
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(scopeKey));
        var lockId = BitConverter.ToInt64(digest, 0);
        await ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            ct,
            lockId);
    }

    public async Task AcquireWorldTowerFloorLockAsync(
        string serverId,
        int floorNumber,
        CancellationToken ct = default)
    {
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        if (Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "A World Tower floor advisory lock requires an active transaction.");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"world-tower:{serverId}:{floorNumber}"));
        var lockId = BitConverter.ToInt64(bytes, 0);
        await ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", ct, lockId);
    }

    public async Task AcquireRaidRunLockAsync(
        Guid raidRunId,
        CancellationToken ct = default)
    {
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
            return;
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException("A raid run advisory lock requires an active transaction.");

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"raid:{raidRunId:N}"));
        var lockId = BitConverter.ToInt64(bytes, 0);
        await ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", ct, lockId);
    }

    public async Task AcquireRaidBossLockAsync(
        string raidBossId,
        CancellationToken ct = default)
    {
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
            return;
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException("A raid boss advisory lock requires an active transaction.");

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"raid-boss:{raidBossId}"));
        var lockId = BitConverter.ToInt64(bytes, 0);
        await ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", ct, lockId);
    }

    public Task AcquireRegionBossScheduleLockAsync(CancellationToken ct = default) =>
        AcquireNamedAdvisoryLockAsync("region-boss:schedule", "A Region Boss schedule advisory lock requires an active transaction.", ct);

    public Task AcquireRegionBossEventLockAsync(Guid eventId, CancellationToken ct = default) =>
        AcquireNamedAdvisoryLockAsync($"region-boss:event:{eventId:N}", "A Region Boss event advisory lock requires an active transaction.", ct);

    public Task AcquireRegionBossRunLockAsync(Guid runId, CancellationToken ct = default) =>
        AcquireNamedAdvisoryLockAsync($"region-boss:run:{runId:N}", "A Region Boss run advisory lock requires an active transaction.", ct);

    public Task AcquireRegionBossRewardGrantLockAsync(Guid grantId, CancellationToken ct = default) =>
        AcquireNamedAdvisoryLockAsync($"region-boss:reward-grant:{grantId:N}", "A Region Boss reward grant advisory lock requires an active transaction.", ct);

    private async Task AcquireNamedAdvisoryLockAsync(string key, string transactionError, CancellationToken ct)
    {
        if (Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
            return;
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException(transactionError);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        var lockId = BitConverter.ToInt64(bytes, 0);
        await ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", ct, lockId);
    }

    public Task<IReadOnlyList<Guid>> ClaimWorldTowerSimulationsAsync(
        string owner,
        DateTimeOffset now,
        DateTimeOffset leaseUntil,
        int limit,
        CancellationToken ct = default) =>
        ClaimWorldTowerWorkAsync(
            """
            SELECT "Id" AS "Value"
            FROM "TowerAttempts"
            WHERE "Status" = {0}
              AND ("SimulationLeaseUntil" IS NULL OR "SimulationLeaseUntil" <= {1})
            ORDER BY "StartedAt"
            LIMIT {2}
            FOR UPDATE SKIP LOCKED
            """,
            [(int)TowerAttemptStatus.Started, now, limit],
            async ids =>
            {
                var rows = await TowerAttempts.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
                foreach (var row in rows)
                {
                    row.SimulationLeaseOwner = owner;
                    row.SimulationLeaseUntil = leaseUntil;
                    row.SimulationAttempts++;
                }
            },
            async () => await TowerAttempts
                .Where(x => x.Status == TowerAttemptStatus.Started
                            && (x.SimulationLeaseUntil == null || x.SimulationLeaseUntil <= now))
                .OrderBy(x => x.StartedAt)
                .Select(x => x.Id)
                .Take(limit)
                .ToArrayAsync(ct),
            ct);

    public Task<IReadOnlyList<Guid>> ClaimWorldTowerPlaybackDispatchesAsync(
        string owner,
        DateTimeOffset now,
        DateTimeOffset leaseUntil,
        int limit,
        CancellationToken ct = default) =>
        ClaimWorldTowerWorkAsync(
            """
            SELECT p."TowerAttemptId" AS "Value"
            FROM "TowerCombatPlaybacks" p
            INNER JOIN "TowerAttempts" a ON a."Id" = p."TowerAttemptId"
            WHERE p."PlaybackStartedAt" <= {0}
              AND p."NextFrameDueAt" <= {0}
              AND (p."LastPublishedSequence" < p."FrameCount" - 1 OR a."Status" = {1})
              AND (p."DispatchLeaseUntil" IS NULL OR p."DispatchLeaseUntil" <= {0})
            ORDER BY p."PlaybackStartedAt"
            LIMIT {2}
            FOR UPDATE OF p SKIP LOCKED
            """,
            [now, (int)TowerAttemptStatus.Playback, limit],
            async ids =>
            {
                var rows = await TowerCombatPlaybacks
                    .Where(x => ids.Contains(x.TowerAttemptId))
                    .ToListAsync(ct);
                foreach (var row in rows)
                {
                    row.DispatchLeaseOwner = owner;
                    row.DispatchLeaseUntil = leaseUntil;
                }
            },
            async () => await TowerCombatPlaybacks
                .Where(x => x.PlaybackStartedAt <= now
                            && x.NextFrameDueAt <= now
                            && (x.LastPublishedSequence < x.FrameCount - 1
                                || x.TowerAttempt.Status == TowerAttemptStatus.Playback)
                            && (x.DispatchLeaseUntil == null || x.DispatchLeaseUntil <= now))
                .OrderBy(x => x.PlaybackStartedAt)
                .Select(x => x.TowerAttemptId)
                .Take(limit)
                .ToArrayAsync(ct),
            ct);

    public async Task<bool> RenewWorldTowerSimulationLeaseAsync(
        Guid attemptId,
        string owner,
        DateTimeOffset leaseUntil,
        CancellationToken ct = default)
    {
        var attempt = await TowerAttempts.SingleOrDefaultAsync(
            x => x.Id == attemptId
                 && x.Status == TowerAttemptStatus.Started
                 && x.SimulationLeaseOwner == owner,
            ct);
        if (attempt is null)
            return false;

        attempt.SimulationLeaseUntil = leaseUntil;
        await SaveChangesAsync(ct);
        return true;
    }

    public async Task ReleaseWorldTowerPlaybackDispatchAsync(
        Guid attemptId,
        string owner,
        CancellationToken ct = default)
    {
        var playback = await TowerCombatPlaybacks.SingleOrDefaultAsync(
            x => x.TowerAttemptId == attemptId && x.DispatchLeaseOwner == owner,
            ct);
        if (playback is null)
            return;
        playback.DispatchLeaseOwner = null;
        playback.DispatchLeaseUntil = null;
        await SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<Guid>> ClaimWorldTowerWorkAsync(
        string sql,
        object[] parameters,
        Func<Guid[], Task> update,
        Func<Task<Guid[]>> fallbackQuery,
        CancellationToken ct)
    {
        await using var transaction = await Database.BeginTransactionAsync(ct);
        var ids = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL"
            ? await Database.SqlQueryRaw<Guid>(sql, parameters).ToArrayAsync(ct)
            : await fallbackQuery();
        if (ids.Length > 0)
        {
            await update(ids);
            await SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return ids;
    }

    public IDbContextTransaction? CurrentTransaction
        => Database.CurrentTransaction;

    public bool HasChanges
        => ChangeTracker.HasChanges();

    public long SaveChangesVersion => _saveChangesVersion;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LLDbContext).Assembly);
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            SetupSqlite(modelBuilder);
        }

        modelBuilder.Entity<Character>().HasBaseType<Entity>();
        modelBuilder.Entity<NPC>().HasBaseType<Entity>();
        modelBuilder.Entity<Creature>().HasBaseType<Entity>();

        // If a class is derived from Entity, add it here
        modelBuilder.Entity<Entity>()
            .HasDiscriminator<int>("EntityType")
            .HasValue<Character>(1)
            .HasValue<NPC>(2)
            .HasValue<Creature>(3);

        modelBuilder.Entity<ItemBase>()
            .HasDiscriminator<ItemType>("ItemType")
            .HasValue<ItemBase>(ItemType.Resource)
            .HasValue<EquipmentBase>(ItemType.Equipment)
            .HasValue<EssenceItemBase>(ItemType.Essence);

        modelBuilder.Entity<ItemInstance>()
            .HasDiscriminator<ItemType>("ItemType")
            .HasValue<ItemInstance>(ItemType.Misc)
            .HasValue<EquipmentInstance>(ItemType.Equipment)
            .HasValue<EssenceItemInstance>(ItemType.Essence);

        modelBuilder.Entity<ActionDetails>()
            .HasDiscriminator<CharacterActionType>("ActionType")
            .HasValue<CombatActionDetails>(CharacterActionType.Combat)
            .HasValue<CraftingActionDetails>(CharacterActionType.Crafting);

    }

    private static void SetupSqlite(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                           || p.PropertyType ==
                                                                           typeof(DateTimeOffset?));
            foreach (var property in properties)
            {
                builder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new DateTimeOffsetToBinaryConverter());
            }
        }
    }

    public DbSet<AdminAction> AdminActions => Set<AdminAction>();
    public DbSet<AdminActionPreview> AdminActionPreviews => Set<AdminActionPreview>();
    public DbSet<AccountRestriction> AccountRestrictions => Set<AccountRestriction>();
    public DbSet<AccountRiskSnapshot> AccountRiskSnapshots => Set<AccountRiskSnapshot>();
    public DbSet<AccountRiskHistory> AccountRiskHistory => Set<AccountRiskHistory>();
    public DbSet<AccountRiskInvestigation> AccountRiskInvestigations => Set<AccountRiskInvestigation>();
    public DbSet<AccountRiskNote> AccountRiskNotes => Set<AccountRiskNote>();
    public DbSet<AchievementDefinition> AchievementDefinitions => Set<AchievementDefinition>();
    public DbSet<AchievementEventLedger> AchievementEventLedgers => Set<AchievementEventLedger>();
    public DbSet<PlayerAchievementProgress> PlayerAchievementProgresses => Set<PlayerAchievementProgress>();
    public DbSet<TitleDefinition> TitleDefinitions => Set<TitleDefinition>();
    public DbSet<PlayerTitleUnlock> PlayerTitleUnlocks => Set<PlayerTitleUnlock>();
    public DbSet<BackgroundJobExecution> BackgroundJobExecutions => Set<BackgroundJobExecution>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<EntityAttribute> EntityAttributes => Set<EntityAttribute>();

    //public DbSet<Building> Buildings => Set<Building>();
    public DbSet<CharacterArenaProfile> CharacterArenaProfiles => Set<CharacterArenaProfile>();
    public DbSet<ArenaTicketStatus> ArenaTicketStatus => Set<ArenaTicketStatus>();
    public DbSet<ColosseumMatchResult> ColosseumMatches => Set<ColosseumMatchResult>();
    public DbSet<ArenaDefenseSnapshot> ArenaDefenseSnapshots => Set<ArenaDefenseSnapshot>();
    public DbSet<ChampionMarketPurchase> ChampionMarketPurchases => Set<ChampionMarketPurchase>();
    public DbSet<TournamentDefinition> TournamentDefinitions => Set<TournamentDefinition>();
    public DbSet<TournamentInstance> ArenaTournaments => Set<TournamentInstance>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();
    public DbSet<TournamentTeamApplication> TournamentTeamApplications => Set<TournamentTeamApplication>();
    public DbSet<TournamentTeamInvite> TournamentTeamInvites => Set<TournamentTeamInvite>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();
    public DbSet<TournamentCombatSnapshot> TournamentCombatSnapshots => Set<TournamentCombatSnapshot>();
    public DbSet<TournamentCombatReplay> TournamentCombatReplays => Set<TournamentCombatReplay>();
    public DbSet<TournamentCombatReplayArtifact> TournamentCombatReplayArtifacts => Set<TournamentCombatReplayArtifact>();
    public DbSet<TournamentRound> TournamentRounds => Set<TournamentRound>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentRewardGrant> TournamentRewardGrants => Set<TournamentRewardGrant>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterSoulstoneUpgrade> CharacterSoulstoneUpgrades => Set<CharacterSoulstoneUpgrade>();
    public DbSet<CharacterQuestProgress> CharacterQuestProgresses => Set<CharacterQuestProgress>();
    public DbSet<CharacterQuestObjectiveProgress> CharacterQuestObjectiveProgresses => Set<CharacterQuestObjectiveProgress>();
    public DbSet<QuestEventLedger> QuestEventLedgers => Set<QuestEventLedger>();
    public DbSet<EventQuestInstance> EventQuestInstances => Set<EventQuestInstance>();
    public DbSet<EventQuestObjectiveProgress> EventQuestObjectiveProgresses => Set<EventQuestObjectiveProgress>();
    public DbSet<EventQuestCharacterContribution> EventQuestCharacterContributions => Set<EventQuestCharacterContribution>();
    public DbSet<EventQuestEventLedger> EventQuestEventLedgers => Set<EventQuestEventLedger>();
    public DbSet<EventQuestRewardClaim> EventQuestRewardClaims => Set<EventQuestRewardClaim>();
    public DbSet<EventQuestMilestoneClaim> EventQuestMilestoneClaims => Set<EventQuestMilestoneClaim>();
    public DbSet<Creature> Creatures => Set<Creature>();

    //public DbSet<Echo> Echoes => Set<Echo>();

    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<EquipmentSlot> EquipmentSlots => Set<EquipmentSlot>();

    public DbSet<EssenceItemBase> EssenceItems => Set<EssenceItemBase>();
    public DbSet<PlayerEssence> PlayerEssences => Set<PlayerEssence>();
    public DbSet<EssenceLoadout> EssenceLoadouts => Set<EssenceLoadout>();
    public DbSet<EssenceLoadoutSlot> EssenceLoadoutSlots => Set<EssenceLoadoutSlot>();
    public DbSet<CreatureResonance> CreatureResonances => Set<CreatureResonance>();
    public DbSet<CharacterCreatureArchiveEntry> CharacterCreatureArchiveEntries => Set<CharacterCreatureArchiveEntry>();

    public DbSet<DungeonRun> DungeonRuns => Set<DungeonRun>();
    public DbSet<RunReward> RunRewards => Set<RunReward>();
    public DbSet<DungeonCompletionRecord> DungeonCompletionRecords => Set<DungeonCompletionRecord>();
    public DbSet<CharacterDungeonMastery> CharacterDungeonMasteries => Set<CharacterDungeonMastery>();

    // Effects

    //public DbSet<Modifier> Modifiers => Set<Modifier>();

    // Player Actions
    public DbSet<CharacterAction> CharacterActions => Set<CharacterAction>();
    public DbSet<ActionDetails> ActionDetails => Set<ActionDetails>();
    public DbSet<CraftingQueueItem> CraftingQueueItems => Set<CraftingQueueItem>(); 

    //public DbSet<Equipment> Equipments => Set<Equipment>();


    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildInvite> GuildInvites => Set<GuildInvite>();

    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<GuildBuilding> GuildBuildings => Set<GuildBuilding>();
    public DbSet<GuildActivityLog> GuildActivityLogs => Set<GuildActivityLog>();
    public DbSet<GuildMissionOption> GuildMissionOptions => Set<GuildMissionOption>();
    public DbSet<GuildMissionInstance> GuildMissionInstances => Set<GuildMissionInstance>();
    public DbSet<GuildMissionContribution> GuildMissionContributions => Set<GuildMissionContribution>();
    public DbSet<PersonalGuildOrder> PersonalGuildOrders => Set<PersonalGuildOrder>();
    public DbSet<GuildMemberContributionPeriod> GuildMemberContributionPeriods => Set<GuildMemberContributionPeriod>();
    public DbSet<GuildContributionLedger> GuildContributionLedgers => Set<GuildContributionLedger>();
    public DbSet<GuildShopPurchase> GuildShopPurchases => Set<GuildShopPurchase>();
    public DbSet<GuildRolePermission> GuildRolePermissions => Set<GuildRolePermission>();
    public DbSet<GuildVaultItem> GuildVaultItems => Set<GuildVaultItem>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<LootHistoryEntry> LootHistoryEntries => Set<LootHistoryEntry>();
    public DbSet<PlayerTransferRecord> PlayerTransferHistory => Set<PlayerTransferRecord>();
    public DbSet<EconomyLedgerEntry> EconomyLedger => Set<EconomyLedgerEntry>();

    public DbSet<ItemBase> ItemBases => Set<ItemBase>();
    public DbSet<ItemInstance> ItemInstances => Set<ItemInstance>();
    public DbSet<MarketPlaceListing> MarketPlaceListings => Set<MarketPlaceListing>();
    public DbSet<MarketPlaceBuyOrder> MarketPlaceBuyOrders => Set<MarketPlaceBuyOrder>();
    public DbSet<MarketPlaceOrder> MarketPlaceOrders => Set<MarketPlaceOrder>();
    public DbSet<GameEventOutboxMessage> GameEventOutboxMessages => Set<GameEventOutboxMessage>();
    public DbSet<GameEventOutboxDelivery> GameEventOutboxDeliveries => Set<GameEventOutboxDelivery>();
    public DbSet<StateSyncRevision> StateSyncRevisions => Set<StateSyncRevision>();

    //public DbSet<Party> Parties => Set<Party>();

    //public DbSet<PartyMember> PartyMembers => Set<PartyMember>();

    public DbSet<Profession> Professions => Set<Profession>();

    //public DbSet<Quest> Quests => Set<Quest>();

    //public DbSet<QuestStage> QuestStages => Set<QuestStage>();

    //public DbSet<Stat> Stats => Set<Stat>();

    //public DbSet<Title> Titles => Set<Title>();

    //public DbSet<Town> Towns => Set<Town>();

    //public DbSet<TownBuilding> TownBuildings => Set<TownBuilding>();

    public DbSet<CharacterRecipeUnlock> CharacterRecipeUnlocks => Set<CharacterRecipeUnlock>();
    public DbSet<CharacterRecipeMastery> CharacterRecipeMasteries => Set<CharacterRecipeMastery>();
    public DbSet<ProphecyDefinition> ProphecyDefinitions => Set<ProphecyDefinition>();
    public DbSet<PlayerProphecyInstance> PlayerProphecyInstances => Set<PlayerProphecyInstance>();
    public DbSet<WeeklyRevelationProgress> WeeklyRevelationProgress => Set<WeeklyRevelationProgress>();
    public DbSet<DailyProphecyRerollState> DailyProphecyRerollStates => Set<DailyProphecyRerollState>();

    public DbSet<Region> Regions => Set<Region>();

    public DbSet<RaidRun> RaidRuns => Set<RaidRun>();
    public DbSet<RaidSignup> RaidSignups => Set<RaidSignup>();
    public DbSet<RaidLaneResult> RaidLaneResults => Set<RaidLaneResult>();
    public DbSet<RaidPlayback> RaidPlaybacks => Set<RaidPlayback>();
    public DbSet<RaidPlaybackArtifact> RaidPlaybackArtifacts => Set<RaidPlaybackArtifact>();
    public DbSet<RaidParticipantResult> RaidParticipantResults => Set<RaidParticipantResult>();
    public DbSet<RaidRewardClaim> RaidRewardClaims => Set<RaidRewardClaim>();
    public DbSet<RaidTrophyPurchase> RaidTrophyPurchases => Set<RaidTrophyPurchase>();

    public DbSet<RegionBossEvent> RegionBossEvents => Set<RegionBossEvent>();
    public DbSet<RegionBossSignup> RegionBossSignups => Set<RegionBossSignup>();
    public DbSet<RegionBossRun> RegionBossRuns => Set<RegionBossRun>();
    public DbSet<RegionBossParticipantResult> RegionBossParticipantResults => Set<RegionBossParticipantResult>();
    public DbSet<RegionBossPlayback> RegionBossPlaybacks => Set<RegionBossPlayback>();
    public DbSet<RegionBossPlaybackArtifact> RegionBossPlaybackArtifacts => Set<RegionBossPlaybackArtifact>();
    public DbSet<RegionBossRewardGrant> RegionBossRewardGrants => Set<RegionBossRewardGrant>();

    public DbSet<TowerFloorProgress> TowerFloorProgresses => Set<TowerFloorProgress>();
    public DbSet<TowerRally> TowerRallies => Set<TowerRally>();
    public DbSet<TowerRallyParticipant> TowerRallyParticipants => Set<TowerRallyParticipant>();
    public DbSet<TowerRallyApplication> TowerRallyApplications => Set<TowerRallyApplication>();
    public DbSet<TowerAttempt> TowerAttempts => Set<TowerAttempt>();
    public DbSet<TowerCombatPlayback> TowerCombatPlaybacks => Set<TowerCombatPlayback>();
    public DbSet<TowerCombatPlaybackArtifact> TowerCombatPlaybackArtifacts => Set<TowerCombatPlaybackArtifact>();
    public DbSet<TowerContribution> TowerContributions => Set<TowerContribution>();
    public DbSet<TowerEchoClear> TowerEchoClears => Set<TowerEchoClear>();
    public DbSet<ServerUnlock> ServerUnlocks => Set<ServerUnlock>();

    // Snapshots
    public DbSet<CharacterSnapshot> CharacterSnapshots => Set<CharacterSnapshot>();
    public DbSet<EquippedEssenceSnapshot> EquippedEssenceSnapshots => Set<EquippedEssenceSnapshot>();

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}

// Used by design time, eg. dotnet ef migrations add Stuff
public class LLDbContextFactory : IDesignTimeDbContextFactory<LLDbContext>
{
    public LLDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("LegendsLegacyDB");

        DbContextOptionsBuilder<LLDbContext> optionsBuilder = new();

        var timeout = configuration.GetSection("Database").GetValue<int>("TimeoutInSeconds");
        optionsBuilder.UseNpgsql(connectionString, sqlServerOptions => sqlServerOptions.CommandTimeout(timeout));

        return new(optionsBuilder.Options);
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidates = new[]
            {
                Path.Combine(current.FullName, "appsettings.json"),
                Path.Combine(current.FullName, "API.LL", "appsettings.json"),
                Path.Combine(current.FullName, "src", "API", "API.LL", "appsettings.json"),
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "appsettings.json"),
            };

            var appSettings = candidates.FirstOrDefault(File.Exists);
            if (appSettings is not null)
            {
                return Path.GetDirectoryName(appSettings)!;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL appsettings.json for design-time DbContext creation.");
    }
}
