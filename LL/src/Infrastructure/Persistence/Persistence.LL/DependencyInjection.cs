using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Domain.Models.Achievements;
using Domain.Models.Administration;
using Domain.Models.Attributes;
using Domain.Models.CharacterActions;
using Domain.Models.Colosseum;
using Domain.Models.Colosseum.Tournaments;
using Domain.Models.Dungeons.Mastery;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Economy;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Essences;
using Domain.Models.Guilds;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.TierPackages;
using Domain.Models.Leaderboards;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting;
using Domain.Models.Prophecies;
using Domain.Models.Quests;
using Domain.Models.Quests.Events;
using Domain.Models.Regions;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Domain.Models.Soulstones;
using Domain.Models.Users;
using Application.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL.BackgroundJobs;
using Persistence.LL.Repositories.Achievements;
using Persistence.LL.Repositories.Administration;
using Persistence.LL.Repositories.Attributes;
using Persistence.LL.Repositories.CharacterActions;
using Persistence.LL.Repositories.Colosseum;
using Persistence.LL.Repositories.Dungeons;
using Persistence.LL.Repositories.Entities;
using Persistence.LL.Repositories.Entities.Characters;
using Persistence.LL.Repositories.Entities.Creatures;
using Persistence.LL.Repositories.Economy;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Essences;
using Persistence.LL.Repositories.Guilds;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Persistence.LL.Repositories.Leaderboards;
using Persistence.LL.Repositories.MarketPlaces;
using Persistence.LL.Repositories.Outbox;
using Persistence.LL.Repositories.Professions;
using Persistence.LL.Repositories.Professions.Craftings;
using Persistence.LL.Repositories.Prophecies;
using Persistence.LL.Repositories.Quests;
using Persistence.LL.Repositories.Regions;
using Persistence.LL.Repositories.Regions.Areas;
using Persistence.LL.Repositories.Snapshots;
using Persistence.LL.Repositories.Soulstones;
using Persistence.LL.Repositories.Users;

namespace Persistence.LL;
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BackgroundJobOptions>()
            .Bind(configuration.GetSection(BackgroundJobOptions.SectionName))
            .Validate(options => options.MaxConcurrency > 0, "BackgroundJobs:MaxConcurrency must be greater than zero.")
            .Validate(options => options.RunningExecutionTimeoutMinutes > 0, "BackgroundJobs:RunningExecutionTimeoutMinutes must be greater than zero.")
            .ValidateOnStart();

        var timeout = configuration.GetSection("Database").GetValue<int>("TimeoutInSeconds");
        services.AddDbContextFactory<LLDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LegendsLegacyDB"), npgsqlOptions => npgsqlOptions.CommandTimeout(timeout))
        );

        services.AddScoped<IDbContext>(provider => provider.GetRequiredService<LLDbContext>() ?? throw new SystemException("LLDbContext could not be resolved"));
        services.AddScoped<IBackgroundJobExecutionService, BackgroundJobExecutionService>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAdministrationRepository, AdministrationRepository>();
        services.AddScoped<IAccountRiskRepository, AccountRiskRepository>();
        // Related to regions
        services.AddScoped<IAreaRepository, AreaRepository>();

        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<ICurrencyTransferRepository, CurrencyTransferRepository>();
        services.AddScoped<ICharacterActionRepository, CharacterActionRepository>();
        services.AddScoped<ICreatureRepository, CreatureRepository>();

        services.AddScoped<IColosseumRepository, ColosseumRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<ITournamentGroundsRepository, TournamentGroundsRepository>();

        services.AddScoped<IDungeonRunRepository, DungeonRunRepository>();
        services.AddScoped<IDungeonSigilAssemblyRepository, DungeonSigilAssemblyRepository>();
        services.AddScoped<ICharacterDungeonMasteryRepository, CharacterDungeonMasteryRepository>();

        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<IEconomyLedgerRepository, EconomyLedgerRepository>();
        services.AddScoped<IEquipmentSlotRepository, EquipmentSlotRepository>();
        services.AddScoped<IEssenceRepository, EssenceRepository>();

        services.AddScoped<IGuildRepository, GuildRepository>();

        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IItemBaseRepository, ItemBaseRepository>();

        services.AddScoped<IMarketPlaceRepository, MarketPlaceRepository>();
        services.AddScoped<IGameEventOutboxRepository, GameEventOutboxRepository>();

        services.AddScoped<IRegionRepository, RegionRepository>();

        services.AddScoped<IProfessionRepository, ProfessionRepository>();
        services.AddScoped<ICraftingRepository, CraftingRepository>();
        services.AddScoped<IProphecyRepository, ProphecyRepository>();
        services.AddScoped<IQuestRepository, QuestRepository>();
        services.AddScoped<IEventQuestRepository, EventQuestRepository>();

        services.AddScoped<IPlayerRepository, PlayerRepository>();

        services.AddScoped<ISoulstoneUpgradeRepository, SoulstoneUpgradeRepository>();

        // Snapshots
        services.AddScoped<ICharacterSnapshotRepository, CharacterSnapshotRepository>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();

        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<ITierPackageProvider, InMemoryTierPackageProvider>();

        return services;
    }
}
