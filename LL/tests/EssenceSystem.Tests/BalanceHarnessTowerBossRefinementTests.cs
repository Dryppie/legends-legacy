using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

public sealed class BalanceHarnessTowerBossRefinementTests
{
    private static string Root => TestContentPaths.FindApiRoot();
    private static string Catalogs => Path.GetFullPath(Path.Combine(Root, "../../../tools/BalanceHarness/Fixtures"));

    [Theory]
    [InlineData(7, 5, 42)]
    [InlineData(8, 4, 35)]
    [InlineData(13, 7, 53)]
    public void Default_refinement_retains_every_historical_finalist_with_fresh_combat_seeds(int floor, int slots, int count)
    {
        var d = TowerBossSearch.Definition(Root, Catalogs, floor, slots, 920531 + floor, "any", "coverage");
        Assert.Equal(2, d.SchemaVersion);
        Assert.Equal(TowerBossSearch.RefinementVersion, TowerBossSearch.Algorithm(d));
        Assert.Equal(count, d.Controls.Count);
        Assert.Contains(d.Controls, p => p.Id == d.Refinement!.AnchorId);
        Assert.NotNull(d.Refinement!.ReferenceSetId);
        Assert.Contains("historical", d.Refinement.ReferenceEvidenceStatus!, StringComparison.OrdinalIgnoreCase);
        Assert.True(d.ExcludedCombatSeeds.Count >= 16760);
        Assert.Empty(TowerBossSearch.CombatSeeds(d).Intersect(d.ExcludedCombatSeeds));
        Assert.Equal(d.MaximumBattles, TowerBossSearch.Validate(d));
        Assert.Equal(count + 8, d.CandidatesPerArm);
    }

    [Fact]
    public void Version_one_omits_new_fields_and_version_two_rejects_undefined_policies_and_anchors()
    {
        var old = TowerBossSearch.Definition(Root, Catalogs, 3, 4, 920540, "any", "coverage", refinement: false);
        Assert.Equal(TowerBossSearch.Version, TowerBossSearch.Algorithm(old));
        Assert.DoesNotContain("refinement", JsonSerializer.Serialize(old, HarnessJson.Options));
        Assert.DoesNotContain("recovery", JsonSerializer.Serialize(new BossBehavior(0, 0, 0, 0, 0), HarnessJson.Options));
        var d = TowerBossSearch.Definition(Root, Catalogs, 13, 7, 920541, "any", "coverage");
        var invalid = new[] {
            d with { Refinement = null },
            d with { SchemaVersion = 1 },
            old with { Refinement = d.Refinement },
            d with { Refinement = d.Refinement! with { AnchorId = "unknown" } },
            d with { Refinement = d.Refinement! with { DiagnosticPolicy = "arbitrary" } },
            d with { Refinement = d.Refinement! with { ProposalPolicy = "arbitrary" } },
            d with { Refinement = d.Refinement! with { ReferenceSetId = null } },
            d with { Finalists = 97 },
            d with { CandidatesPerArm = 101 }
        };
        Assert.All(invalid, changed => Assert.Throws<InvalidDataException>(() => TowerBossSearch.Validate(changed)));
    }

