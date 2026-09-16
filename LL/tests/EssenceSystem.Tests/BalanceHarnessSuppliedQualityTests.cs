using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

// A green test verifies execution/accounting. The separately persisted quality
// gate may Fail; never turn that scientific outcome into an automatic retry.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessSuppliedQualityTests
{
    private sealed record Arm(string Method, int Root, string Status, int Evaluations, int Proposals,
        int InitialBest, int Best, int Oracle, int Regret, double NormalizedRegret,
        bool RecoveredAfterInitial, string? FirstBestOperator, string? FirstBestProposal);
    private sealed record Case(string Landscape, int Map, int Oracle, int LegalParties,
        string Status, Arm[] Arms, bool BudgetComplete, int BlockRecoveries);
    private static string Label(int semantic, int map) => "e" + (map switch {
        0 => semantic, 1 => 7 - semantic, 2 => (3 * semantic + 1) % 8, _ => throw new InvalidOperationException()
    }).ToString("D2");
    private static PartyChoice Party(int first, int second, int map) => TowerPartySelection.Choice("quality-fixture",
        new Dictionary<int, IReadOnlyList<string>> {
            [1] = Enumerable.Range(0, 4).Select(f => Label(f + ((first & (1 << f)) == 0 ? 0 : 4), map)).Order(StringComparer.Ordinal).ToArray(),
            [2] = Enumerable.Range(0, 4).Select(f => Label(f + ((second & (1 << f)) == 0 ? 0 : 4), map)).Order(StringComparer.Ordinal).ToArray()
        });
    private static int Score(PartyChoice party, string landscape, int map)
    {
        bool Bit(int owner, int family) => party.Builds[owner].Contains(Label(family + 4, map));
        int Trap(bool a, bool b) => a && b ? 3 : !a && !b ? 2 : 0;
        return landscape switch {
            "additive" => Enumerable.Range(1, 2).Sum(o => Enumerable.Range(0, 4).Count(f => Bit(o, f))),
            "within" => Enumerable.Range(1, 2).Sum(o => Trap(Bit(o, 0), Bit(o, 1)) + Trap(Bit(o, 2), Bit(o, 3))),
            "cross" => Enumerable.Range(0, 4).Sum(f => Trap(Bit(1, f), Bit(2, f))),
            _ => throw new InvalidOperationException()
        };
    }
    private static void Save(string name, object value)
    {
        var root = Environment.GetEnvironmentVariable("LL_SUPPLIED_QUALITY_OUTPUT");
        if (string.IsNullOrEmpty(root)) return;
        Directory.CreateDirectory(root); HarnessJson.WriteNew(Path.Combine(root, name + ".json"), value);
    }

    [Fact]
    public async Task Frozen_end_to_end_quality_matrix_records_all_outcomes_and_a_separate_gate()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Quality fixture entered combat.")).Activate();
        using var encoding = HarnessJson.UseCompactOutput();
        var cases = new List<Case>();
        foreach (var landscape in new[] { "additive", "within", "cross" })
        foreach (var map in new[] { 0, 1, 2 }) {
            var input = F.Input(owners: 2, poolSize: 8, candidates: 64, attempts: 256) with {
                AllowedEssences = Enumerable.Range(0, 8).Select(i => new BossDiscoveryEssence(Label(i, map), "family" + i % 4)).ToArray()
            };
            var source = F.Definition(input); var start = Party(0, 0, map);
            source = source with {
                Generation = source.Generation with { Seeds = new[] { 17, 31, 47 } },
                References = [new("saved-zero", "fixture", TowerBossDiscovery.Scenario(source, "fixture", start, []),
                    "Frozen synthetic all-zero anchor; no historical fitness", new string('d', 64))]
            };
            var d = TowerSuppliedCompositionSearch.Prepare(source, ["saved-zero"], TowerSuppliedCompositionSearch.ScheduledVersion);
            var prepared = TowerBossImprovement.Inputs(d);
            var space = (from a in Enumerable.Range(0, 16) from b in Enumerable.Range(0, 16) select Party(a, b, map)).ToArray();
            Assert.Equal(256, space.Select(p => p.Id).Distinct().Count());
            Assert.All(space, p => TowerBossDiscovery.ValidateParty(d, p));
            var oracle = space.Max(p => Score(p, landscape, map));
            Assert.Equal(landscape == "additive" ? 8 : 12, oracle);
            Assert.Single(space.Where(p => Score(p, landscape, map) == oracle));
            Assert.Equal(oracle, Score(Party(15, 15, map), landscape, map));
            if (landscape != "additive") Assert.All(space.Where(p => TowerSuppliedCompositionSearch.Distance(start, p) == 2),
                p => Assert.True(Score(p, landscape, map) < Score(start, landscape, map)));
            var result = await TowerBossImprovement.ExecuteAsync(d, prepared, F.Mechanics(prepared), (party, _, token) => {
                token.ThrowIfCancellationRequested(); var original = F.Measure(prepared, party);
                var cells = original.Cells.Select(c => c with { GuardianHealth = 100 - 5 * Score(party, landscape, map) }).ToArray();
                return Task.FromResult(original with { Cells = cells, Fitness = TowerBossGeneration.Fitness(prepared, cells, 100) });
            }, default);
            var name = landscape + "-map-" + map;
            Save(name + "-definition", d); Save(name + "-report", result);
            Assert.Equal(6, result.Arms.Count);
            foreach (var root in new[] { 17, 31, 47 }) {
                var pair = result.Arms.Where(a => a.Seed == root).ToArray(); Assert.Equal(2, pair.Length);
                Assert.Equal(pair[0].Proposals.Take(7).Select(p => p.Party?.Id), pair[1].Proposals.Take(7).Select(p => p.Party?.Id));
            }
            var arms = result.Arms.Select(a => {
                var evaluated = a.Proposals.Where(p => p.Result == "evaluated").ToArray();
                Assert.Equal(evaluated.Length, a.Evaluations.Count); Assert.Equal(evaluated.Length, evaluated.Select(p => p.Party!.Id).Distinct().Count());
                Assert.All(evaluated, p => {
                    TowerBossDiscovery.ValidateParty(d, p.Party!);
                    Assert.All(p.Party!.Builds.Values, ids => Assert.True(TowerCompositionSearch.IsCanonical(ids)));
                    Assert.Equal(100 - 5 * Score(p.Party!, landscape, map), a.Evaluations.Single(r => r.Id == p.Party!.Id).Fitness.GuardianHealth);
                });
                var initial = a.Proposals.Take(7).Where(p => p.Result == "evaluated").Select(p => Score(p.Party!, landscape, map)).DefaultIfEmpty(0).Max();
                var best = evaluated.Select(p => Score(p.Party!, landscape, map)).DefaultIfEmpty(0).Max();
                var first = evaluated.FirstOrDefault(p => Score(p.Party!, landscape, map) == best);
                return new Arm(a.Method, a.Seed, a.StopReason, a.Evaluations.Count, a.Proposals.Count, initial, best, oracle, oracle - best,
                    (oracle - best) / (double)oracle, best == oracle && best > initial, first?.Provenance.Operator, first?.Provenance.Id);
            }).ToArray();
            cases.Add(new(landscape, map, oracle, space.Length, result.Status, arms,
                result.Status == "Complete" && arms.All(a => a.Evaluations == 64 && a.Proposals <= 256),
                arms.Count(a => a.Method == TowerSuppliedCompositionSearch.Block && a.RecoveredAfterInitial)));
        }
        var all = cases.SelectMany(c => c.Arms).ToArray();
        var block = all.Where(a => a.Method == TowerSuppliedCompositionSearch.Block).Sum(a => a.NormalizedRegret);
        var baseline = all.Where(a => a.Method == TowerSuppliedCompositionSearch.Baseline).Sum(a => a.NormalizedRegret);
        var complete = cases.All(c => c.BudgetComplete);
        var recovery = cases.Where(c => c.Landscape != "additive").All(c => c.BlockRecoveries >= 2);
        var competitive = block <= baseline + 1e-12;
        Save("quality-gate", new {
            status = complete && recovery && competitive ? "Pass" : "Fail", budgetsComplete = complete,
            coordinatedRecovery = recovery, aggregateNonRegression = competitive,
            blockNormalizedRegret = block, baselineNormalizedRegret = baseline,
            cases, newFights = 0, newBalanceValues = 0,
            interpretation = "Prospective finite synthetic engineering gate. No combat-strength or statistical superiority claim. Do not retry a quality failure."
        });
    }
}
