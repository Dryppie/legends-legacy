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
