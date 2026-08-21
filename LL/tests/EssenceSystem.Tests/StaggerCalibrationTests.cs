using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat.Engine;

namespace EssenceSystem.Tests;

public sealed class StaggerCalibrationTests
{
    [Fact]
    public void Catalog_discovers_authored_tower_raid_and_raid_plus_stagger_profiles()
    {
        var catalog = CreateCatalog();

        Assert.Equal(1, catalog.Version);
        Assert.Equal(1_800, catalog.EvaluationDurationTicks);
        Assert.Equal(19, catalog.Encounters.Count);
        Assert.Equal(10, catalog.Encounters.Count(encounter =>
            encounter.ContentType == StaggerCalibrationContentType.Tower));
        Assert.Equal(5, catalog.Encounters.Count(encounter =>
            encounter.ContentType == StaggerCalibrationContentType.Raid));
        Assert.Equal(4, catalog.Encounters.Count(encounter =>
            encounter.ContentType == StaggerCalibrationContentType.RaidPlus));
        Assert.Equal(3, catalog.Cohorts.Count);
        Assert.Equal(3, catalog.Profiles.Count);
        Assert.Single(catalog.Cohorts, cohort => cohort.IsAssessmentCohort);

        var floorOne = catalog.Encounters.Single(encounter =>
            encounter.Id == "tower.floor-01");
        Assert.Equal(125, floorOne.Definition.BaseThreshold);
        Assert.Equal(5, floorOne.Definition.ReferenceParticipantCount);

        var floorFive = catalog.Encounters.Single(encounter =>
            encounter.Id == "tower.floor-05");
        Assert.Equal(250, floorFive.Definition.BaseThreshold);
        Assert.Equal(10, floorFive.Definition.ReferenceParticipantCount);

        var regular = catalog.Encounters.Single(encounter =>
            encounter.Id == "raid-boss.hives-abyss.tier-1");
        var plusSix = catalog.Encounters.Single(encounter =>
            encounter.Id == "raid-boss.hives-abyss.plus-6");
        Assert.True(plusSix.Definition.BaseThreshold > regular.Definition.BaseThreshold);
    }

    [Fact]
    public void Runner_is_deterministic_and_reports_party_size_and_control_profiles()
    {
        var catalog = CreateCatalog();
        var runner = new StaggerCalibrationRunner();

        var first = runner.Run(catalog);
        var second = runner.Run(catalog);

        Assert.Equal(171, first.Results.Count);
        Assert.Equal(2_736, first.Results.Sum(result => result.SampleCount));
        Assert.Equal(
            JsonSerializer.Serialize(first),
            JsonSerializer.Serialize(second));
        Assert.All(first.Results, result =>
        {
            Assert.InRange(result.AverageBreaks, 0, 4);
            Assert.InRange(result.AverageContributionEfficiencyPercent, 0, 100);
            Assert.InRange(result.AverageStaggerUptimePercent, 0, 100);
            Assert.InRange(result.BreakCapRate, 0, 1);
            Assert.True(result.InitialThreshold > 0);
            Assert.True(result.ContributorCount > 0);
        });

        var floorFive = first.Results.Where(result =>
                result.EncounterId == "tower.floor-05"
                && result.ProfileId == "balanced")
            .OrderBy(result => result.ParticipantCount)
            .ToList();
        Assert.Equal(3, floorFive.Count);
        Assert.True(floorFive[0].InitialThreshold < floorFive[1].InitialThreshold);
        Assert.True(floorFive[1].InitialThreshold < floorFive[2].InitialThreshold);
    }

    [Fact]
    public void Focused_report_is_deterministic_and_explains_isolation_assumptions()
    {
        var catalog = CreateCatalog();
        var options = new StaggerCalibrationRunOptions(
            EncounterIds: ["tower.floor-05"],
            CohortIds: ["reference"],
            ProfileIds: ["balanced"],
            SampleCount: 32);
        var report = new StaggerCalibrationRunner().Run(catalog, options);
        var artifact = StaggerCalibrationReportRenderer.CreateArtifact(catalog, report);
        var markdown = StaggerCalibrationReportRenderer.RenderMarkdown(artifact);

        var result = Assert.Single(report.Results);
        Assert.Equal(32, result.SampleCount);
        Assert.Equal("reference", result.CohortId);
        Assert.Equal("balanced", result.ProfileId);
        Assert.Contains("mechanic-isolation", markdown);
        Assert.Contains("Party-size sensitivity", markdown);
        Assert.Contains("does not scale with Essence ascension", markdown);
        Assert.Equal(
            StaggerCalibrationReportRenderer.RenderJson(artifact),
            StaggerCalibrationReportRenderer.RenderJson(
                StaggerCalibrationReportRenderer.CreateArtifact(catalog, report)));
    }

    private static StaggerCalibrationCatalog CreateCatalog()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return new StaggerCalibrationCatalogFactory(
                configuration,
                TestContentPaths.FindApiRoot(),
                options)
            .CreateCatalog();
    }
}
