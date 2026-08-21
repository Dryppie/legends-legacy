using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Services.LL.Regions;
using Domain.Models.Attributes;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using Services.LL.Regions;

namespace EssenceSystem.Tests;

public sealed class EncounterCalibrationTests
{
    [Fact]
    public void Catalog_resolves_representative_authored_content_sources()
    {
        var context = CreateContext();

        var catalog = context.EncounterFactory.CreateCatalog();

        Assert.Equal(7, catalog.Version);
        Assert.Equal(18, catalog.Encounters.Count);
        Assert.Equal(4, catalog.Encounters.Select(encounter => encounter.ContentType).Distinct().Count());
        Assert.Equal(3, catalog.Encounters.Count(encounter => encounter.ContentType == EncounterCalibrationContentType.Idle));
        Assert.Equal(4, catalog.Encounters.Count(encounter => encounter.ContentType == EncounterCalibrationContentType.Dungeon));
        Assert.Equal(4, catalog.Encounters.Count(encounter => encounter.ContentType == EncounterCalibrationContentType.Tower));
        Assert.Equal(7, catalog.Encounters.Count(encounter => encounter.ContentType == EncounterCalibrationContentType.Raid));
        Assert.All(catalog.Encounters, encounter =>
        {
            Assert.NotEmpty(encounter.Hostiles);
            Assert.True(encounter.MaxTicks > 0);
            Assert.True(encounter.PlayerCount > 0);
            Assert.All(encounter.Hostiles, hostile =>
            {
                Assert.StartsWith("monster.", hostile.MonsterId);
                Assert.True(hostile.Attributes[AttributeType.MaxHealth] > 0);
                Assert.True(hostile.Attributes[AttributeType.Power] > 0);
                Assert.NotEmpty(hostile.AbilityIds);
            });
        });

        var idleTriple = catalog.Encounters.Single(encounter => encounter.Id == "idle.duskmire.triple");
        var dungeonBoss = catalog.Encounters.Single(encounter => encounter.Id == "dungeon.goblin-mines.boss");
        var tower = catalog.Encounters.Single(encounter => encounter.Id == "tower.floor-01.guardian");
        var staggerTower = catalog.Encounters.Single(encounter => encounter.Id == "tower.floor-05.warden");
        var floorSeven = catalog.Encounters.Single(encounter => encounter.Id == "tower.floor-07.guardian");
        var raid = catalog.Encounters.Single(encounter =>
            encounter.Id == "raid.hives-abyss.tier-1.final-assault");
        Assert.Equal(3, idleTriple.Hostiles.Count);
        Assert.Equal(4, dungeonBoss.Hostiles.Count);
        Assert.Equal(5, tower.PlayerCount);
        Assert.Single(tower.Hostiles);
        Assert.Equal(5, tower.PartyCompositions.Count);
        Assert.All(tower.PartyCompositions, composition =>
            Assert.Equal(5, composition.MemberBuildFamilyIds.Count));
        Assert.NotNull(staggerTower.Hostiles.Single().StaggerDefinition);
        Assert.Equal(10, staggerTower.Hostiles.Single().StaggerParticipantCount);
        Assert.Equal(5, floorSeven.PlayerCount);
        Assert.Equal(7, floorSeven.ProgressionPosition);
        Assert.Equal(3, raid.PlayerCount);
        Assert.Single(raid.Hostiles);
        Assert.NotNull(raid.Hostiles.Single().StaggerDefinition);
        Assert.Equal(3, raid.Hostiles.Single().StaggerParticipantCount);
        Assert.Equal(4_500, raid.OvertimeStartsAtTick);
        Assert.True(raid.Hostiles.Single().AbilityCooldownDelayFraction > 0);
        Assert.All(
            catalog.Encounters.Where(encounter =>
                encounter.ContentType is EncounterCalibrationContentType.Idle
                    or EncounterCalibrationContentType.Dungeon),
            encounter => Assert.Equal(
                ["offensive", "control", "summon"],
                encounter.Band.AssessedBuildFamilyIds));
        Assert.All(
            catalog.Encounters.Where(encounter =>
                encounter.ContentType is EncounterCalibrationContentType.Tower
                    or EncounterCalibrationContentType.Raid),
            encounter => Assert.Null(encounter.Band.AssessedBuildFamilyIds));
        Assert.Equal("balanced", catalog.SupportAssessment.BaselineCompositionId);
        Assert.Equal("sustain-heavy", catalog.SupportAssessment.SupportCompositionId);
        Assert.Equal(0.20, catalog.CompositionAssessment.AlternativeMinimumWinRate);
        Assert.Equal(0.35, catalog.CompositionAssessment.CounteredMaximumWinRate);
        Assert.Equal(
            CompositionExpectation.Expected,
            staggerTower.CompositionExpectations["control-oriented"]);
        Assert.Equal(
            CompositionExpectation.Countered,
            staggerTower.CompositionExpectations["sustain-heavy"]);
        Assert.Equal(
            CompositionExpectation.Challenge,
            floorSeven.CompositionExpectations["balanced"]);
        var threePlayerRosters = catalog.PartyCompositions.ToDictionary(
            composition => composition.Id,
            composition => composition.RosterOverrides!.Single(roster => roster.PlayerCount == 3)
                .MemberBuildFamilyIds);
        Assert.Equal(["offensive", "offensive", "sustain"], threePlayerRosters["balanced"]);
        Assert.Equal(["offensive", "offensive", "offensive"], threePlayerRosters["offense-heavy"]);
        Assert.Equal(["offensive", "sustain", "sustain"], threePlayerRosters["sustain-heavy"]);
        Assert.Equal(["offensive", "control", "control"], threePlayerRosters["control-oriented"]);
        Assert.Equal(["offensive", "summon", "summon"], threePlayerRosters["summon-oriented"]);
    }

