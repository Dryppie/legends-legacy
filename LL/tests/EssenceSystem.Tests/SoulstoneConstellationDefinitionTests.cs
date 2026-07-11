using Domain.Models.Soulstones.UpgradeDefinition;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class SoulstoneConstellationDefinitionTests
{
    [Fact]
    public void Catalog_contains_only_supported_active_constellations()
    {
        var definitions = LoadDefinitions();
        var enabledIds = definitions.Where(x => x.Enabled).Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var disabledIds = definitions.Where(x => !x.Enabled).Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        string[] expectedEnabledIds =
        [
                "essence.resonance",
                "essence.echo-memory",
                "essence.duplicate-echoes",
                "essence.archive-focus",
                "combat.battle-lessons",
                "combat.survival-notes",
                "gathering.careful-harvest",
                "gathering.gathering-lessons",
                "gathering.rare-node-sense",
                "crafting.crafting-lessons",
                "crafting.steady-temper",
                "crafting.blueprint-study",
                "dungeon.sigil-traces",
                "dungeon.checkpoint-satchel"
        ];

        Assert.Equal(
            expectedEnabledIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase),
            enabledIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

        Assert.Empty(disabledIds);
    }

    [Fact]
    public void Catalog_uses_plan_basis_point_values_for_active_v1_upgrades()
    {
        var definitions = LoadDefinitions().ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        AssertValues(definitions["essence.resonance"], [300, 600, 900, 1200, 1500]);
        AssertValues(definitions["essence.echo-memory"], [500, 1000, 1500, 2000, 2500]);
        AssertValues(definitions["essence.duplicate-echoes"], [200, 400, 600, 800, 1000]);
        AssertValues(definitions["essence.archive-focus"], [500, 1000, 1500, 2000, 2500]);
        AssertValues(definitions["combat.battle-lessons"], [150, 300, 450, 600, 750]);
        AssertValues(definitions["combat.survival-notes"], [1000, 2000, 3000, 4000, 5000]);
        AssertValues(definitions["gathering.careful-harvest"], [100, 200, 300, 400, 500]);
        AssertValues(definitions["gathering.gathering-lessons"], [150, 300, 450, 600, 750]);
        AssertValues(definitions["gathering.rare-node-sense"], [200, 400, 600, 800, 1000]);
        AssertValues(definitions["crafting.crafting-lessons"], [150, 300, 450, 600, 750]);
        AssertValues(definitions["crafting.steady-temper"], [30, 60, 90, 120, 150]);
        AssertValues(definitions["crafting.blueprint-study"], [200, 400, 600, 800, 1000]);
        AssertValues(definitions["dungeon.sigil-traces"], [150, 300, 450, 600, 750]);
        AssertValues(definitions["dungeon.checkpoint-satchel"], [200, 400, 600, 800, 1000]);
    }

    [Fact]
    public void Catalog_does_not_include_removed_legacy_soulstone_economy_upgrades()
    {
        var ids = LoadDefinitions().Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("misc.soulstone.drop.rate", ids);
        Assert.DoesNotContain("misc.soulstone.double.drop.chance", ids);
        Assert.DoesNotContain("combat.double.exp.chance", ids);
        Assert.DoesNotContain("gathering.double.drop.chance", ids);
        Assert.DoesNotContain("gathering.double.exp.chance", ids);
        Assert.DoesNotContain("crafting.double.item.exp.chance", ids);
        Assert.DoesNotContain("combat.veteran-rhythm", ids);
        Assert.DoesNotContain("crafting.salvage-instinct", ids);
        Assert.DoesNotContain("dungeon.cartographers-eye", ids);
        Assert.DoesNotContain("dungeon.reward-lens", ids);
        Assert.DoesNotContain("convenience.archive-presets", ids);
    }

    private static void AssertValues(SoulstoneUpgradeDefinition definition, int[] expected)
    {
        var effect = Assert.Single(definition.Effects);
        Assert.Equal(expected, effect.ValuesByRank);
        Assert.Equal(definition.MaxRank, definition.CostsByRank.Count);
        Assert.Equal(definition.MaxRank, effect.ValuesByRank.Count);
    }

    private static IReadOnlyList<SoulstoneUpgradeDefinition> LoadDefinitions()
    {
        var path = FindDefinitionPath();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var definitions = JsonSerializer.Deserialize<List<SoulstoneUpgradeDefinition>>(
            File.ReadAllText(path),
            options);

        Assert.NotNull(definitions);
        return definitions;
    }

    private static string FindDefinitionPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidates = new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL", "Data", "progression", "soulstone-upgrades.json"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL", "Data", "progression", "soulstone-upgrades.json")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate soulstone-upgrades.json from the test output directory.");
    }
}
