using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Application.UseCases.Items.Dtos;
using Domain.Models.Attributes;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;
using Services.LL.Professions.Craftings;

namespace EssenceSystem.Tests;

public sealed class CraftingCompositionContentTests
{
    [Fact]
    public void ProviderLoadsAndValidatesConcreteRecipesAndReusableBlueprints()
    {
        var provider = CreateProvider();

        Assert.Equal(31, provider.GetRecipes().Count);
        Assert.Equal(13, provider.GetBlueprints().Count);
        Assert.Equal(11, provider.GetEquipmentSets().Count);
        Assert.Equal(31, provider.GetRecipes().Select(recipe => recipe.OutputItemId).Distinct().Count());
        Assert.All(provider.GetRecipes(), recipe =>
        {
            Assert.Equal(2, recipe.TierRange.Max);
            Assert.Equal(1, recipe.MinimumProfessionLevel);
        });
        Assert.Equal("copper_ore", Assert.IsType<MaterialDefinition>(
            provider.GetStandardMaterial(MaterialFamily.Metal, 2)).ItemId);
        Assert.Equal("bloodwood", Assert.IsType<MaterialDefinition>(
            provider.GetStandardMaterial(MaterialFamily.Wood, 2)).ItemId);
        Assert.Equal("thick_hide", Assert.IsType<MaterialDefinition>(
            provider.GetStandardMaterial(MaterialFamily.Hide, 2)).ItemId);
    }

    [Fact]
    public void ProductionBlueprintsAssignSetsExceptDeferredRaidBlueprints()
    {
        var provider = CreateProvider();
        var blueprints = provider.GetBlueprints().ToDictionary(blueprint => blueprint.Id);

        Assert.All(
            blueprints.Values.Where(blueprint => blueprint.Id is not
                "blueprint_raidforged" and not "blueprint_gravebound"),
            blueprint => Assert.False(string.IsNullOrWhiteSpace(blueprint.EquipmentSetId)));
        Assert.Null(blueprints["blueprint_raidforged"].EquipmentSetId);
        Assert.Null(blueprints["blueprint_gravebound"].EquipmentSetId);
    }

