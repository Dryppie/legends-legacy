using API.LiveOps.Hosting;
using Application.MediatR.Behaviors;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Administration;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.WebSockets;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Equipments.Dtos;
using Application.UseCases.Essences.Dtos;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Domain.Models.Administration;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Services.LL;
using Services.LL.Administration;

namespace EssenceSystem.Tests;

public sealed class LiveOpsApplicationRegistrationTests
{
    [Fact]
    public void LiveOps_registers_only_administration_request_handlers()
    {
        var services = new ServiceCollection();

        services.AddLiveOpsApplication();

        var handlers = services
            .Where(descriptor =>
                descriptor.ServiceType.IsGenericType &&
                (descriptor.ServiceType.GetGenericTypeDefinition() ==
                    typeof(IRequestHandler<>) ||
                 descriptor.ServiceType.GetGenericTypeDefinition() ==
                    typeof(IRequestHandler<,>)))
            .ToList();

        Assert.Equal(19, handlers.Count);
        Assert.Contains(handlers, descriptor => descriptor.ImplementationType == typeof(Application.UseCases.Administration.Queries.GetCompensationEquipmentOptions.GetCompensationEquipmentOptionsQueryHandler));
        Assert.All(handlers, descriptor => Assert.StartsWith(
            "Application.UseCases.Administration",
            descriptor.ImplementationType?.Namespace,
            StringComparison.Ordinal));
    }

    [Fact]
    public void LiveOps_pipeline_propagates_unexpected_exceptions_to_the_host_boundary()
    {
        var services = new ServiceCollection();

        services.AddLiveOpsApplication();

        var pipelineBehaviors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .ToArray();

        var applicationBehaviors = pipelineBehaviors
            .Select(descriptor => descriptor.ImplementationType!)
            .Where(type =>
                type is not null &&
                type.Assembly == typeof(TransactionBehavior<,>).Assembly)
            .ToArray();

        Assert.Equal([typeof(TransactionBehavior<,>)], applicationBehaviors);
    }

    [Fact]
    public void LiveOps_registers_state_sync_and_outbox_realtime_dependencies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddLiveOpsServices(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IStateSyncService));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IGameRealtimeBroadcaster));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAccountTemporalCorrelationService));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveOps_registration_paths_register_shared_administration_services_once(
        bool registerFullGameServices)
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = RegisterServices(configuration, registerFullGameServices);

        AssertSingleRegistration<IAccountRestrictionIndex>(services, ServiceLifetime.Singleton);
        AssertSingleRegistration<IAccountAccessPolicy>(services, ServiceLifetime.Scoped);
        AssertSingleRegistration<ILiveOpsService>(services, ServiceLifetime.Scoped);
        AssertSingleRegistration<ILiveOpsAccountRiskService>(services, ServiceLifetime.Scoped);
        AssertSingleRegistration<IAccountTemporalCorrelationService>(services, ServiceLifetime.Scoped);
        AssertSingleRegistration<IChatModerationGateway>(services, ServiceLifetime.Scoped);
        AssertSingleRegistration<TimeProvider>(services, ServiceLifetime.Singleton);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveOps_registration_paths_validate_temporal_correlation_options(
        bool registerFullGameServices)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AccountTemporalCorrelationOptions.SectionName}:AnalysisVersion"] = "0"
            })
            .Build();
        var services = RegisterServices(configuration, registerFullGameServices);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AccountTemporalCorrelationOptions>>();

        Assert.Throws<OptionsValidationException>(() => options.Value);
    }

    [Fact]
    public void LiveOps_registers_application_mapping_dependencies()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();
        services.AddLiveOpsApplication();
        services.AddLiveOpsServices(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEssenceDefinitionRepository>());
        Assert.NotNull(provider.GetRequiredService<IEssenceProgressionService>());
        Assert.NotNull(provider.GetRequiredService<EssenceLoadoutConverter>());
        Assert.NotNull(provider.GetRequiredService<PlayerEssenceArchiveEntryConverter>());
        Assert.NotNull(provider.GetRequiredService<CharacterOverviewConverter>());
    }

    [Fact]
    public void LiveOps_registers_administration_collection_mappings()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLiveOpsApplication();

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        IReadOnlyList<PlayerAdministrationSnapshot> players =
        [
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "local-account",
                "operator@example.test",
                "Admin",
                42,
                new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc),
                null,
                null,
                null,
                null,
                null,
                null)
        ];

        var result = mapper.Map<IReadOnlyList<PlayerAdministrationDto>>(players);

        var player = Assert.Single(result);
        Assert.Equal(players[0].AccountId, player.AccountId);
        Assert.Equal("Admin", player.CharacterName);
    }

    [Fact]
    public void LiveOps_registers_compensation_inventory_item_mappings()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLiveOpsApplication();

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        var resourceInstanceId = Guid.NewGuid();
        var equipmentInstanceId = Guid.NewGuid();
        var essenceInstanceId = Guid.NewGuid();
        var itemBase = new ItemBase
        {
            Id = "iron_ore",
            Name = "Iron Ore",
            ItemType = ItemType.Resource
        };
        var inventoryItems = new List<InventoryItem>
        {
            new()
            {
                ItemInstanceId = resourceInstanceId,
                ItemInstance = new ItemInstance
                {
                    Id = resourceInstanceId,
                    ItemBaseId = itemBase.Id,
                    ItemBase = itemBase
                },
                Quantity = 10
            },
            new()
            {
                ItemInstanceId = equipmentInstanceId,
                ItemInstance = new EquipmentInstance
                {
                    Id = equipmentInstanceId,
                    ItemBaseId = "iron_sword",
                    ItemBase = new EquipmentBase
                    {
                        Id = "iron_sword",
                        Name = "Iron Sword",
                        EquipmentType = EquipmentType.OneHanded
                    }
                },
                Quantity = 1
            },
            new()
            {
                ItemInstanceId = essenceInstanceId,
                ItemInstance = new EssenceItemInstance
                {
                    Id = essenceInstanceId,
                    ItemBaseId = "item.ember_essence",
                    ItemBase = new EssenceItemBase
                    {
                        Id = "item.ember_essence",
                        Name = "Ember Essence",
                        EssenceDefinitionId = "ember_essence"
                    }
                },
                Quantity = 1
            }
        };

        var result = mapper.Map<IReadOnlyList<InventoryItemDto>>(inventoryItems);

        Assert.Equal(3, result.Count);
        Assert.Equal(resourceInstanceId, result[0].ItemInstanceId);
        Assert.Equal("iron_ore", result[0].ItemInstance.ItemBase.Id);
        Assert.Equal(10, result[0].Quantity);
        Assert.IsType<EquipmentInstanceDto>(result[1].ItemInstance);
        Assert.IsType<EssenceItemInstanceDto>(result[2].ItemInstance);
    }

    private static ServiceCollection RegisterServices(
        IConfiguration configuration,
        bool registerFullGameServices)
    {
        var services = new ServiceCollection();
        if (registerFullGameServices)
        {
            services.AddServices(configuration, Directory.GetCurrentDirectory());
        }
        else
        {
            services.AddLiveOpsServices(configuration);
        }

        return services;
    }

    private static void AssertSingleRegistration<TService>(
        ServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType == typeof(TService));
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }
}
