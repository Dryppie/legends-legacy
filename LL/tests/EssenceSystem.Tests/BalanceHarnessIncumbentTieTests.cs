using System.Text.Json;
using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessIncumbentTieTests
{
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static TowerBossDiscoveryDefinition OptIn(TowerBossDiscoveryDefinition d, string reference = "anchor-0")
        => d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion,
            SelectionPrimaryReferenceId = reference } };

    private static PartyChoice[] Shortlist(TowerBossDiscoveryDefinition d)
    {
        var primary = d.Starts[0].Party;
        PartyChoice Challenger(string last) => TowerPartySelection.Choice("fixture", primary.Builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)new[] { "e00", "e01", "e02", last } : p.Value));
        return [Challenger("e10"), Challenger("e11"), d.Starts[1].Party, primary];
    }

    private static BossDiscoveryMeasurement[] Measurements(TowerBossDiscoveryDefinition d, PartyChoice[] shortlist,
        int first, int second, int other, int primary, double primaryHealth = 99)
    {
        var input = TowerBossImprovement.Inputs(d) with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection) };
        var counts = new[] { first, second, other, primary };
        return shortlist.Select((p, i) => I.Measure(input, p, counts[i], i == 3 ? primaryHealth : i + 1)).Reverse().ToArray();
    }

    [Theory]
    [InlineData(22, 18, 20, 22, 3, "retained")]
    [InlineData(22, 22, 22, 22, 3, "retained")]
    [InlineData(25, 18, 20, 22, 0, "Unique maximum")]
    [InlineData(21, 18, 20, 22, 3, "Unique maximum")]
    [InlineData(23, 23, 20, 22, 0, "below")]
    [InlineData(0, 0, 0, 0, 0, "zero")]
    public void Only_a_positive_top_tie_retains_the_designated_primary(int first, int second, int other, int primary,
        int expected, string reason)
    {
        var d = OptIn(I.Definition()); var shortlist = Shortlist(d); var rows = Measurements(d, shortlist, first, second, other, primary);
        var mechanics = F.Mechanics(TowerBossImprovement.Inputs(d));
        var selected = Assert.Single(TowerBossStudyPolicy.Select(d, mechanics, shortlist, rows));
        Assert.Equal(shortlist[expected].Id, selected.Party.Id); Assert.Contains(reason, selected.Reason);
        var freeze = TowerBossStudyPolicy.Freeze(d, shortlist, rows, [selected], 640);
        Assert.Equal(new BossIncumbentSelection("anchor-0", d.Starts[0].Party.Id, selected.Reason), freeze.IncumbentSelection);
        Assert.Equal(TowerBossStudyPolicy.IncumbentTieVersion, freeze.PolicyVersion);
        // The supplied identity is bound before selection, not inferred from the selected output.
        Assert.Equal(d.Starts[0].Party.Id, freeze.IncumbentSelection!.PartyId);
    }

    [Fact]
    public void Zero_win_health_ties_keep_discovery_order_and_missing_primary_cannot_fall_back()
    {
        var d = OptIn(I.Definition()); var shortlist = Shortlist(d);
        var rows = Measurements(d, shortlist, 0, 0, 0, 0, primaryHealth: 1);
        var mechanics = F.Mechanics(TowerBossImprovement.Inputs(d));
        Assert.Equal(shortlist[0].Id, TowerBossStudyPolicy.Select(d, mechanics, shortlist, rows).Single().Party.Id);
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(d, mechanics, shortlist.Take(3).ToArray(),
            rows.Where(r => r.Id != d.Starts[0].Party.Id).ToArray()));
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(d, mechanics,
            shortlist.Where(p => p.Id != d.Starts[1].Party.Id).ToArray(), rows.Where(r => r.Id != d.Starts[1].Party.Id).ToArray()));
        var input = TowerBossImprovement.Inputs(d);
        Assert.Throws<InvalidDataException>(() => TowerBossStudyPolicy.Select(input, mechanics, shortlist, rows,
            d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection), 1, TowerBossStudyPolicy.IncumbentTieVersion));
    }

    [Theory]
    [InlineData("missing")] [InlineData("unknown")] [InlineData("legacy")] [InlineData("duplicate")]
    [InlineData("independent")] [InlineData("standalone")] [InlineData("finalists")] [InlineData("nominees")]
    public void Invalid_designations_and_scopes_fail_before_allocation(string invalid)
    {
        var d = OptIn(I.Definition());
        d = invalid switch {
            "missing" => d with { Stages = d.Stages with { SelectionPrimaryReferenceId = null } },
            "unknown" => d with { Stages = d.Stages with { SelectionPrimaryReferenceId = "unknown" } },
            "legacy" => d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.ZeroWinVersion } },
            "duplicate" => d with { Starts = [d.Starts[0], d.Starts[0] with { Id = "duplicate" }] },
            "independent" => d with { Mode = TowerBossDiscovery.Independent, Starts = [] },
            "standalone" => d with { Generation = d.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.StandaloneVersion } },
            "finalists" => d with { Stages = d.Stages with { GeneratedFinalists = 2 } },
            _ => d with { Stages = d.Stages with { Shortlist = 5 } }
        };
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d));
        var template = BalanceHarnessPracticalAllocationTests.Template(d with { ExcludedCombatSeeds = [-987] });
        var q = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, "content", "definition", new string('a', 64),
            "registry", "output", new Dictionary<string, string>(), 300, 32 * 1048576,
            Allocation: new(77, "tie-fixture", 8, 32, 256));
        Assert.Throws<InvalidDataException>(() => TowerPracticalSearch.ValidateAllocationTemplate(q, template));
    }

    [Fact]
    public async Task Legacy_serialization_nomination_and_selected_output_are_preserved()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture entered combat.")).Activate();
        var old = I.Definition(); var d = OptIn(old); var input = TowerBossImprovement.Inputs(old); var mechanics = F.Mechanics(input);
        Task<BossGenerationResult> Discover(TowerBossDiscoveryDefinition definition) => TowerSuppliedCompositionSearch.RunAsync(definition,
            mechanics, (p, _, _) => Task.FromResult(I.Measure(input, p, d.Starts.Any(s => s.Party.Id == p.Id) ? 0 : 8)));
        var before = await Discover(old); var after = await Discover(d);
        Assert.Equal("Complete", before.Status); Assert.Equal(Json(before), Json(after));
        Assert.All(d.Starts, s => Assert.Contains(after.DiscoveryShortlist, p => p.Id == s.Party.Id));
        var shortlist = Shortlist(old); var rows = Measurements(old, shortlist, 22, 18, 20, 22);
        var original = TowerBossStudyPolicy.Select(input, mechanics, shortlist, rows,
            old.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value.Selection), 1, TowerBossStudyPolicy.ZeroWinVersion);
        Assert.Equal(Json(original), Json(TowerBossStudyPolicy.Select(old, mechanics, shortlist, rows)));
        Assert.Equal(shortlist[0].Id, original.Single().Party.Id);
        Assert.DoesNotContain("selectionPrimaryReferenceId", Json(old));
        Assert.DoesNotContain("incumbentSelection", Json(TowerBossStudyPolicy.Freeze(old, shortlist, rows, original, 640)));
    }

    [Theory]
    [InlineData("incumbent")] [InlineData("racing")] [InlineData("anchored")]
    public void Allocation_binds_the_opt_in_but_closed_comparisons_reject_it(string method)
    {
        var old = method switch { "racing" => BalanceHarnessEvaluationAllocationTests.Definition(),
            "anchored" => BalanceHarnessAnchoredNeighborhoodTests.Definition(), _ => I.Definition() };
        // Selection designation may differ from the anchored generator's explicit parent.
        var d = OptIn(old, "anchor-1"); TowerBossDiscovery.Validate(d);
        var template = BalanceHarnessPracticalAllocationTests.Template(d with { ExcludedCombatSeeds = [-987] });
        var q = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, "content", "definition", new string('a', 64),
            "registry", "output", new Dictionary<string, string>(), 300, 32 * 1048576,
            Allocation: new(77, "tie-fixture", method == "racing" ? 48 : 8, 32, 256));
        var bound = TowerPracticalSearch.ValidateAllocationTemplate(q, template);
        Assert.Equal(d.Stages.SelectionPrimaryReferenceId, bound.Stages.SelectionPrimaryReferenceId);
        Assert.Equal(d.Stages.SelectionPolicyVersion, bound.Stages.SelectionPolicyVersion);
        Assert.Equal(d.PrimaryReferenceId, bound.PrimaryReferenceId);
        Assert.Throws<InvalidDataException>(() => TowerAllocationComparison.ValidateSelection(template));
        Assert.Throws<InvalidDataException>(() => TowerAllocationComparison.Bind(template, Enumerable.Range(1, 3243).ToArray(), 0, false));
        if (method == "anchored")
            Assert.Throws<InvalidDataException>(() => TowerAnchoredComparison.Bind(template, Enumerable.Range(1, 3123).ToArray(), 0, true));
    }

    [Fact]
    public async Task Native_archive_reconstructs_designation_and_rejects_rehashed_tampering_without_combat()
    {
        using var temp = new DiscoveryTemp(); var root = TestContentPaths.FindApiRoot();
        var source = BalanceHarnessTowerBossDiscoveryContractTests.Definition(); var context = source.Contexts.Single().Id;
        var ids = source.AllowedEssences.DistinctBy(e => e.Family, StringComparer.OrdinalIgnoreCase).Take(12)
            .Select(e => e.Id).Order(StringComparer.Ordinal).ToArray();
        source = source with {
            AllowedEssences = source.AllowedEssences.Where(e => ids.Contains(e.Id)).ToArray(),
            Generation = source.Generation with { CandidatesPerArm = 9, MaximumAttemptsPerArm = 128, Seeds = [613719] },
            Stages = new(4, 1, 0, 0, new Dictionary<string, BossDiscoverySchedule> { [context] = new([81091], [82091], [83091], []) }),
            MaximumBattles = 25
        };
        var first = TowerPartySelection.Choice("fixture", source.Contexts.Single().CharacterTemplates.ToDictionary(p => p.PartySlot,
            _ => (IReadOnlyList<string>)ids.Take(4).ToArray()));
        var second = TowerPartySelection.Choice("fixture", first.Builds.ToDictionary(p => p.Key,
            p => p.Key == 1 ? (IReadOnlyList<string>)ids.Take(3).Append(ids[4]).ToArray() : p.Value));
        source = source with { References = new[] { first, second }.Select((p, i) => new BossBenchmarkReference("anchor-" + i, context,
            TowerBossDiscovery.Scenario(source, context, p, []), "Native selector integration fixture; no quality inference", new string('d', 64))).ToArray() };
        var d = OptIn(TowerSuppliedCompositionSearch.Prepare(source, ["anchor-0", "anchor-1"], TowerSuppliedCompositionSearch.IncumbentVersion));
        var path = Path.Combine(temp.Path, "study");
        var report = await TowerBossStudy.RunAsync(root, path, d);
        Assert.Equal("Complete", report.Status); Assert.Null(report.Error);
        Assert.Equal(9, report.Accounting.Completed["discovery"]); Assert.Equal(4, report.Accounting.Completed["selection"]);
        Assert.Equal(d.Starts[0].Party.Id, report.Confirmation!.IncumbentSelection!.PartyId);
        Assert.Contains(report.Confirmation.IncumbentSelection.Reason, TowerBossStudy.Markdown(report, d));
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Verification executed combat.")).Activate();
        Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerBossStudy.VerifyAsync(path)));
        void Rehash(string name, object value)
        {
            File.WriteAllText(Path.Combine(path, name), Json(value));
            var manifestPath = Path.Combine(path, "files.json");
            var files = HarnessJson.Read<Dictionary<string, string>>(manifestPath);
            files[name] = HarnessJson.FileHash(Path.Combine(path, name));
            File.WriteAllText(manifestPath, Json(files));
        }
        Rehash("confirmation-freeze.json", report.Confirmation with { IncumbentSelection = report.Confirmation.IncumbentSelection with { PartyId = second.Id } });
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossStudy.VerifyAsync(path));
        Rehash("confirmation-freeze.json", report.Confirmation);
        Rehash("definition.json", OptIn(d, "anchor-1"));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossStudy.VerifyAsync(path));
    }
}
