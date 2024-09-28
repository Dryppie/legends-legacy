using Application.Common.Interfaces;
using Domain.Models.CharacterActions;
using Domain.Models.Entities;
using Domain.Models.Entities.Actors;
using Domain.Models.Entities.Actors.Characters;
using Domain.Models.GatheringNodes;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Domain.Models.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Persistence.LL.Seeds;

namespace Persistence.LL;
public class LLDbContext(DbContextOptions<LLDbContext> options) : IdentityDbContext<AppUser>(options), IDbContext
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LLDbContext).Assembly);

        // Apply provider-specific configurations
        if (Database.IsSqlServer() || Database.IsMySql())
        {
            SetupProviderSpecificConversions(modelBuilder);
        }

        // Configure inheritance using a discriminator
        modelBuilder.Entity<Entity>()
            .HasDiscriminator<int>("EntityType")
            .HasValue<Actor>(1)
            //.HasValue<Item>(2)
            .HasValue<Character>(3);
        //.HasValue<NPC>(4)
        //.HasValue<Creature>(5);

        // Seed data
        SeedData.Seed(modelBuilder);
    }

    private static void SetupProviderSpecificConversions(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));

            foreach (var property in properties)
            {
                builder.Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new DateTimeOffsetToBinaryConverter());
            }
        }
    }

    //public DbSet<Ability> Abilities => Set<Ability>();

    //public DbSet<Achievement> Achievements => Set<Achievement>();

    //public DbSet<AttributeBase> Attributes => Set<AttributeBase>();
    //public DbSet<EntityAttribute> EntityAttributes => Set<EntityAttribute>();

    //public DbSet<Building> Buildings => Set<Building>();

    public DbSet<Character> Characters => Set<Character>();

    //public DbSet<Echo> Echoes => Set<Echo>();

    //public DbSet<Entity> Entities => Set<Entity>();

    // Effects

    //public DbSet<Modifier> Modifiers => Set<Modifier>();

    // Player Actions
    public DbSet<CharacterAction> CharacterActions => Set<CharacterAction>();

    public DbSet<GatheringNode> GatheringNodes => Set<GatheringNode>();

    //public DbSet<Equipment> Equipments => Set<Equipment>();

    //public DbSet<Essence> Essences => Set<Essence>();

    //public DbSet<Guild> Guilds => Set<Guild>();

    //public DbSet<GuildMember> GuildMembers => Set<GuildMember>();

    public DbSet<Inventory> Inventories => Set<Inventory>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<Item> Items => Set<Item>();
    public DbSet<LootTable> LootTables => Set<LootTable>();

    //public DbSet<Party> Parties => Set<Party>();

    //public DbSet<PartyMember> PartyMembers => Set<PartyMember>();

    //public DbSet<Profession> Professions => Set<Profession>();

    //public DbSet<Quest> Quests => Set<Quest>();

    //public DbSet<QuestStage> QuestStages => Set<QuestStage>();

    //public DbSet<Stat> Stats => Set<Stat>();

    //public DbSet<Title> Titles => Set<Title>();

    //public DbSet<Town> Towns => Set<Town>();

    //public DbSet<TownBuilding> TownBuildings => Set<TownBuilding>();

    public new DbSet<AppUser> Users => Set<AppUser>();
}

// Used by design time, eg. dotnet ef migrations add Stuff
public class LLDbContextFactory : IDesignTimeDbContextFactory<LLDbContext>
{
    public LLDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory() + "\\..\\API.LL";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("LegendsLegacyDB");

        DbContextOptionsBuilder<LLDbContext> optionsBuilder = new();

        var timeout = configuration.GetSection("Database").GetValue<int>("TimeoutInSeconds");
        optionsBuilder.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.CommandTimeout(timeout));

        return new(optionsBuilder.Options);
    }
}