    [Fact]
    public void ProviderLoadsAndResolvesBlueprintEquipmentSetMembership()
    {
        var temporaryRoot = CreateTemporaryDataRoot(includeEquipmentSet: true);

        try
        {
            var provider = CreateProvider(temporaryRoot);

            var equipmentSet = Assert.Single(provider.GetEquipmentSets());
            Assert.Equal("set.test", equipmentSet.Id);
            Assert.Equal("Test Set", equipmentSet.Name);
            Assert.Equal("set.test", provider.GetBlueprint("blueprint_fury")?.EquipmentSetId);
            Assert.Same(equipmentSet, provider.GetEquipmentSet("SET.TEST"));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void ProviderRejectsBlueprintWithMissingEquipmentSet()
    {
        var temporaryRoot = CreateTemporaryDataRoot(includeEquipmentSet: false);

        try
        {
            var provider = CreateProvider(temporaryRoot);

            var exception = Assert.Throws<InvalidOperationException>(provider.GetBlueprints);

            Assert.Contains("references missing equipment set 'set.test'", exception.Message);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void CraftingFamiliesContainOnlyTheRequestedRecipes()
    {
        var recipes = CreateProvider().GetRecipes();
        var expectedByFamily = new Dictionary<CraftType, string[]>
        {
            [CraftType.JewelryCrafting] = ["Amulet", "Relic", "Ring"],
            [CraftType.ArmorForging] =
            [
                "Cloth Cowl",
                "Cloth Pants",
                "Cloth Robe",
                "Heavy Breastplate",
                "Heavy Helm",
                "Heavy Legplates",
                "Light Hood",
                "Light Legwraps",
                "Light Vest",
                "Medium Greaves",
                "Medium Helm",
                "Medium Mail"
            ],
            [CraftType.WeaponSmithing] =
            [
                "Battle Axe",
                "Crossbow",
                "Dagger",
                "Gauntlets",
                "Greatsword",
                "Grimoire",
                "Hand Axe",
                "Longbow",
                "Mace",
                "Maul",
                "Shortsword",
                "Spear",
                "Spiritward",
                "Staff",
                "Towershield",
                "Wand"
            ]
        };

        Assert.Equal(expectedByFamily.Keys.Order(), recipes.Select(recipe => recipe.Category).Distinct().Order());
        foreach (var (family, expectedNames) in expectedByFamily)
        {
            Assert.Equal(
                expectedNames.Order(StringComparer.Ordinal),
                recipes
                    .Where(recipe => recipe.Category == family)
                    .Select(recipe => recipe.Name)
                    .Order(StringComparer.Ordinal));
        }
    }

    [Fact]
    public void EveryRecipeDefinesItsOutputStatsBehaviorAndTempering()
    {
        Assert.All(CreateProvider().GetRecipes(), recipe =>
        {
            Assert.StartsWith("recipe.", recipe.Id);
            Assert.NotEmpty(recipe.OutputItemId);
            Assert.NotEmpty(recipe.InitialStatProfile);
            Assert.NotEmpty(recipe.TemperingProfile.Stats);
            if (recipe.Tags.Contains("Weapon"))
            {
                Assert.NotEmpty(recipe.Behavior.Handedness);
                Assert.NotEmpty(recipe.Behavior.AttackCategory);
            }
        });
    }

    [Fact]
    public void EveryWeaponUsesStandardBasicAttackTimingAndDamage()
    {
        var weapons = CreateProvider()
            .GetRecipes()
            .Where(recipe => recipe.Tags.Contains("Weapon"))
            .ToList();

        Assert.NotEmpty(weapons);
        Assert.All(weapons, weapon =>
        {
            Assert.Equal(1d, weapon.Behavior.BasicAttackIntervalMultiplier);
            Assert.Equal(1d, weapon.Behavior.BasicAttackDamageMultiplier);
        });
    }

    [Fact]
    public void EveryWeaponUsesTheApprovedSeventyThirtyIdentityProfile()
    {
        var expectedSecondaryAttributes = new Dictionary<string, AttributeType>
        {
            ["recipe.weapon.one_handed.shortsword"] = AttributeType.CritChance,
            ["recipe.weapon.one_handed.dagger"] = AttributeType.AttackSpeed,
            ["recipe.weapon.one_handed.hand_axe"] = AttributeType.CritDamage,
            ["recipe.weapon.one_handed.mace"] = AttributeType.ArmorPenetration,
            ["recipe.weapon.one_handed.wand"] = AttributeType.MagicPenetration,
            ["recipe.weapon.two_handed.greatsword"] = AttributeType.CritChance,
            ["recipe.weapon.two_handed.battle_axe"] = AttributeType.CritDamage,
            ["recipe.weapon.two_handed.maul"] = AttributeType.ArmorPenetration,
            ["recipe.weapon.two_handed.spear"] = AttributeType.AttackSpeed,
            ["recipe.weapon.two_handed.staff"] = AttributeType.MagicPenetration,
            ["recipe.weapon.two_handed.longbow"] = AttributeType.CritChance,
            ["recipe.weapon.two_handed.crossbow"] = AttributeType.ArmorPenetration,
            ["recipe.weapon.two_handed.gauntlets"] = AttributeType.AttackSpeed
        };
        var weapons = CreateProvider()
            .GetRecipes()
            .Where(recipe => recipe.Tags.Contains("Weapon"))
            .ToList();

        Assert.Equal(expectedSecondaryAttributes.Count, weapons.Count);
        Assert.Equal(5, expectedSecondaryAttributes.Values.Distinct().Count());
        Assert.All(weapons, weapon =>
        {
            var secondaryAttribute = expectedSecondaryAttributes[weapon.Id];
            Assert.Equal(2, weapon.InitialStatProfile.Count);
            Assert.Equal(0.7d, weapon.InitialStatProfile[AttributeType.Power]);
            Assert.Equal(0.3d, weapon.InitialStatProfile[secondaryAttribute]);

            Assert.Collection(
                weapon.TemperingProfile.Stats,
                power =>
                {
                    Assert.Equal(AttributeType.Power, power.Stat);
                    Assert.Equal(70, power.Weight);
                    Assert.Equal(0.7d, power.MaxBudgetShare);
                },
                secondary =>
                {
                    Assert.Equal(secondaryAttribute, secondary.Stat);
                    Assert.Equal(30, secondary.Weight);
                    Assert.Equal(0.3d, secondary.MaxBudgetShare);
                });
        });
    }

    [Fact]
    public void JewelryRecipesUseTheirApprovedSingleStatIdentityProfiles()
    {
        var expectedByRecipe = new Dictionary<string, AttributeType>
        {
            ["recipe.jewelry.ring.band"] = AttributeType.Power,
            ["recipe.jewelry.necklace.amulet"] = AttributeType.MaxHealth,
            ["recipe.jewelry.relic.vial"] = AttributeType.HealthRegeneration
        };
        var recipes = CreateProvider().GetRecipes();

        foreach (var (recipeId, attribute) in expectedByRecipe)
        {
            var recipe = recipes.Single(candidate => candidate.Id == recipeId);
            Assert.Equal(
                new Dictionary<AttributeType, double> { [attribute] = 1d },
                recipe.InitialStatProfile);
            Assert.Collection(
                recipe.TemperingProfile.Stats,
                tempering =>
                {
                    Assert.Equal(attribute, tempering.Stat);
                    Assert.Equal(100, tempering.Weight);
                    Assert.Equal(1d, tempering.MaxBudgetShare);
                    Assert.True(tempering.CanIntroduce);
                    Assert.True(tempering.CanIncrease);
                });
        }
    }

    [Fact]
    public void GrimoireUsesTheApprovedPowerAndCooldownIdentityProfile()
    {
        var recipe = CreateProvider()
            .GetRecipes()
            .Single(candidate => candidate.Id == "recipe.offhand.grimoire");

        Assert.Equal(
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.7d,
                [AttributeType.Cooldown] = 0.3d
            },
            recipe.InitialStatProfile);
        Assert.Collection(
            recipe.TemperingProfile.Stats,
            power =>
            {
                Assert.Equal(AttributeType.Power, power.Stat);
                Assert.Equal(70, power.Weight);
                Assert.Equal(0.7d, power.MaxBudgetShare);
            },
            cooldown =>
            {
                Assert.Equal(AttributeType.Cooldown, cooldown.Stat);
                Assert.Equal(30, cooldown.Weight);
                Assert.Equal(0.3d, cooldown.MaxBudgetShare);
            });
    }

    [Fact]
    public void EveryArmorSlotUsesTheApprovedFamilyIdentityProfile()
    {
        var expectedByFamily = new Dictionary<string, Dictionary<AttributeType, double>>
        {
            ["HeavyArmor"] = new()
            {
                [AttributeType.Armor] = 0.35d,
                [AttributeType.MaxHealth] = 0.35d,
                [AttributeType.Resistance] = 0.30d
            },
            ["MediumArmor"] = new()
            {
                [AttributeType.Armor] = 0.25d,
                [AttributeType.MaxHealth] = 0.25d,
                [AttributeType.CritChance] = 0.25d,
                [AttributeType.CritDamage] = 0.25d
            },
            ["LightArmor"] = new()
            {
                [AttributeType.MaxHealth] = 0.25d,
                [AttributeType.HealthRegeneration] = 0.25d,
                [AttributeType.DodgeChance] = 0.25d,
                [AttributeType.AttackSpeed] = 0.25d
            },
            ["ClothArmor"] = new()
            {
                [AttributeType.Resistance] = 0.25d,
                [AttributeType.HealthRegeneration] = 0.25d,
                [AttributeType.HealingPowerPercent] = 0.25d,
                [AttributeType.Cooldown] = 0.25d
            }
        };
        var recipes = CreateProvider().GetRecipes();

        foreach (var (familyTag, expectedProfile) in expectedByFamily)
        {
            var familyRecipes = recipes.Where(recipe => recipe.Tags.Contains(familyTag)).ToList();
            Assert.Equal(3, familyRecipes.Count);

            Assert.All(familyRecipes, recipe =>
            {
                Assert.Equal(expectedProfile, recipe.InitialStatProfile);
                Assert.Equal(1d, recipe.InitialStatProfile.Values.Sum(), precision: 4);

                var temperingByAttribute = recipe.TemperingProfile.Stats
                    .ToDictionary(stat => stat.Stat);
                Assert.Equal(expectedProfile.Keys.Order(), temperingByAttribute.Keys.Order());
                foreach (var (attribute, budgetShare) in expectedProfile)
                {
                    var tempering = temperingByAttribute[attribute];
                    Assert.Equal((int)(budgetShare * 100), tempering.Weight);
                    Assert.Equal(budgetShare, tempering.MaxBudgetShare);
                    Assert.True(tempering.CanIntroduce);
                    Assert.True(tempering.CanIncrease);
                }
            });
        }
    }

    [Fact]
    public void ReviewedBlueprintProfilesRetainTheirBonusBudgetsAndWeights()
    {
        var provider = CreateProvider();
        var execution = provider.GetBlueprint("blueprint_execution")!;
        var aegis = provider.GetBlueprint("blueprint_aegis")!;

        Assert.Equal(0.2d, execution.BonusStatBudgetMultiplier);
        Assert.Equal(0.2d, aegis.BonusStatBudgetMultiplier);
        Assert.Equal(
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.35d,
                [AttributeType.ArmorPenetration] = 0.4d,
                [AttributeType.CritDamage] = 0.25d
            },
            execution.BonusStatProfile);
        Assert.Contains("Armor Penetration", execution.Description);
        Assert.Contains("ArmorPenetration", execution.Tags);
        Assert.DoesNotContain(AttributeType.CritChance, execution.BonusStatProfile.Keys);
        Assert.Equal(
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.25d,
                [AttributeType.Resistance] = 0.25d,
                [AttributeType.MaxHealth] = 0.35d,
                [AttributeType.DamageReduction] = 0.15d
            },
            aegis.BonusStatProfile);
    }

    [Fact]
    public void BlueprintItemMetadataIncludesCraftedAttributeContribution()
    {
        var provider = CreateProvider();
        var metadata = new BlueprintItemMetadataResolver(provider).Resolve(
            new ItemBase { Id = "blueprint_fury" },
            null!,
            null,
            null!);

        Assert.NotNull(metadata);
        Assert.Equal(
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.45d,
                [AttributeType.CritChance] = 0.3d,
                [AttributeType.CritDamage] = 0.25d
            },
            metadata.BonusStatProfile);
    }