    [Fact]
    public void Content_pressure_layers_are_applied_after_the_shared_creature_curve()
    {
        var catalog = CreateContext().EncounterFactory.CreateCatalog();
        var idleGoblin = catalog.Encounters.Single(encounter => encounter.Id == "idle.lumo-ruins.single")
            .Hostiles.Single();
        var dungeonGoblin = catalog.Encounters.Single(encounter => encounter.Id == "dungeon.goblin-mines.room")
            .Hostiles.Single(hostile => hostile.MonsterId == "monster.goblin");
        var towerGuardian = catalog.Encounters.Single(encounter => encounter.Id == "tower.floor-01.guardian")
            .Hostiles.Single();

        Assert.True(dungeonGoblin.Attributes[AttributeType.MaxHealth]
                    > idleGoblin.Attributes[AttributeType.MaxHealth]);
        Assert.True(dungeonGoblin.Attributes[AttributeType.Power]
                    > idleGoblin.Attributes[AttributeType.Power]);
        Assert.True(towerGuardian.Attributes[AttributeType.MaxHealth]
                    > idleGoblin.Attributes[AttributeType.MaxHealth]);
        Assert.True(towerGuardian.Attributes[AttributeType.Power]
                    > idleGoblin.Attributes[AttributeType.Power]);
    }

