using BalanceHarness;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

// Conditional component integration, not an end-to-end search or combat study.
// Freeze the archive and the 30 mutation opportunities; vary retention only.
[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessControlledRetentionTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Controlled fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    internal sealed record State(string Family, PartyChoice Party, int Score, BossGeneratedProposal Proposal, BossDiscoveryMeasurement Measurement);
    internal sealed record Step(int Mutation, int ProposalTurn, string[] Population, string[] EligibleParents,
        string ParentId, string ParentProvenance, int ParentScore, string? DonorId, int? DonorScore,
        string Operator, int OperatorOrdinal, int[] RandomChoices, PartyChoice? Child,
        string Result, string? Rejection, int? Score, bool ImprovesEveryParent);
    internal sealed record Trace(int LabelMap, string Retention, bool ExternalAnchor, int OpportunityBudget,
        State[] InitialArchive, string[] Anchors, string ValleyId, Step[] Steps, int InitialBest, int FinalBest,
        bool ValleyRetained, bool ValleySelected, bool ValleyEssenceOpportunity, bool StrictValleyImprovement,
        string Interpretation);

    private sealed class Transcript(IEnumerable<int> values) : Random
    {
        private readonly Queue<int> values = new(values);
        internal int Remaining => values.Count;
        public override int Next(int maximum) => Next(0, maximum);
        public override int Next(int minimum, int maximum) { var value = values.Dequeue(); Assert.InRange(value, minimum, maximum - 1); return value; }
    }
    private static int[] Shuffle(int count, IEnumerable<int> prefix)
    {
        var desired = prefix.Concat(Enumerable.Range(0, count).Except(prefix)).ToArray();
        var current = Enumerable.Range(0, count).ToArray(); var choices = new List<int>();
        for (var at = 0; at < count - 1; at++) {
            var from = Array.IndexOf(current, desired[at], at); choices.Add(from); (current[at], current[from]) = (current[from], current[at]);
        }
        return choices.ToArray();
    }
    private static string Label(int value, int map) => "e" + (map switch { 0 => value, 1 => 19 - value, 2 => (7 * value + 3) % 20, _ => throw new InvalidOperationException() }).ToString("D2");
    private static PartyChoice Party(IEnumerable<int> values, int map) => TowerPartySelection.Choice("controlled",
        new Dictionary<int, IReadOnlyList<string>> { [1] = values.Select(v => Label(v, map)).Order(StringComparer.Ordinal).ToArray() });
    private static BossDiscoveryMeasurement Measure(BossDiscoveryInputs input, PartyChoice party, int score)
    {
        var original = F.Measure(input, party); var cells = original.Cells.Select(c => c with { GuardianHealth = 100 - score * 10 }).ToArray();
        return original with { Cells = cells, Fitness = TowerBossGeneration.Fitness(input, cells, 100) };
    }
    private static void Save(string name, object value)
    {
        var output = Environment.GetEnvironmentVariable("LL_CONTROLLED_TRACE_OUTPUT");
        if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output); HarnessJson.WriteNew(Path.Combine(output, name + ".json"), value);
    }
    internal static Trace Run(int map, bool externalAnchor, bool diverse, bool scheduled = false)
    {
        var input = F.Input(owners: 1, poolSize: 20);
        var a = Enumerable.Range(3, 10).Select(i => Party([0, 1, 2, i], map)).ToArray();
        var b = Party([14, 15, 16, 17], map);
        var targets = new[] { Party([14, 15, 18, 19], map), Party([13, 14, 18, 19], map) };
        int Score(PartyChoice party) => targets.Any(p => p.Id == party.Id) ? 9 : party.Id == b.Id ? 5 : a.Any(p => p.Id == party.Id) ? 6 : 0;
        var archive = a.Append(b).Select((p, i) => new State(i == 10 ? "B" : "A", p, Score(p),
            new(new("initial-" + i, 17, TowerSuppliedCompositionSearch.Block, "fresh-legal", [], []), p, "controlled", null, "evaluated"), Measure(input, p, Score(p)))).ToArray();
        var rows = archive.Select(s => s.Measurement).ToArray(); var measured = archive.ToDictionary(s => s.Party.Id, s => s.Proposal);
        var full = TowerSuppliedCompositionSearch.Retain(rows, measured);
        Assert.Equal(8, full.Length); Assert.Equal(b.Id, full[4]);
        Assert.All(a, p => Assert.All(targets, target => Assert.Equal(8, TowerSuppliedCompositionSearch.Distance(p, target))));
        var anchor = externalAnchor ? a.Where(p => !full.Contains(p.Id)).OrderBy(p => p.Id, StringComparer.Ordinal).First().Id : full[0];
        string[] anchors = [anchor]; var steps = new List<Step>(); var seen = archive.Select(s => s.Party.Id).ToHashSet();
        var schedule = new TowerSuppliedCompositionSearch.BlockSchedule();
        for (var mutation = 0; mutation < 30; mutation++) {
            // The same production planner and operator are used in both retention arms.
            var population = diverse ? TowerSuppliedCompositionSearch.Retain(rows, measured) : TowerBossGeneration.Rank(rows).Take(4).Select(r => r.Id).ToArray();
            var plan = scheduled ? schedule.Next(population, anchors, mutation)
                : TowerSuppliedCompositionSearch.PlanBlockMutation(population, anchors, mutation);
            var parent = archive.Single(s => s.Party.Id == plan.ParentId);
            var eligible = population.Concat(anchors).Distinct().ToArray();
            var donor = plan.Operator == "donor-block" ? archive.Single(s => s.Party.Id == eligible.First(id => id != parent.Party.Id)) : null;
            var choices = Array.Empty<int>();
            if (plan.Operator == "essence-block") {
                var count = 2 + plan.OperatorOrdinal % 2; var old = parent.Party.Builds[1].ToArray();
                var replaced = parent.Party.Id == b.Id ? (count == 2 ? new[] { 16, 17 } : new[] { 15, 16, 17 }).Select(v => Array.IndexOf(old, Label(v, map))).ToArray()
                    : Enumerable.Range(0, count).ToArray();
                var replacements = (count == 2 ? new[] { 18, 19 } : new[] { 13, 18, 19 }).Select(v => Label(v, map));
                var pool = input.AllowedEssences.OrderBy(e => e.Id, StringComparer.Ordinal).Select(e => e.Id).ToArray();
                choices = new[] { 0 }.Concat(Shuffle(4, replaced)).Concat(Shuffle(pool.Length, replacements.Select(id => Array.IndexOf(pool, id)))).ToArray();
            }
            var random = new Transcript(choices);
            var proposed = TowerSuppliedCompositionSearch.Propose(input, parent.Party, donor?.Party, plan.Operator, plan.OperatorOrdinal, random);
            Assert.Equal(0, random.Remaining);
            var rejection = proposed.Rejection; int? score = null; var result = "rejected";
            if (proposed.Party is not null) {
                TowerBossDiscovery.ValidateParty(F.Definition(input), proposed.Party);
                Assert.Equal(proposed.Party.Builds[1].Order(StringComparer.Ordinal), proposed.Party.Builds[1]);
                score = Score(proposed.Party); result = seen.Add(proposed.Party.Id) ? "evaluated" : "duplicate";
            }
            var improved = result == "evaluated" && score > parent.Score && (donor is null || score > donor.Score);
            steps.Add(new(mutation, mutation + mutation / 3, population, eligible, parent.Party.Id, parent.Proposal.Provenance.Id, parent.Score,
                donor?.Party.Id, donor?.Score, plan.Operator, plan.OperatorOrdinal, choices, proposed.Party, result, rejection, score, improved));
        }
        var trace = new Trace(map, diverse ? "diverse" : "elite-only", externalAnchor, 30, archive, anchors, b.Id, steps.ToArray(), 6,
            Math.Max(6, steps.Where(s => s.Result == "evaluated").Select(s => s.Score!.Value).DefaultIfEmpty(0).Max()),
            steps.All(s => s.Population.Contains(b.Id)), steps.Any(s => s.ParentId == b.Id),
            steps.Any(s => s.ParentId == b.Id && s.Operator == "essence-block"),
            steps.Any(s => s.ParentId == b.Id && s.Score == 9 && s.ImprovesEveryParent),
            "Fixed archive and prescribed random choices; one-owner essence-block mechanism only; no adaptive search or strength estimate.");
        Save((scheduled ? "v2-" : "") + $"map-{map}-parents-{(externalAnchor ? 9 : 8)}-{trace.Retention}", trace); return trace;
    }
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Eight_parent_fixture_connects_diverse_retention_to_a_strict_coordinated_improvement(int map)
    {
        var diverse = Run(map, false, true); var elite = Run(map, false, false);
        Assert.True(diverse.ValleyRetained && diverse.ValleySelected && diverse.ValleyEssenceOpportunity && diverse.StrictValleyImprovement);
        Assert.Equal(9, diverse.FinalBest); Assert.Equal(6, elite.FinalBest); Assert.False(elite.ValleyRetained || elite.ValleySelected);
        Assert.All(diverse.Steps, s => Assert.Equal(8, s.EligibleParents.Length));
        Assert.All(diverse.Steps.GroupBy(s => s.ParentId), g => Assert.Equal(3, g.Select(s => s.Operator).Distinct().Count()));
    }
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Nine_parent_fixture_exposes_operator_starvation_despite_retaining_and_selecting_the_valley(int map)
    {
        var diverse = Run(map, true, true); var elite = Run(map, true, false);
        Assert.True(diverse.ValleyRetained && diverse.ValleySelected);
        Assert.False(diverse.ValleyEssenceOpportunity || diverse.StrictValleyImprovement);
        Assert.Equal(6, diverse.FinalBest); Assert.Equal(6, elite.FinalBest);
        Assert.All(diverse.Steps, s => Assert.Equal(9, s.EligibleParents.Length));
        Assert.All(diverse.Steps.GroupBy(s => s.ParentId), g => Assert.Single(g.Select(s => s.Operator).Distinct()));
        Assert.All(diverse.Steps.Where(s => s.ParentId == diverse.ValleyId), s => Assert.Equal("character-block", s.Operator));
    }
    [Fact]
    public void Fixed_population_counter_rule_preserves_the_existing_period_for_eight_nine_and_ten_parents()
    {
        foreach (var count in new[] { 8, 9, 10 }) {
            var population = Enumerable.Range(0, 8).Select(i => "parent-" + i).ToArray();
            var anchors = new[] { population[0] }.Concat(Enumerable.Range(8, count - 8).Select(i => "parent-" + i)).ToArray();
            var eligible = population.Concat(anchors).Distinct().ToArray();
            var plans = Enumerable.Range(0, count * 3).Select(i => TowerSuppliedCompositionSearch.PlanBlockMutation(population, anchors, i)).ToArray();
            Assert.Equal(count, plans.Select(p => p.ParentId).Distinct().Count());
            Assert.All(plans.GroupBy(p => p.ParentId), g => Assert.Equal(count == 9 ? 1 : 3, g.Select(p => p.Operator).Distinct().Count()));
            for (var i = 0; i < plans.Length; i++) {
                Assert.Equal(eligible[i % count], plans[i].ParentId);
                Assert.Equal(new[] { "essence-block", "character-block", "donor-block" }[i % 3], plans[i].Operator);
                Assert.Equal(i / 3, plans[i].OperatorOrdinal);
            }
        }
    }
}