    [Fact]
    public void Defensive_offhand_shields_share_health_and_block_with_distinct_typed_defense()
    {
        var recipes = CreateProvider().GetRecipes();
        var expectedDefenseByRecipe = new Dictionary<string, AttributeType>
        {
            ["recipe.offhand.towershield"] = AttributeType.Armor,
            ["recipe.offhand.spiritward"] = AttributeType.Resistance
        };

        foreach (var (recipeId, defense) in expectedDefenseByRecipe)
        {
            var recipe = recipes.Single(x => x.Id == recipeId);
            Assert.Contains("Shield", recipe.Tags);
            Assert.Contains("Block", recipe.Tags);
            Assert.Equal(
                new Dictionary<AttributeType, double>
                {
                    [AttributeType.MaxHealth] = 0.35d,
                    [AttributeType.BlockChance] = 0.35d,
                    [defense] = 0.30d
                },
                recipe.InitialStatProfile);
            Assert.Collection(
                recipe.TemperingProfile.Stats,
                maxHealth =>
                {
                    Assert.Equal(AttributeType.MaxHealth, maxHealth.Stat);
                    Assert.Equal(35, maxHealth.Weight);
                    Assert.Equal(0.35d, maxHealth.MaxBudgetShare);
                },
                block =>
                {
                    Assert.Equal(AttributeType.BlockChance, block.Stat);
                    Assert.Equal(35, block.Weight);
                    Assert.Equal(0.35d, block.MaxBudgetShare);
                },
                typedDefense =>
                {
                    Assert.Equal(defense, typedDefense.Stat);
                    Assert.Equal(30, typedDefense.Weight);
                    Assert.Equal(0.30d, typedDefense.MaxBudgetShare);
                });
        }
    }

