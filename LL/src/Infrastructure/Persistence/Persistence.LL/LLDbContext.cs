using Application.Common.Interfaces;
using Domain.Models.Achievements;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons.Runs;
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
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.LootTables;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Prophecies;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Domain.Models.Soulstones;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;

namespace Persistence.LL;
public class LLDbContext(DbContextOptions<LLDbContext> options) : DbContext(options), IDbContext
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteSqlRawAsync(string sql, CancellationToken token = default, params object[] sqlParams)
    {
        return await Database.ExecuteSqlRawAsync(sql, sqlParams);
    }

    public EntityEntry<TEntity> GetEntry<TEntity>(TEntity entity) where TEntity : class
        => Entry(entity);

    public IExecutionStrategy CreateExecutionStrategy()
        => Database.CreateExecutionStrategy();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => Database.BeginTransactionAsync(ct);

    public IDbContextTransaction? CurrentTransaction
        => Database.CurrentTransaction;

    public bool HasChanges
        => ChangeTracker.HasChanges();

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

        modelBuilder.Entity<LootTableEntry>()
            .HasDiscriminator<int>("LootTableType")
            .HasValue<LootTable>(1)
            .HasValue<LootTableItem>(2);
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

    public DbSet<AchievementDefinition> AchievementDefinitions => Set<AchievementDefinition>();
    public DbSet<PlayerAchievementProgress> PlayerAchievementProgresses => Set<PlayerAchievementProgress>();
    public DbSet<TitleDefinition> TitleDefinitions => Set<TitleDefinition>();
    public DbSet<PlayerTitleUnlock> PlayerTitleUnlocks => Set<PlayerTitleUnlock>();
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
    public DbSet<TournamentRound> TournamentRounds => Set<TournamentRound>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();
    public DbSet<TournamentRewardGrant> TournamentRewardGrants => Set<TournamentRewardGrant>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterSoulstoneUpgrade> CharacterSoulstoneUpgrades => Set<CharacterSoulstoneUpgrade>();
    public DbSet<Creature> Creatures => Set<Creature>();

    //public DbSet<Echo> Echoes => Set<Echo>();

    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<EquipmentSlot> EquipmentSlots => Set<EquipmentSlot>();

    public DbSet<EssenceItemBase> EssenceItems => Set<EssenceItemBase>();
    public DbSet<PlayerEssence> PlayerEssences => Set<PlayerEssence>();
    public DbSet<EssenceLoadout> EssenceLoadouts => Set<EssenceLoadout>();
    public DbSet<EssenceLoadoutSlot> EssenceLoadoutSlots => Set<EssenceLoadoutSlot>();
    public DbSet<CreatureResonance> MonsterResonances => Set<CreatureResonance>();

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

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<ItemBase> ItemBases => Set<ItemBase>();
    public DbSet<ItemInstance> ItemInstances => Set<ItemInstance>();
    public DbSet<LootTable> LootTables => Set<LootTable>();
    public DbSet<LootTableItem> LootTableItems => Set<LootTableItem>();

    public DbSet<MarketPlaceListing> MarketPlaceListings => Set<MarketPlaceListing>();

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

    public DbSet<Region> Regions => Set<Region>();

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
