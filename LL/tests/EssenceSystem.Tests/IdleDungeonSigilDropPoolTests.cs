using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Layers.Rewards.Idle;

namespace EssenceSystem.Tests;

public sealed class IdleDungeonSigilDropPoolTests
{
    [Fact]
    public void Meran_areas_include_the_future_Tangled_Cave_and_Great_Tree_sigils()
    {
        var apiRoot = FindApiRoot();
        var provider = new JsonIdleDungeonSigilDropPool(
            new ConfigurationBuilder().Build(),
            apiRoot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var expectedSigilIds = new[] { "sigil_tangled_cave", "sigil_great_tree" };

        Assert.Equal(expectedSigilIds, provider.GetAdditionalSigilIds("region_02_area_01"));
        Assert.Equal(expectedSigilIds, provider.GetAdditionalSigilIds("region_02_area_02"));
        Assert.Equal(expectedSigilIds, provider.GetAdditionalSigilIds("region_02_area_03"));
        Assert.Equal(expectedSigilIds, provider.GetAdditionalSigilIds("region_02_area_04"));
    }

    [Fact]
    public void Future_dungeon_sigils_have_seeded_item_definitions()
    {
        var apiRoot = FindApiRoot();
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "items", "items.json")));
        var items = document.RootElement.EnumerateArray()
            .ToDictionary(
                item => item.GetProperty("id").GetString()!,
                item => item.GetProperty("name").GetString()!,
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal("Silkbound Sigil", items["sigil_tangled_cave"]);
        Assert.Equal("Heartwood Sigil", items["sigil_great_tree"]);
    }

    [Fact]
    public void All_dungeon_sigil_item_definitions_are_bound()
    {
        var expectedSigilIds = new[]
        {
            "sigil_goblin_mines",
            "sigil_forgotten_catacombs",
            "sigil_hives_abyss",
            "sigil_tangled_cave",
            "sigil_great_tree"
        };
        var apiRoot = FindApiRoot();
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(apiRoot, "Data", "items", "items.json")));
        var sigils = document.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("id").GetString()!
                .StartsWith("sigil_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                item => item.GetProperty("id").GetString()!,
                item => item,
                StringComparer.OrdinalIgnoreCase);

        Assert.Equal(expectedSigilIds.Order(), sigils.Keys.Order());
        Assert.All(expectedSigilIds, sigilId =>
            Assert.True(
                sigils[sigilId].TryGetProperty("isBound", out var isBound) && isBound.GetBoolean(),
                $"Dungeon Sigil '{sigilId}' must be bound."));
    }

    private static string FindApiRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "API", "API.LL");
            if (Directory.Exists(Path.Combine(candidate, "Data")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the API.LL project root.");
    }
}