    [Fact]
    public void Primal_blueprint_uses_the_stats_inherited_by_summons()
    {
        var provider = CreateProvider();
        var primal = provider.GetBlueprint("blueprint_primal")!;

        Assert.Equal(
            new Dictionary<AttributeType, double>
            {
                [AttributeType.Power] = 0.5d,
                [AttributeType.MaxHealth] = 0.3d,
                [AttributeType.CrowdControlResistance] = 0.2d
            },
            primal.BonusStatProfile);
        Assert.Equal(1d, primal.BonusStatProfile.Values.Sum(), precision: 4);
    }

    [Fact]
    public void BlueprintsComposeAcrossCompatibleRecipesWithoutAuthoredCombinations()
    {
        var provider = CreateProvider();
        var venom = provider.GetBlueprint("blueprint_venom")!;
        var weapons = provider.GetRecipes().Where(recipe => recipe.Tags.Contains("Weapon")).ToList();

        Assert.NotEmpty(weapons);
        Assert.All(weapons, recipe =>
        {
            Assert.True(EquipmentCraftingDesignComposer.IsCompatible(recipe, venom));
            var design = EquipmentCraftingDesignComposer.Compose(recipe, venom);
            Assert.Contains("Venom-Touched", design.Name);
            Assert.NotEmpty(design.InitialStatProfile);
            Assert.Equal(recipe.InitialStatProfile, design.InitialStatProfile);
            Assert.Equal(venom.BonusStatProfile, design.BlueprintBonusStatProfile);
            Assert.NotEmpty(design.TemperingProfile.Stats);
        });
    }

