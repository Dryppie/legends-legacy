using Domain.Models.Combat.Abilities;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EssenceSystem.Tests;

public sealed class AbilityBalanceTuningTests
{
    [Fact]
    public void Balance_passes_use_the_expected_tuned_values()
    {
        var catalog = new JsonAbilityCatalogProvider(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build(),
            FindApiContentRoot(),
            CreateJsonOptions()).GetCatalog();

        Assert.Equal(
            120,
            catalog.AbilitiesById["ability.creature.nightshade_blossom.withering_petals"]
                .CooldownTicks);
        Assert.Equal(
            15f,
            Effect(
                catalog,
                "ability.creature.green_slime.acid_splash",
                "effect.creature.green_slime.acid_splash.poison").BaseValue);
        Assert.Equal(
            200,
            catalog.AbilitiesById["ability.creature.green_slime.acid_splash"]
                .CooldownTicks);
        Assert.Equal(
            7f,
            Effect(
                catalog,
                "ability.creature.bog_mite.infestation",
                "effect.creature.bog_mite.infestation.wound").BaseValue);
        Assert.Equal(
            1.25f,
            Effect(
                catalog,
                "ability.creature.bog_mite.infestation",
                "effect.creature.bog_mite.infestation.damage").ScalingCoefficient);
        Assert.Equal(
            2f,
            Effect(
                catalog,
                "ability.creature.ravenous_ghoul.draining_claws",
                "effect.creature.ravenous_ghoul.draining_claws.damage").ScalingCoefficient);

        Assert.Equal(
            5f,
            Effect(
                catalog,
                "ability.creature.transparent_slime.transparent_engulf",
                "effect.creature.transparent_slime.transparent_engulf.guard").BaseValue);
        Assert.Contains(
            catalog.AbilitiesById["ability.creature.transparent_slime.reconstitute"]
                .Triggers.SelectMany(trigger => trigger.Conditions),
            condition => condition.Type == AbilityConditionType.HealthBelowPercent
                && condition.Value == 80);
        Assert.Equal(
            -100,
            catalog.AbilitiesById["ability.creature.moss_lizard.moss_camouflage"].ThreatValue);

        var faesCorrosion = Assert.Single(
            catalog.AbilitiesById["ability.creature.enchanted_fairy.faes_corrosion"].Effects);
        Assert.Equal("effect.creature.enchanted_fairy.faes_corrosion.corrosion", faesCorrosion.Id);
        Assert.Equal(6f, faesCorrosion.BaseValue);
        Assert.Equal(
            16f,
            Effect(
                catalog,
                "ability.creature.venomous_spiderling.venom_web",
                "effect.creature.venomous_spiderling.venom_web.poison").BaseValue);
        Assert.Equal(
            3f,
            Effect(
                catalog,
                "ability.creature.blood_harpy.rupturing_talons",
                "effect.creature.blood_harpy.rupturing_talons.bleed").BaseValue);

        Assert.Equal(
            2.5f,
            Effect(
                catalog,
                "ability.creature.bark_golem.timber_slam",
                "effect.creature.bark_golem.timber_slam.damage").ScalingCoefficient);
        Assert.Equal(
            380f,
            Effect(
                catalog,
                "ability.creature.elder_treant.ancient_sap",
                "effect.creature.elder_treant.ancient_sap.doom").BaseValue);
        Assert.Equal(
            0.7f,
            Effect(
                catalog,
                "ability.creature.elder_treant.thornstorm",
                "effect.creature.elder_treant.thornstorm.magical_damage").ScalingCoefficient);
        Assert.Equal(
            0.7f,
            Effect(
                catalog,
                "ability.creature.elder_treant.thornstorm",
                "effect.creature.elder_treant.thornstorm.physical_damage").ScalingCoefficient);
    }

    private static AbilityEffectSpec Effect(
        AbilityCatalog catalog,
        string abilityId,
        string effectId) =>
        catalog.AbilitiesById[abilityId].Effects.Single(effect => effect.Id == effectId);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "API", "API.LL");
            if (Directory.Exists(Path.Combine(candidate, "Data")))
                return candidate;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL content root.");
    }
}
