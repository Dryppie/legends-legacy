using Domain.Models.Attributes;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL;
using Services.LL.Combat.Engine;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class AttributeRolloutRegistrationTests
{
    public static IEnumerable<object[]> ShippedHostSettings()
    {
        foreach (var host in new[] { "API/API.LL", "Worker/Worker.LL", "API/API.LiveOps", "API/API.AdminDashboard" })
            foreach (var environment in new[] { "Development", "Production" })
                yield return [host, environment];
    }

    [Theory]
    [MemberData(nameof(ShippedHostSettings))]
    public void Shipped_settings_enable_the_same_attributes_equipment_and_healing(string host, string environment)
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var sourceRoot = Path.GetFullPath(Path.Combine(apiRoot, "..", ".."));
        var configuration = new ConfigurationBuilder().SetBasePath(Path.Combine(sourceRoot, host))
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = Path.Combine(apiRoot, "Data") })
            .Build();
        var services = new ServiceCollection();
        if (host == "API/API.LiveOps") services.AddLiveOpsServices(configuration);
        else services.AddServices(configuration, apiRoot);
        using var provider = services.BuildServiceProvider();
        var rules = provider.GetRequiredService<AttributeRulesSelection>();
        var catalog = provider.GetRequiredService<StarterEquipmentCatalog>();
        var abilities = provider.GetRequiredService<IAbilityCatalogProvider>().GetCatalog();
        Assert.Equal(18, rules.Version);
        Assert.Equal(4, rules.EquipmentBalanceVersion);
        if (host == "API/API.LL")
        {
            Assert.True(configuration.GetValue<bool>("EquipmentConversion:RunOnStartup"));
            Assert.Equal(4, configuration.GetValue<int>("EquipmentConversion:TargetBalanceVersion"));
        }
        Assert.Equal(4, catalog.Evaluator.Balance.Version);
        Assert.Equal(4, provider.GetRequiredService<EquipmentMigrationCatalog>().Get(null).Evaluator.Balance.Version);
        Assert.Equal(1.5, catalog.Evaluator.Balance.GetMaterializedCostPerPoint(AttributeType.Restoration, 1));
        Assert.Equal(1.05f, abilities.AbilitiesById["ability.creature.lizardfolk_shaman.herb_mixture"].Effects[0].ScalingCoefficient);
        Assert.Equal(1.25f, abilities.AbilitiesById["ability.creature.treant_sapling.sprouting_surge"].Effects[0].ScalingCoefficient);
    }

    [Theory]
    [InlineData(false, 3, null, 2.1f)]
    [InlineData(true, 3, null, 2.1f)]
    [InlineData(false, 4, "healing-v1", 1.05f)]
    [InlineData(true, 4, "healing-v1", 1.05f)]
    public void Game_and_operator_hosts_resolve_matching_rollout_catalogs(
        bool liveOps, int equipmentVersion, string? abilityProfile, float herbCoefficient)
    {
        var root = TestContentPaths.FindApiRoot();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Content:Root"] = Path.Combine(root, "Data"),
            ["AttributeRedesign:LiveVersion"] = "18",
            ["EquipmentBalance:LiveVersion"] = equipmentVersion.ToString(),
            ["Combat:AbilityBalanceProfile"] = abilityProfile
        }).Build();
        var services = new ServiceCollection();
        if (liveOps) services.AddLiveOpsServices(configuration);
        else services.AddServices(configuration, root);
        using var provider = services.BuildServiceProvider();
        var rules = provider.GetRequiredService<AttributeRulesSelection>();
        var equipment = provider.GetRequiredService<StarterEquipmentCatalog>();
        var abilities = provider.GetRequiredService<IAbilityCatalogProvider>().GetCatalog();
        Assert.Equal(18, rules.Version);
        Assert.Equal(equipmentVersion, rules.EquipmentBalanceVersion);
        Assert.Equal(equipmentVersion, equipment.Evaluator.Balance.Version);
        Assert.Equal(equipmentVersion, provider.GetRequiredService<EquipmentMigrationCatalog>().Get(null).Evaluator.Balance.Version);
        Assert.Equal(18, equipment.Evaluator.Balance.AttributeVersion);
        Assert.Equal(4, equipment.Evaluator.Balance.GetMaterializedCostPerPoint(AttributeType.ArmorPenetration, 1));
        Assert.Equal(equipmentVersion == 4 ? 1.5 : 3, equipment.Evaluator.Balance.GetMaterializedCostPerPoint(AttributeType.Restoration, 1));
        Assert.Equal(herbCoefficient, abilities.AbilitiesById["ability.creature.lizardfolk_shaman.herb_mixture"].Effects[0].ScalingCoefficient);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Legacy_staging_still_previews_the_first_modern_release(bool liveOps)
    {
        var root = TestContentPaths.FindApiRoot();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Content:Root"] = Path.Combine(root, "Data"),
            ["AttributeRedesign:LiveVersion"] = "17",
            ["EquipmentBalance:LiveVersion"] = "1"
        }).Build();
        var services = new ServiceCollection();
        if (liveOps) services.AddLiveOpsServices(configuration);
        else services.AddServices(configuration, root);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(2, provider.GetRequiredService<EquipmentMigrationCatalog>().Get(null).Evaluator.Balance.Version);
        Assert.Equal(4, provider.GetRequiredService<EquipmentMigrationCatalog>().Get(4).Evaluator.Balance.Version);
    }
}