    [Fact]
    public void Aegis_cloth_cowl_preserves_every_base_roll_and_adds_bonus_power()
    {
        var provider = CreateProvider();
        var recipe = provider.GetRecipes().Single(x => x.Id == "recipe.armor.head.cloth_cowl");
        var aegis = provider.GetBlueprint("blueprint_aegis")!;
        var equipment = new EquipmentBase
        {
            Id = recipe.OutputItemId,
            Name = recipe.Name,
            EquipmentType = recipe.OutputItemType
        };
        var service = new ItemStatRollService();
        var baseStats = service.RollBaseStats(
            equipment,
            EquipmentCraftingDesignComposer.Compose(recipe, null),
            1,
            ItemQuality.Standard,
            new FixedRandom(0.5d));
        var aegisStats = service.RollBaseStats(
            equipment,
            EquipmentCraftingDesignComposer.Compose(recipe, aegis),
            1,
            ItemQuality.Standard,
            new FixedRandom(0.5d));
        var baseByAttribute = baseStats.ToDictionary(x => x.AttributeType, x => x.Amount);
        var aegisByAttribute = aegisStats.ToDictionary(x => x.AttributeType, x => x.Amount);

        Assert.All(baseByAttribute, stat =>
            Assert.True(aegisByAttribute[stat.Key] >= stat.Value));
        Assert.Equal(
            baseByAttribute[AttributeType.HealingPowerPercent],
            aegisByAttribute[AttributeType.HealingPowerPercent]);
        Assert.True(
            EquipmentBudgetEvaluator.Evaluate(aegisStats, 1) >
            EquipmentBudgetEvaluator.Evaluate(baseStats, 1));
        Assert.True(aegisByAttribute[AttributeType.Armor] > 0);
        Assert.True(aegisByAttribute[AttributeType.DamageReduction] > 0);
    }

