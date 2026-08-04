using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Helpers;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Essences;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CanonicalEquipmentBuildFactoryTests
{
    private readonly CanonicalEquipmentBuildFactory _factory = CreateFactory();

    [Fact]
    public void Ladder_is_deterministic_and_projects_authored_items_across_supported_equipment_tiers()
    {
        var first = _factory.GetProgressionLadder();
        var second = _factory.GetProgressionLadder();

        Assert.Equal(first, second);
        Assert.Equal(120, first.Count);
        Assert.Equal(
            Enumerable.Range(1, 20),
            first.Select(rung => rung.Tier).Distinct());
        Assert.All(first, rung => Assert.Equal(7, rung.EquippedSlotCount));
        Assert.Equal("t1-standard-common", first[0].Id);
        Assert.Equal("t20-standard-legendary", first[^1].Id);
        Assert.False(first.Single(rung => rung.Id == "t10-standard-common").UsesProjectedTierScaling);
        Assert.True(first.Single(rung => rung.Id == "t11-standard-common").UsesProjectedTierScaling);
        Assert.Equal(first.Count, first.Select(rung => rung.Id).Distinct().Count());
        Assert.All(first, rung => Assert.Equal(ItemQuality.Standard, rung.Quality));
        Assert.All(
            first.GroupBy(rung => rung.Tier),
            tier => Assert.Equal(
                [
                    Rarity.Common,
                    Rarity.Uncommon,
                    Rarity.Rare,
                    Rarity.Epic,
                    Rarity.Unique,
                    Rarity.Legendary
                ],
                tier.Select(rung => rung.Rarity)));
    }

    [Fact]
    public void Every_matrix_rung_uses_a_complete_authored_equipment_set()
    {
        foreach (var rung in _factory.GetProgressionLadder())
        {
            var build = _factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);

            Assert.Equal(7, build.Equipment.Count);
            Assert.Equal(
                "recipe.weapon.two_handed.greatsword",
                build.MainHandRecipeId);
        }
    }

    [Fact]
    public void Canonical_build_uses_real_character_attributes()
    {
        var rung = _factory.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-standard-common");
        var build = _factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
        var expected = EntityBaseAttributeHelper.CreateEntityAttributes(Guid.Empty)
            .ToDictionary(attribute => attribute.AttributeType, attribute => attribute.Value);
        var actual = build.Character.BaseAttributes
            .ToDictionary(attribute => attribute.AttributeType, attribute => attribute.Value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Every_profile_uses_authored_items_and_two_region_one_essences()
    {
        var rung = _factory.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-standard-common");
        var builds = Enum.GetValues<CanonicalPartyProfile>()
            .Select(profile => _factory.CreateBuild(profile, rung))
            .ToList();

        Assert.All(builds, build =>
        {
            Assert.Equal(7, build.Equipment.Count);
            Assert.All(build.Equipment, item =>
            {
                Assert.False(string.IsNullOrWhiteSpace(item.ItemBaseId));
                Assert.False(string.IsNullOrWhiteSpace(item.BaseRecipeId));
                Assert.NotNull(item.ItemBase);
                Assert.DoesNotContain("canonical", item.ItemBaseId, StringComparison.OrdinalIgnoreCase);
            });
            Assert.Equal(2, build.EquippedEssences.Count);
            Assert.All(build.EquippedEssences, essence =>
            {
                Assert.Equal(1, essence.NativeRegion);
                Assert.Equal(1, essence.Level);
                Assert.Equal(1, essence.PotentialTier);
            });
            Assert.Equal(EquipmentStatBudgetCatalog.BalanceVersion, build.EquipmentBalanceVersion);
        });

        var balanced = builds.Single(build => build.Profile == CanonicalPartyProfile.Balanced);
        Assert.Equal(
            [
                "medium_mail",
                "greatsword",
                "medium_helm",
                "medium_greaves",
                "band",
                "amulet",
                "vial"
            ],
            balanced.Equipment.Select(item => item.ItemBaseId));
        Assert.Equal(
            ["essence.goblin", "essence.vampire_bat"],
            balanced.EquippedEssences.Select(essence => essence.EssenceDefinitionId));
    }

    [Theory]
    [InlineData(
        CanonicalPartyProfile.Balanced,
        "medium_mail",
        "greatsword",
        "essence.goblin",
        "essence.vampire_bat",
        10)]
    [InlineData(
        CanonicalPartyProfile.Offense,
        "light_vest",
        "gauntlets",
        "essence.goblin_archer",
        "essence.glade_panther",
        15)]
    [InlineData(
        CanonicalPartyProfile.Sustain,
        "cloth_robe",
        "staff",
        "essence.enchanted_fairy",
        "essence.pixie",
        15)]
    [InlineData(
        CanonicalPartyProfile.Defensive,
        "heavy_breastplate",
        "maul",
        "essence.brown_slime",
        "essence.goblin_warrior",
        10)]
    [InlineData(
        CanonicalPartyProfile.Area,
        "cloth_robe",
        "staff",
        "essence.flame_imp",
        "essence.pixie",
        15)]
    public void Profile_loadout_identity_is_explicit_and_reproducible(
        CanonicalPartyProfile profile,
        string chestItemBaseId,
        string weaponItemBaseId,
        string firstEssenceId,
        string secondEssenceId,
        int characterLevel)
    {
        var rung = _factory.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-standard-common");
        var build = _factory.CreateBuild(profile, rung);

        Assert.Equal(chestItemBaseId, build.Equipment[0].ItemBaseId);
        Assert.Equal(weaponItemBaseId, build.Equipment[1].ItemBaseId);
        Assert.Equal(characterLevel, build.Character.Level);
        Assert.Equal(
            [firstEssenceId, secondEssenceId],
            build.EquippedEssences.Select(essence => essence.EssenceDefinitionId));
    }

    [Fact]
    public void Combat_rating_uses_actual_loadout_attributes_and_rises_with_equipment_tier()
    {
        foreach (var profile in Enum.GetValues<CanonicalPartyProfile>())
        {
            foreach (var milestone in new[]
                     {
                         "standard-common",
                         "standard-uncommon",
                         "standard-rare",
                         "standard-epic",
                         "standard-unique",
                         "standard-legendary"
                     })
            {
                var ratings = Enumerable.Range(1, 20)
                    .Select(tier =>
                    {
                        var rung = _factory.GetProgressionLadder()
                            .Single(candidate => candidate.Id == $"t{tier}-{milestone}");
                        return (rung.Id, _factory.CreateBuild(profile, rung).Rating.Overall);
                    })
                    .ToList();

                Assert.All(ratings, item => Assert.True(item.Overall > 0));
                Assert.All(
                    ratings.Zip(ratings.Skip(1)),
                    pair => Assert.True(
                        pair.Second.Overall > pair.First.Overall,
                        $"{profile} Combat Rating did not increase: " +
                        $"{pair.First.Id} {pair.First.Overall} -> " +
                        $"{pair.Second.Id} {pair.Second.Overall}."));
            }
        }
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 6)]
    public void Dungeon_tier_profiles_use_the_allowed_number_of_real_essences(
        int dungeonTier,
        int expectedEssenceCount)
    {
        var rung = _factory.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-standard-common");
        var slotUnlocks = new EssenceSlotUnlockService();

        foreach (var profile in Enum.GetValues<CanonicalPartyProfile>())
        {
            var build = _factory.CreateBuildForDungeonTier(profile, rung, dungeonTier);

            Assert.Equal(expectedEssenceCount, build.EquippedEssences.Count);
            Assert.Equal(
                expectedEssenceCount,
                build.EquippedEssences.Select(essence => essence.EssenceDefinitionId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
            Assert.All(build.EquippedEssences, essence => Assert.Equal(1, essence.NativeRegion));
            Assert.True(
                slotUnlocks.GetUnlockedSlotCount(build.Character.Level) >= expectedEssenceCount,
                $"Level {build.Character.Level} cannot equip {expectedEssenceCount} Essences.");
        }
    }

    [Fact]
    public void Builds_are_deterministic()
    {
        var rung = _factory.GetProgressionLadder()[^1];
        var first = _factory.CreateBuild(CanonicalPartyProfile.Sustain, rung);
        var second = _factory.CreateBuild(CanonicalPartyProfile.Sustain, rung);

        Assert.Equal(first.Rating, second.Rating);
        Assert.Equal(
            first.Equipment.Select(item =>
                (item.Id, item.ItemBaseId, item.BaseRecipeId, item.Rarity, item.Quality)),
            second.Equipment.Select(item =>
                (item.Id, item.ItemBaseId, item.BaseRecipeId, item.Rarity, item.Quality)));
        Assert.Equal(
            first.Equipment.SelectMany(item => item.InstanceModifiers)
                .Select(modifier =>
                    (modifier.AttributeType, modifier.Amount, modifier.ModifierType)),
            second.Equipment.SelectMany(item => item.InstanceModifiers)
                .Select(modifier =>
                    (modifier.AttributeType, modifier.Amount, modifier.ModifierType)));
        Assert.Equal(
            first.EquippedEssences.Select(essence =>
                (essence.Id, essence.EssenceDefinitionId, essence.Level, essence.PotentialTier)),
            second.EquippedEssences.Select(essence =>
                (essence.Id, essence.EssenceDefinitionId, essence.Level, essence.PotentialTier)));
    }

    private static CanonicalEquipmentBuildFactory CreateFactory()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var balance = Options.Create(new CraftingBalanceOptions());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            apiRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var creatureEssences = new JsonCreatureEssenceLootTableRepository(
            configuration,
            apiRoot,
            jsonOptions,
            essenceDefinitions);
        var essenceResolver = new EssenceSystemService(
            null!,
            null!,
            null!,
            essenceDefinitions,
            creatureEssences,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        return new CanonicalEquipmentBuildFactory(
            new JsonCraftingDefinitionProvider(configuration, apiRoot, jsonOptions),
            new ItemStatRollService(balance),
            new TemperingMechanicsService(balance),
            new ItemPotentialService(balance),
            essenceResolver,
            essenceDefinitions);
    }

    private static string FindApiRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "src", "API", "API.LL"),
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (Directory.Exists(Path.Combine(candidate, "Data")))
                    return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the API.LL content root.");
    }
}
