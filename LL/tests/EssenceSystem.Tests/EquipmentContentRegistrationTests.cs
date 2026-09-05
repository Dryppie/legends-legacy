using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL;

namespace EssenceSystem.Tests;

public sealed class EquipmentContentRegistrationTests
{
    public static IEnumerable<object?[]> ContentRoots()
    {
        var apiRoot = TestContentPaths.FindApiRoot();
        var parentRoot = Path.GetDirectoryName(apiRoot)!;
        yield return new object?[] { apiRoot, null };
        yield return new object?[] { parentRoot, Path.Combine(Path.GetFileName(apiRoot), "Data") };
        yield return new object?[] { parentRoot, Path.Combine(apiRoot, "Data") };
    }

    [Theory]
    [MemberData(nameof(ContentRoots))]
    public void Equipment_catalogs_load_through_service_registration(
        string hostContentRoot, string? configuredContentRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = configuredContentRoot
            })
            .Build();
        var services = new ServiceCollection();
        services.AddServices(configuration, hostContentRoot);

        using var provider = services.BuildServiceProvider();
        var equipment = provider.GetRequiredService<StarterEquipmentCatalog>();
        var ordinary = provider.GetRequiredService<CombatAcquisitionCatalog>();

        Assert.Same(equipment, ordinary.Equipment);
        Assert.Equal(new[] { 1, 2 }, ordinary.Pools.Select(p => p.EquipmentTier).Order());
        var dungeons = provider.GetRequiredService<Application.Interfaces.Services.LL.Dungeons.IDungeonDefinitions>();
        foreach (var pool in ordinary.Pools)
        {
            Assert.NotEmpty(pool.Areas);
            Assert.Equal(31, equipment.GetOptions(pool.EquipmentTier).Count);
            foreach (var sigil in pool.Sigils)
            {
                var dungeon = dungeons.GetByKey(sigil.FamilyId);
                Assert.Equal(dungeon.SigilItemId, sigil.ItemBaseId);
            }
        }
        Assert.All(Enum.GetValues<EquipmentRarity>(), rarity =>
            Assert.True(ordinary.DropDefinitions(rarity).Count >= 31));
    }
}