    [Fact]
    public void ExactBlueprintCanRemainNarrow()
    {
        var provider = CreateProvider();
        var hivefang = provider.GetBlueprint("blueprint_hive")!;
        var compatible = provider.GetRecipes()
            .Where(recipe => EquipmentCraftingDesignComposer.IsCompatible(recipe, hivefang))
            .ToList();

        var dagger = Assert.Single(compatible);
        Assert.Equal("recipe.weapon.one_handed.dagger", dagger.Id);
    }

    [Fact]
    public void EveryRecipeHasSeveralBlueprintChoicesWithoutAuthoredCombinationContent()
    {
        var provider = CreateProvider();

        Assert.All(provider.GetRecipes(), recipe =>
            Assert.True(
                provider.GetBlueprints().Count(blueprint =>
                    EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint)) >= 5,
                $"{recipe.Name} should have at least five reusable Blueprint choices."));
    }

    [Fact]
    public void LegacyRecipeVariantCatalogIsNoLongerRuntimeContent()
    {
        Assert.Throws<FileNotFoundException>(() => ReadArray("crafting/recipe-variants.json"));
        Assert.Equal(31, ReadArray("crafting/base-recipes.json").Count);
    }

    internal static JsonArray ReadArray(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            foreach (var root in new[]
            {
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data")
            })
            {
                var candidate = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                    return JsonNode.Parse(File.ReadAllText(candidate))!.AsArray();
            }

            current = current.Parent;
        }

        throw new FileNotFoundException(relativePath);
    }

    private static JsonCraftingDefinitionProvider CreateProvider()
        => CreateProvider(FindDataRoot());

    private static JsonCraftingDefinitionProvider CreateProvider(string dataRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Content:Root"] = "." })
            .Build();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return new JsonCraftingDefinitionProvider(configuration, dataRoot, options);
    }

    private static string CreateTemporaryDataRoot(bool includeEquipmentSet)
    {
        var sourceRoot = FindDataRoot();
        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"legends-legacy-equipment-set-tests-{Guid.NewGuid():N}");
        var craftingRoot = Path.Combine(temporaryRoot, "crafting");
        var itemsRoot = Path.Combine(temporaryRoot, "items");
        Directory.CreateDirectory(craftingRoot);
        Directory.CreateDirectory(itemsRoot);

        foreach (var fileName in new[] { "materials.json", "base-recipes.json" })
        {
            File.Copy(
                Path.Combine(sourceRoot, "crafting", fileName),
                Path.Combine(craftingRoot, fileName));
        }

        File.Copy(
            Path.Combine(sourceRoot, "items", "items.json"),
            Path.Combine(itemsRoot, "items.json"));

        var blueprints = JsonNode.Parse(File.ReadAllText(
            Path.Combine(sourceRoot, "crafting", "blueprints.json")))!.AsArray();
        foreach (var blueprint in blueprints)
            blueprint!["equipmentSetId"] = null;
        blueprints[0]!["equipmentSetId"] = "set.test";
        File.WriteAllText(
            Path.Combine(craftingRoot, "blueprints.json"),
            blueprints.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        var equipmentSets = includeEquipmentSet
            ? new JsonArray(new JsonObject
            {
                ["id"] = "set.test",
                ["name"] = "Test Set",
                ["description"] = "Test equipment-set metadata."
            })
            : [];
        File.WriteAllText(
            Path.Combine(craftingRoot, "equipment-sets.json"),
            equipmentSets.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return temporaryRoot;
    }

    private static string FindDataRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data"),
                Path.Combine(current.FullName, "src", "API", "API.LL", "Data")
            })
            {
                if (Directory.Exists(candidate))
                    return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Crafting data root not found.");
    }

    private sealed class FixedRandom(double nextDouble) : Random
    {
        public override double NextDouble() => nextDouble;
    }
}
