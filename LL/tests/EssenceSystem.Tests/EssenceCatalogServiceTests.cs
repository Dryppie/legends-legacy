using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using Services.LL.Essences;

namespace EssenceSystem.Tests;

public sealed class EssenceCatalogServiceTests
{
    [Fact]
    public async Task Catalog_includes_Meran_essences_with_item_and_ability_metadata()
    {
        var apiRoot = FindApiContentRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var definitions = new JsonEssenceDefinitionRepository(
            configuration,
            apiRoot,
            options,
            new EssenceDefinitionValidator());
        var lootTables = new JsonCreatureEssenceLootTableRepository(
            configuration,
            apiRoot,
            options,
            definitions);
        var service = new EssenceCatalogService(
            new CatalogItemBaseRepository(LoadExplicitEssenceItemIds(apiRoot)),
            definitions,
            lootTables,
            new JsonAbilityCatalogProvider(configuration, apiRoot, options));

        var report = await service.GetCatalogAsync(CancellationToken.None);

        Assert.Equal(["Shenic", "Meran"], report.Regions.Select(region => region.Name));
        var meran = report.Regions.Single(region => region.Id == "region_02");
        Assert.Equal(
            ["Warfang Frontier", "Rotgrave Fields", "Tempest Aerie", "Wolfsbane Reach", "Tangled Cave", "The Great Tree", "Sanguine Horror"],
            meran.Areas.Select(area => area.Name));
        Assert.All(meran.Areas.Where(area => area.SourceType == "Idle Area"), area => Assert.Equal("T2", area.Tier));
        Assert.All(meran.Areas.Where(area => area.SourceType == "Dungeon"), area => Assert.Equal("T1-T3", area.Tier));
        Assert.Equal("T2-T3", meran.Areas.Single(area => area.SourceType == "Raid").Tier);

        var essences = meran.Areas
            .SelectMany(area => area.Monsters)
            .SelectMany(monster => monster.Essences)
            .ToList();
        Assert.Equal(27, essences.Count);
        Assert.All(essences, essence =>
        {
            Assert.Equal($"item.{essence.Id}", essence.ItemId);
            Assert.NotNull(essence.ActiveAbility);
            Assert.NotNull(essence.PassiveAbility);
            Assert.False(string.IsNullOrWhiteSpace(essence.ActiveAbility.Name));
            Assert.False(string.IsNullOrWhiteSpace(essence.ActiveAbility.Description));
            Assert.False(string.IsNullOrWhiteSpace(essence.PassiveAbility.Name));
            Assert.False(string.IsNullOrWhiteSpace(essence.PassiveAbility.Description));
        });
    }

    private static IReadOnlyDictionary<string, string> LoadExplicitEssenceItemIds(string apiRoot)
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "items", "items.json")));

        return document.RootElement
            .EnumerateArray()
            .Where(item =>
                item.TryGetProperty("essenceDefinitionId", out var definitionId) &&
                !string.IsNullOrWhiteSpace(definitionId.GetString()))
            .ToDictionary(
                item => item.GetProperty("essenceDefinitionId").GetString()!,
                item => item.GetProperty("id").GetString()!,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var apiPath in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (File.Exists(Path.Combine(apiPath, "Data", "essences", "essences.json")))
                    return apiPath;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate LL/src/API/API.LL/Data.");
    }

    private sealed class CatalogItemBaseRepository(
        IReadOnlyDictionary<string, string> itemIdsByEssenceId) : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(itemIdsByEssenceId);

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddMissingItemBasesAsync(
            IReadOnlyCollection<ItemBase> itemBases,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