    [Theory]
    [InlineData("anchor")]
    [InlineData("evidence-status")]
    [InlineData("omitted-reference")]
    public async Task Official_reference_tampering_fails_preflight_before_any_combat(string tamper)
    {
        var temp = Path.Combine(Path.GetTempPath(), "tower-boss-reference-preflight-" + Guid.NewGuid().ToString("N"));
        try
        {
            var d = TowerBossSearch.Definition(Root, Catalogs, 7, 5, 920570, "any", "coverage");
            d = tamper switch
            {
                "anchor" => d with { Refinement = d.Refinement! with { AnchorId = d.Controls.First(p => p.Id != d.Refinement!.AnchorId).Id } },
                "evidence-status" => d with { Refinement = d.Refinement! with { ReferenceEvidenceStatus = "Freshly confirmed current evidence" } },
                _ => d with { Controls = d.Controls.Where((p, i) => i == 0 || p.Id == d.Refinement!.AnchorId).ToArray() }
            };
            // These values remain structurally valid. The frozen reference payload must reject their changed meaning.
            Assert.True(TowerBossSearch.Validate(d) > 0);
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => TowerBossSearch.RunAsync(Root, Catalogs, temp, d));
            Assert.Contains("historical boss references", error.Message);
            var report = HarnessJson.Read<TowerBossSearchReport>(Path.Combine(temp, "boss-search.json"));
            Assert.Equal("Invalid", report.Status);
            Assert.Equal(0, report.ActualBattles);
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(temp, "battles")));
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(temp, "recipes")));
        }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }

    [Theory]
    [InlineData("random")]
    [InlineData("legacy")]
    [InlineData("joint")]
    [InlineData("graph")]
    public async Task Anchored_proposals_are_deterministic_and_leave_uniform_random_arm_unchanged(string method)
    {
        PartyChoice Party(string source, string[] first, string[] second) => TowerPartySelection.Choice(source,
            new Dictionary<int, IReadOnlyList<string>> { [1] = first, [2] = second, [3] = new[] { "a", "f" } });
        var families = new[] { "a", "b", "c", "d", "e", "f", "g", "h" }.ToDictionary(id => id, id => id);
        PartyChoice[] starts = [Party("control", ["a", "b"], ["c", "d"]), Party("anchor", ["a", "e"], ["c", "f"])];
        PartyChoice[] mechanism = [Party("A", ["g", "e"], ["c", "f"]), Party("B", ["a", "e"], ["h", "f"]),
            Party("AB", ["g", "e"], ["h", "f"])];
        Task<BossMeasurement> Evaluate(PartyChoice p, CancellationToken _) => Task.FromResult(new BossMeasurement(p.Id,
            new(p.Builds.Values.Sum(ids => ids.Count(id => id == "e")), 40, 60, 10), [], new(0, 0, 0, 0, 0)));
        Task<BossSearchResult> Run(string? anchor, IReadOnlyList<PartyChoice>? quartet) => TowerBossOptimization.RunAsync(
            method, 920550, 24, 2000, starts, families, [1, 2], [("g", "h")], Evaluate,
            anchorId: anchor, mechanismStarts: quartet);
        var result = await Run(starts[1].Id, mechanism);
        Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(await Run(starts[1].Id, mechanism)));
        Assert.Equal(24, result.Evaluations.Count);
        Assert.Equal(starts.Select(p => p.Id), result.Parties.Take(2).Select(p => p.Id));
        Assert.All(result.Parties, p => Assert.Equal(starts[0].Builds[3], p.Builds[3]));
        if (method == "random")
        {
            Assert.Equal(HarnessJson.Hash(await Run(null, null)), HarnessJson.Hash(result));
            Assert.All(result.Proposals.Skip(2), p => Assert.Equal("random-restart", p.Origin));
        }
        else
        {
            Assert.Contains(result.Proposals, p => p.Parent == starts[1].Id && p.Origin.StartsWith("refinement-"));
            Assert.Contains(result.Proposals, p => p.Origin == "random-restart");
            if (method == "graph")
                Assert.Equal(mechanism.Select(p => p.Id), result.Proposals.Where(p => p.Origin == "boss-mechanic-replacement").Select(p => p.Party.Id));
        }
    }

    [Fact]
    public async Task Refinement_reconstructs_frozen_search_and_both_recovery_routes_from_actual_target_reports()
    {
        var temp = Path.Combine(Path.GetTempPath(), "tower-boss-refinement-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var d = TowerBossSearch.Definition(Root, Catalogs, 13, 7, 920560, "protection", "coverage");
            var anchor = d.Controls.Single(p => p.Id == d.Refinement!.AnchorId);
            d = d with { Controls = [d.Controls[0], anchor], GenerationSeeds = d.GenerationSeeds.Take(1).ToArray(),
                CandidatesPerArm = 5, DiscoverySamples = 1, ConfirmationSamples = 1, DiagnosticSamples = 1,
                Finalists = 11, Refinement = d.Refinement! with { ReferenceSetId = null, ReferenceEvidenceStatus = null } };
            var report = await TowerBossSearch.RunAsync(Root, Catalogs, temp, d);
            Assert.Equal("Complete", report.Status);
            Assert.Equal(HarnessJson.Hash(report), HarnessJson.Hash(await TowerBossSearch.VerifyAsync(temp)));
            var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(temp, "scope.json"));
            Assert.Equal(TowerBossSearch.RefinementVersion, scope.Algorithm);
            Assert.All(report.Arms, arm => Assert.Equal(5, arm.Evaluations.Count));
            Assert.Equal(4, report.Diagnostics.Count);
            Assert.Contains(report.Confirmation, row => row.Behavior.Recovery!.GuardianHealing > 0);
            foreach (var row in report.Confirmation.Concat(report.Diagnostics))
            {
                var reports = row.Cells.Where(c => c.Floor == 13).SelectMany(c => c.Trials).Distinct()
                    .Select(id => TowerLoadoutArchive.ReadBattle(temp, id, scope.ReportStorage)).ToArray();
                var expected = new BossRecovery(
                    reports.Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Friendly.Any(f => f.Id == s.EntityId)).Sum(s => (double)s.HealthRegenerated)),
                    reports.Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Hostile.Any(f => f.Id == s.EntityId)).Sum(s => (double)s.HealingDone)),
                    reports.Average(r => r.Battle.Summary.Statistics.Where(s => r.Battle.Summary.Hostile.Any(f => f.Id == s.EntityId)).Sum(s => (double)s.HealthRegenerated)));
                Assert.Equal(expected, row.Behavior.Recovery);
            }
        }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
