using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using Application.Interfaces.Services.LL.Essences;
using Domain.Components.Attributes;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Combat;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Services.LL.Essences;
using Services.LL.Combat.Engine;
using Services.LL.PowerRatings;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceFull")]
[Trait("BalanceShard", "Misc")]
public sealed class CanonicalEquipmentBuildFactoryTests
{
    private readonly CanonicalEquipmentBuildFactory _factory = CreateFactory();

    [Fact]
    public void Balance_simulator_handles_the_default_large_random_pool_with_canonical_equipment()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            apiRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var simulator = new AbilityBalanceSimulator(
            new JsonAbilityCatalogProvider(configuration, apiRoot, jsonOptions),
            essenceDefinitions,
            _factory);

        var request = new AbilityBalanceSimulationRequest(
            BattleCount: 100,
            TeamSize: 2,
            EssencesPerParticipant: 5,
            RandomSeed: 1337,
            TopResults: 1000,
            CandidatePoolSize: 1000,
            CandidateTeams: null);
        var report = simulator.Run(request);
        var repeated = simulator.Run(request);

        Assert.Equal(100, report.BattlesRun);
        Assert.Equal(1000, report.CandidateTeamCount);
        Assert.Equal(
            JsonSerializer.Serialize(report.RankedCombinations),
            JsonSerializer.Serialize(repeated.RankedCombinations));
        Assert.Equal(
            JsonSerializer.Serialize(report.EssenceResults),
            JsonSerializer.Serialize(repeated.EssenceResults));
        Assert.Equal(
            JsonSerializer.Serialize(report.BattleSummaries),
            JsonSerializer.Serialize(repeated.BattleSummaries));
        var physicalMitigation = AttributeCombatRules.CalculateDefenseMitigation(
            report.ParticipantAttributes[nameof(AttributeType.Armor)],
            report.ParticipantAttributes[nameof(AttributeType.ArmorPenetration)]);
        var magicalMitigation = AttributeCombatRules.CalculateDefenseMitigation(
            report.ParticipantAttributes[nameof(AttributeType.Resistance)],
            report.ParticipantAttributes[nameof(AttributeType.MagicPenetration)]);
        Assert.Equal(physicalMitigation, magicalMitigation, precision: 6);
        Assert.Equal(0.3f, physicalMitigation, precision: 6);
        Assert.Equal(0, report.ParticipantAttributes[nameof(AttributeType.ArmorPenetration)]);
        Assert.Equal(0, report.ParticipantAttributes[nameof(AttributeType.MagicPenetration)]);
        Assert.Equal(0, report.ParticipantAttributes[nameof(AttributeType.HealingPowerPercent)]);
    }

    [Fact]
    public void Ladder_is_deterministic_and_projects_authored_items_across_supported_equipment_tiers()
    {
        var first = _factory.GetProgressionLadder();
        var second = _factory.GetProgressionLadder();

        Assert.Equal(first, second);
        Assert.Equal(600, first.Count);
        Assert.Equal(
            Enumerable.Range(1, 100),
            first.Select(rung => rung.Tier).Distinct());
        Assert.All(first, rung => Assert.Equal(7, rung.EquippedSlotCount));
        Assert.Equal("t1-standard-common", first[0].Id);
        Assert.Equal("t100-standard-legendary", first[^1].Id);
        Assert.False(first.Single(rung => rung.Id == "t10-standard-common").UsesProjectedTierScaling);
        Assert.False(first.Single(rung => rung.Id == "t11-standard-common").UsesProjectedTierScaling);
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
        var expected = EntityBaseAttributeHelper
            .CreateEntityAttributesForLevel(Guid.Empty, build.Character.Level)
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
                Assert.Equal(1, essence.Level);
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

    [Fact]
    public void Tier_ten_profile_combat_ratings_remain_comparable()
    {
        var rung = _factory.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t10-standard-legendary");
        var builds = Enum.GetValues<CanonicalPartyProfile>()
            .ToDictionary(
                profile => profile,
                profile => _factory.CreateBuild(profile, rung));
        var ratings = builds.ToDictionary(x => x.Key, x => x.Value.Rating.Overall);
        var summary = string.Join(
            ", ",
            builds.Select(entry =>
            {
                var attributes = CombatRatingCalculator.ProjectDirectAttributes(
                    entry.Value.Character.BaseAttributes,
                    entry.Value.Equipment.SelectMany(equipment => equipment.AttributeModifiers));
                return $"{entry.Key}={entry.Value.Rating.Overall / 10}" +
                       $"(O{entry.Value.Rating.SingleTargetOffense / 10}/" +
                       $"P{entry.Value.Rating.PhysicalDurability / 10}/" +
                       $"M{entry.Value.Rating.MagicalDurability / 10}/" +
                       $"S{entry.Value.Rating.Sustain / 10};" +
                       $"Pow{attributes.GetValueOrDefault(AttributeType.Power):0.#}/" +
                       $"HP{attributes.GetValueOrDefault(AttributeType.MaxHealth):0.#}/" +
                       $"CC{attributes.GetValueOrDefault(AttributeType.CritChance):0.#}/" +
                       $"CD{attributes.GetValueOrDefault(AttributeType.CritDamage):0.#}/" +
                       $"AS{attributes.GetValueOrDefault(AttributeType.AttackSpeed):0.#}/" +
                       $"SR{attributes.GetValueOrDefault(AttributeType.StatusResistance):0.#})";
            }));

        var defensiveToOffense = ratings[CanonicalPartyProfile.Defensive]
                                 / (double)ratings[CanonicalPartyProfile.Offense];
        Assert.True(
            defensiveToOffense is >= 0.9 and <= 1.15,
            $"Tier-10 defensive/offense Combat Rating ratio was " +
            $"{defensiveToOffense:0.###}: {summary}.");
        Assert.True(
            ratings.Values.Min() >= ratings.Values.Max() * 0.75,
            $"Tier-10 canonical Combat Ratings diverged too far: {summary}.");
    }

    [Fact]
    public void Balanced_epic_basic_attack_pacing_is_stable_across_live_equipment_tiers()
    {
        var results = new[] { 1, 5, 10 }
            .Select(tier =>
            {
                var rung = _factory.GetProgressionLadder()
                    .Single(candidate => candidate.Id == $"t{tier}-standard-epic");
                var build = _factory.CreateBuild(CanonicalPartyProfile.Balanced, rung);
                var attributes = AttributeCalculator.CalculateProjectedAttributes(
                    build.Character.BaseAttributes.ToDictionary(
                        attribute => attribute.AttributeType,
                        attribute => attribute.Value),
                    build.Equipment
                        .SelectMany(equipment => equipment.AttributeModifiers)
                        .Cast<AttributeModifierBase>());
                attributes[AttributeType.Armor] = 30;
                attributes[AttributeType.Resistance] = 30;
                attributes[AttributeType.ArmorPenetration] = 0;
                attributes[AttributeType.MagicPenetration] = 0;
                attributes[AttributeType.HealthRegeneration] = 0;

                var durations = Enumerable.Range(1, 31)
                    .Select(seed => RunBasicAttackMirror(attributes, seed))
                    .Order()
                    .ToList();
                return new
                {
                    Tier = tier,
                    Power = attributes[AttributeType.Power],
                    MaxHealth = attributes[AttributeType.MaxHealth],
                    CritChance = attributes.GetValueOrDefault(AttributeType.CritChance),
                    CritDamage = attributes.GetValueOrDefault(AttributeType.CritDamage),
                    AttackSpeed = attributes.GetValueOrDefault(AttributeType.AttackSpeed),
                    MedianDuration = durations[durations.Count / 2]
                };
            })
            .ToList();
        var tierOneDuration = results[0].MedianDuration;
        var summary = string.Join(
            ", ",
            results.Select(result =>
                $"T{result.Tier}: {result.MedianDuration} ticks, " +
                $"P{result.Power:0.##}/HP{result.MaxHealth:0.##}/" +
                $"CC{result.CritChance:0.##}/CD{result.CritDamage:0.##}/" +
                $"AS{result.AttackSpeed:0.##}"));

        Assert.All(
            results,
            result => Assert.True(
                result.MedianDuration >= tierOneDuration * 0.8
                && result.MedianDuration <= tierOneDuration * 1.25,
                $"Balanced basic-attack pacing diverged by tier: {summary}."));
    }

    [Fact]
    public void Neutral_essence_battle_pacing_does_not_accelerate_across_equipment_tiers()
    {
        var simulator = CreateBalanceSimulator();
        var results = new[] { 1, 5, 10 }
            .Select(tier =>
            {
                var report = simulator.Run(new AbilityBalanceSimulationRequest(
                    BattleCount: 300,
                    TeamSize: 1,
                    EssencesPerParticipant: 5,
                    RandomSeed: 1337,
                    TopResults: 100,
                    CandidatePoolSize: 100,
                    CandidateTeams: null,
                    EquipmentTier: tier));
                var durations = report.BattleSummaries
                    .Select(battle => battle.Duration)
                    .Order()
                    .ToList();
                return new
                {
                    Tier = tier,
                    MedianDuration = durations[durations.Count / 2]
                };
            })
            .ToList();
        var tierOneDuration = results[0].MedianDuration;
        var summary = string.Join(
            ", ",
            results.Select(result => $"T{result.Tier}: {result.MedianDuration} ticks"));

        Assert.All(
            results,
            result => Assert.True(
                result.MedianDuration >= tierOneDuration * 0.65
                && result.MedianDuration <= tierOneDuration * 1.5,
                $"Neutral essence battle pacing diverged by tier: {summary}."));
    }

    [Fact]
    public void Tier_one_epic_profile_combat_ratings_remain_comparable()
    {
        var rung = _factory.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-standard-epic");
        var ratings = Enum.GetValues<CanonicalPartyProfile>()
            .ToDictionary(
                profile => profile,
                profile => _factory.CreateBuild(profile, rung).Rating.Overall);
        var summary = string.Join(
            ", ",
            ratings.Select(entry => $"{entry.Key}={entry.Value / 10}"));
        var expectedDisplayedRatings = new Dictionary<CanonicalPartyProfile, int>
        {
            [CanonicalPartyProfile.Balanced] = 133,
            [CanonicalPartyProfile.Offense] = 142,
            [CanonicalPartyProfile.Sustain] = 137,
            [CanonicalPartyProfile.Defensive] = 132,
            [CanonicalPartyProfile.Area] = 136
        };

        Assert.Equal(
            expectedDisplayedRatings,
            ratings.ToDictionary(entry => entry.Key, entry => entry.Value / 10));
        Assert.True(
            ratings.Values.Min() >= ratings.Values.Max() * 0.8,
            $"Tier-1 epic canonical Combat Ratings diverged too far: {summary}.");
    }

    [Fact]
    public void Tier_one_legendary_profile_ratings_are_stable()
    {
        var rung = _factory.GetProgressionLadder()
            .Single(candidate => candidate.Id == "t1-standard-legendary");
        var ratings = Enum.GetValues<CanonicalPartyProfile>()
            .ToDictionary(
                profile => profile,
                profile => _factory.CreateBuild(profile, rung).Rating.Overall / 10);
        var expectedDisplayedRatings = new Dictionary<CanonicalPartyProfile, int>
        {
            [CanonicalPartyProfile.Balanced] = 141,
            [CanonicalPartyProfile.Offense] = 145,
            [CanonicalPartyProfile.Sustain] = 138,
            [CanonicalPartyProfile.Defensive] = 133,
            [CanonicalPartyProfile.Area] = 141
        };

        Assert.Equal(expectedDisplayedRatings, ratings);
    }

    [Fact]
    public void Tutorial_starter_build_uses_only_the_common_mace_and_goblin_essence()
    {
        var build = _factory.CreateTutorialStarterBuild();

        Assert.Equal(1, build.Character.Level);
        Assert.Equal("mace", Assert.Single(build.Equipment).ItemBaseId);
        Assert.Null(build.Equipment[0].BaseRecipeId);
        Assert.Equal(Rarity.Common, build.Equipment[0].Rarity);
        Assert.Equal(
            "essence.goblin",
            Assert.Single(build.EquippedEssences).EssenceDefinitionId);
        Assert.Equal(
            10,
            build.Character.BaseAttributes.Single(attribute =>
                attribute.AttributeType == AttributeType.Power).Value);
        Assert.Equal(
            140,
            build.Character.BaseAttributes.Single(attribute =>
                attribute.AttributeType == AttributeType.MaxHealth).Value);
        Assert.Equal(47, build.Rating.Overall / 10);
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
                (essence.Id, essence.EssenceDefinitionId, essence.Level, essence.AscensionTier)),
            second.EquippedEssences.Select(essence =>
                (essence.Id, essence.EssenceDefinitionId, essence.Level, essence.AscensionTier)));
    }

    private static int RunBasicAttackMirror(
        IReadOnlyDictionary<AttributeType, float> attributes,
        int seed)
    {
        RuntimeCombatant Combatant(string id, CombatTeam team) =>
            new(
                id,
                id,
                team,
                attributes.ToDictionary(pair => pair.Key, pair => pair.Value),
                [],
                ["Role.Balance"]);

        var engine = new FastCombatEngine(
            new Dictionary<string, CompiledStatus>(),
            new FastCombatEngineOptions(MaxTicks: 6_000, RandomSeed: seed));
        return engine.Run(
            [Combatant("friendly", CombatTeam.Friendly)],
            [Combatant("hostile", CombatTeam.Hostile)]).Duration;
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

    private static AbilityBalanceSimulator CreateBalanceSimulator()
    {
        var apiRoot = FindApiRoot();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "Data" })
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            apiRoot,
            jsonOptions,
            new EssenceDefinitionValidator());

        return new AbilityBalanceSimulator(
            new JsonAbilityCatalogProvider(configuration, apiRoot, jsonOptions),
            essenceDefinitions,
            CreateFactory());
    }

    private static string FindApiRoot([CallerFilePath] string sourceFilePath = "")
    {
        foreach (var start in new[]
                 {
                     new DirectoryInfo(AppContext.BaseDirectory),
                     new DirectoryInfo(Directory.GetCurrentDirectory()),
                     new FileInfo(sourceFilePath).Directory!
                 })
        {
            var current = start;
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
        }

        throw new DirectoryNotFoundException("Could not find the API.LL content root.");
    }
}