    [Fact]
    public void Runner_crosses_builds_and_essence_envelopes_with_real_encounters()
    {
        var context = CreateContext();
        var catalog = context.EncounterFactory.CreateCatalog();
        var players = context.PlayerFactory.CreateScenarios();

        var report = context.Runner.Run(catalog, players);

        Assert.Equal(996, report.Results.Count);
        Assert.All(report.Results, result =>
        {
            Assert.Equal(3, result.SampleCount);
            Assert.InRange(result.WinRate, 0, 1);
            Assert.InRange(result.WinRate, result.WinRateConfidenceLower95, result.WinRateConfidenceUpper95);
            Assert.InRange(result.TimeoutRate, 0, 1);
            Assert.InRange(result.TimeoutRate, result.TimeoutRateConfidenceLower95, result.TimeoutRateConfidenceUpper95);
            Assert.True(result.AverageDurationTicks > 0);
            Assert.True(result.AverageEnemyAbilityUses >= 0);
            Assert.True(result.AverageHealthRegenerated >= 0);
            Assert.True(result.AverageBarrierConsumed >= 0);
        });
        Assert.Contains(report.Results, result => result.AverageEnemyAbilityUses > 0);
        var expectedCohort = report.Results.Where(result =>
                result.GearEnvelopeId == catalog.AssessmentGearEnvelopeId
                && result.EssenceEnvelopeId == catalog.AssessmentEssenceEnvelopeId)
            .ToList();
        Assert.Equal(83, expectedCohort.Count);
        Assert.Equal(76, expectedCohort.Count(result => result.IncludedInRoleAssessment));
        Assert.Equal(7, expectedCohort.Count(result => !result.IncludedInRoleAssessment));
        Assert.All(
            report.Results.Where(result =>
                (result.ContentType == EncounterCalibrationContentType.Idle
                 || result.ContentType == EncounterCalibrationContentType.Dungeon)
                && result.BuildFamilyId == "sustain"),
            result => Assert.False(result.IncludedInRoleAssessment));
        var towerResults = report.Results.Where(result =>
                result.ContentType == EncounterCalibrationContentType.Tower)
            .ToList();
        Assert.Equal(4 * 3 * 5 * 4, towerResults.Count);
        Assert.All(towerResults, result => Assert.False(string.IsNullOrWhiteSpace(result.PartyCompositionId)));
        Assert.All(towerResults, result => Assert.True(result.IncludedInRoleAssessment));
        Assert.All(towerResults, result => Assert.NotEqual(
            CompositionExpectation.NotApplicable,
            result.CompositionExpectation));
        Assert.Equal(
            1,
            towerResults.Single(result =>
                result.EncounterId == "tower.floor-01.guardian"
                && result.GearEnvelopeId == "expected"
                && result.EssenceEnvelopeId == "expected"
                && result.PartyCompositionId == "balanced").SustainMemberCount);
        Assert.Equal(
            2,
            towerResults.Single(result =>
                result.EncounterId == "tower.floor-01.guardian"
                && result.GearEnvelopeId == "expected"
                && result.EssenceEnvelopeId == "expected"
                && result.PartyCompositionId == "sustain-heavy").SustainMemberCount);
        Assert.Equal(5, towerResults.Select(result => result.PartyCompositionId).Distinct().Count());
        var raidResults = report.Results.Where(result =>
                result.ContentType == EncounterCalibrationContentType.Raid)
            .ToList();
        Assert.Equal(7 * 3 * 5 * 4, raidResults.Count);
        Assert.All(raidResults, result =>
        {
            Assert.True(result.IncludedInRoleAssessment);
            Assert.True(result.StaggerEnabled);
            Assert.False(string.IsNullOrWhiteSpace(result.PartyCompositionId));
            Assert.InRange(result.AverageStaggerUptimePercent, 0, 100);
            Assert.InRange(result.AverageDamageDuringStaggerPercent, 0, 100);
            Assert.InRange(result.StaggerBreakCapRate, 0, 1);
        });
        Assert.Contains(raidResults, result =>
            result.EncounterId == "raid.hives-abyss.tier-1.final-assault"
            && result.GearEnvelopeId == "expected"
            && result.EssenceEnvelopeId == "expected"
            && result.PartyCompositionId == "control-oriented"
            && result.AverageStaggerContributed > 0
            && result.AverageStaggerBreaks > 0);
        Assert.Equal(
            1,
            raidResults.Single(result =>
                result.EncounterId == "raid.hives-abyss.tier-1.final-assault"
                && result.GearEnvelopeId == "expected"
                && result.EssenceEnvelopeId == "expected"
                && result.PartyCompositionId == "balanced").SustainMemberCount);
        Assert.Equal(
            2,
            raidResults.Single(result =>
                result.EncounterId == "raid.hives-abyss.tier-1.final-assault"
                && result.GearEnvelopeId == "expected"
                && result.EssenceEnvelopeId == "expected"
                && result.PartyCompositionId == "sustain-heavy").SustainMemberCount);
        var encounterIds = catalog.Encounters.Select(encounter => encounter.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(report.Exceptions, exception =>
            Assert.Contains(exception.EncounterId, encounterIds));
        var soloEncounterIds = catalog.Encounters.Where(encounter =>
                encounter.ContentType is EncounterCalibrationContentType.Idle
                    or EncounterCalibrationContentType.Dungeon)
            .Select(encounter => encounter.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(report.Exceptions, exception =>
            soloEncounterIds.Contains(exception.EncounterId)
            && (exception.BuildFamilyId == "sustain"
                || exception.Metric == "BuildWinRateSpread"));
        Assert.DoesNotContain(report.Exceptions, exception =>
            exception.EncounterId == "tower.floor-05.warden"
            && exception.BuildFamilyId == "sustain-heavy"
            && exception.Metric == "WinRate");
        Assert.Contains(report.Exceptions, exception =>
            exception.EncounterId == "tower.floor-05.warden"
            && exception.BuildFamilyId == "offense-heavy"
            && exception.Classification == "UnexpectedSuccess"
            && exception.Metric == "WinRate");
        Assert.Contains(report.Exceptions, exception =>
            exception.EncounterId == "tower.floor-07.guardian"
            && exception.BuildFamilyId == "balanced"
            && exception.Classification == "UnexpectedSuccess"
            && exception.Metric == "WinRate");
        Assert.DoesNotContain(report.Exceptions, exception =>
            exception.EncounterId is "tower.floor-05.warden" or "tower.floor-07.guardian"
            && exception.Metric == "BuildWinRateSpread");

        var artifact = EncounterCalibrationReportRenderer.CreateArtifact(report, catalog);
        var markdown = EncounterCalibrationReportRenderer.RenderMarkdown(artifact);
        Assert.Equal(7, artifact.SchemaVersion);
        Assert.Equal(996, artifact.Summary.ResultCount);
        Assert.Equal(2_988, artifact.Summary.SeededSampleCount);
        Assert.Equal(76, artifact.Summary.AssessedResultCount);
        Assert.Equal(4, artifact.Summary.Content.Count);
        Assert.NotNull(artifact.SupportComparisons);
        Assert.Equal(11 * 3 * 4, artifact.SupportComparisons.Count);
        Assert.Equal(
            11,
            artifact.SupportComparisons.Count(comparison =>
                comparison.GearEnvelopeId == "expected"
                && comparison.EssenceEnvelopeId == "expected"));
        Assert.All(
            artifact.SupportComparisons,
            comparison => Assert.False(string.IsNullOrWhiteSpace(comparison.Classification)));
        Assert.Contains(artifact.SupportComparisons, comparison =>
            comparison.EncounterId == "raid.hives-abyss.tier-1.final-assault"
            && comparison.GearEnvelopeId == "expected"
            && comparison.EssenceEnvelopeId == "expected"
            && comparison.BaselineSustainMembers == 1
            && comparison.SupportSustainMembers == 2
            && comparison.Classification != "NoAdditionalSupport");
        Assert.Contains(artifact.SupportComparisons, comparison =>
            comparison.EncounterId == "raid.sanguine-horror.tier-2.final-assault"
            && comparison.GearEnvelopeId == "expected"
            && comparison.EssenceEnvelopeId == "expected"
            && comparison.Classification == "UnnecessaryForCompletion");
        Assert.All(
            artifact.SupportComparisons.Where(comparison =>
                comparison.EncounterId is "tower.floor-05.warden" or "tower.floor-07.guardian"
                && comparison.GearEnvelopeId == "expected"
                && comparison.EssenceEnvelopeId == "expected"),
            comparison => Assert.Equal("CompletionRegressed", comparison.Classification));
        Assert.Contains("Expected-cohort encounter overview", markdown);
        Assert.Contains("Authored composition expectations", markdown);
        Assert.Contains("Failure diagnostics", markdown);
        Assert.Contains("Observational role diagnostics", markdown);
        Assert.Contains("Multiplayer support effectiveness", markdown);
        Assert.Contains("Stagger diagnostics", markdown);
        Assert.Contains("Review order", markdown);
        Assert.Equal(
            EncounterCalibrationReportRenderer.RenderJson(artifact),
            EncounterCalibrationReportRenderer.RenderJson(
                EncounterCalibrationReportRenderer.CreateArtifact(report, catalog)));

        var compared = EncounterCalibrationReportRenderer.CreateArtifact(report, catalog, artifact);
        Assert.NotNull(compared.Comparison);
        Assert.Empty(compared.Comparison.ResultChanges);
        Assert.Empty(compared.Comparison.IntroducedExceptions);
        Assert.Empty(compared.Comparison.ResolvedExceptions);
    }

    [Fact]
    public void Focused_encounter_report_is_deterministic()
    {
        var context = CreateContext();
        var fullCatalog = context.EncounterFactory.CreateCatalog();
        var catalog = fullCatalog with
        {
            Encounters =
            [
                fullCatalog.Encounters.Single(encounter =>
                    encounter.Id == "dungeon.goblin-mines.room")
            ]
        };
        var players = context.PlayerFactory.CreateScenarios()
            .Where(player => player.SnapshotAnchorId == "region-01-end"
                             && player.GearEnvelopeId == "expected"
                             && player.BuildFamilyId == "offensive")
            .ToList();

        var options = new EncounterCalibrationRunOptions(
            EssenceEnvelopeIds: ["expected"],
            SampleCount: 10);
        var first = context.Runner.Run(catalog, players, options);
        var second = context.Runner.Run(catalog, players, options);

        var result = Assert.Single(first.Results);
        Assert.Equal(10, result.SampleCount);
        Assert.Contains(
            "High-sample confidence",
            EncounterCalibrationReportRenderer.RenderMarkdown(
                EncounterCalibrationReportRenderer.CreateArtifact(first, catalog)));
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
    }

    private static TestContext CreateContext()
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
        var contentRoot = TestContentPaths.FindApiRoot();
        IRegionCreatureScalingProvider scaling = new RegionCreatureScalingProvider(
            configuration,
            contentRoot,
            options);
        var creatureAbilities = new JsonCreatureAbilityDefinitionProvider(
            configuration,
            contentRoot,
            options);
        var encounterFactory = new AuthoredEncounterCalibrationFactory(
            configuration,
            contentRoot,
            options,
            scaling,
            creatureAbilities);
        var snapshots = new PlayerProgressionSnapshotFactory(
            configuration,
            contentRoot,
            options);
        var slotUnlocks = new EssenceSlotUnlockService();
        var playerFactory = new EssenceCalibrationMatrixFactory(
            configuration,
            contentRoot,
            options,
            snapshots,
            slotUnlocks);
        var abilityCatalog = new JsonAbilityCatalogProvider(configuration, contentRoot, options);
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            contentRoot,
            options,
            new EssenceDefinitionValidator());
        var runner = new EncounterCalibrationRunner(abilityCatalog, essenceDefinitions);

        return new TestContext(encounterFactory, playerFactory, runner);
    }

    private sealed record TestContext(
        AuthoredEncounterCalibrationFactory EncounterFactory,
        EssenceCalibrationMatrixFactory PlayerFactory,
        EncounterCalibrationRunner Runner);
}
