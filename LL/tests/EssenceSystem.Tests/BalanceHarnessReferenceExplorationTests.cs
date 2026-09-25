using BalanceHarness;
using System.Security.Cryptography;
using System.Text.Json;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessReferenceExplorationTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Exploration fixture entered combat.")).Activate();
    public void Dispose() => guard.Dispose();

    [Theory]
    [InlineData(17, "1c78e2da2fc298f8a00a5b1b22851d78000b259aacf2a33df1dec48a7d9f8387")]
    [InlineData(31, "fd789e3f620347e6b4550727b080882c0d83b012b2c5b4886fc9f36caf004359")]
    [InlineData(47, "5d4f25a5140b804aaf4a8cd689f04c4ff7d94f048457bc6c3ce7613451e10d74")]
    public async Task Legacy_three_reference_trajectory_remains_byte_identical(int seed, string expected)
    {
        var d = BalanceHarnessThreeReferenceTests.Definition();
        d = d with { Generation = d.Generation with { Seeds = [seed] } };
        var input = TowerBossImprovement.Inputs(d);
        var result = await TowerSuppliedCompositionSearch.RunAsync(d, F.Mechanics(input),
            (party, _, _) => Task.FromResult(I.Measure(input, party, 3)));
        Assert.Equal("Complete", result.Status);
        Assert.Equal(expected, Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(result, HarnessJson.Options))));
        var destination = Environment.GetEnvironmentVariable("LL_EXPLORATION_PARITY_DIRECTORY");
        if (!string.IsNullOrEmpty(destination)) HarnessJson.WriteNew(Path.Combine(destination, $"legacy-{seed}.json"), result);
    }

    internal static TowerBossDiscoveryDefinition Definition(int owners = 5, string policyVersion = TowerReferenceExploration.Version)
    {
        var d = BalanceHarnessThreeReferenceTests.Definition(owners);
        return d with { Generation = d.Generation with { PolicyVersion = policyVersion } };
    }

    [Theory]
    [InlineData(3)] [InlineData(5)] [InlineData(10)] [InlineData(15)]
    public void Each_reference_alternates_radius_and_balances_character_visits_even_after_rejections(int owners)
    {
        var d = Definition(owners); var schedule = new TowerReferenceExploration.Schedule(d.Starts.Reverse().ToArray());
        var counts = d.Starts.ToDictionary(s => s.ReferenceId, _ => new int[owners]);
        var ids = d.Starts.Select(s => s.ReferenceId).Order(StringComparer.Ordinal).ToArray();
        for (var opportunity = 0; opportunity < 96; opportunity++)
        {
            var step = schedule.Next();
            Assert.Equal(ids[opportunity % 3], step.ReferenceId);
            Assert.Equal(opportunity / 3, step.Visit); Assert.Equal(2 + step.Visit % 2, step.Radius);
            Assert.Equal(step.Radius, step.ScheduledSlots.Distinct().Count());
            foreach (var slot in step.ScheduledSlots) counts[step.ReferenceId][slot - 1]++;
            Assert.InRange(counts[step.ReferenceId].Max() - counts[step.ReferenceId].Min(), 0, 1);
            // No result is fed back: rejected, duplicate and accepted opportunities consume the same schedule.
        }
    }

    [Fact]
    public void Construction_frees_all_selected_owned_copies_before_replacing_and_has_exact_distance()
    {
        var input = F.Input(owners: 3, poolSize: 12);
        input = input with { OwnedCopies = input.AllowedEssences.ToDictionary(e => e.Id, _ => 1) };
        var party = TowerPartySelection.Choice("fixture", Enumerable.Range(1, 3).ToDictionary(s => s,
            s => (IReadOnlyList<string>)Enumerable.Range((s - 1) * 4, 4).Select(i => $"e{i:D2}").ToArray()));
        var step = new BossReferenceExplorationTrace("anchor", 0, 2, [1, 2], 0);
        var (choice, trace) = TowerReferenceExploration.Propose(input, party, step, new Random(17));
        Assert.Null(choice.Rejection); Assert.Equal(1, trace.ConstructionChecks);
        Assert.Equal(4, TowerSuppliedCompositionSearch.Distance(party, choice.Party!));
        Assert.Equal(party.Builds[3], choice.Party!.Builds[3]);
        Assert.Equal(party.Builds.Values.SelectMany(v => v).Order(), choice.Party.Builds.Values.SelectMany(v => v).Order());
        Assert.All(new[] { 1, 2 }, slot => Assert.Single(party.Builds[slot].Except(choice.Party.Builds[slot])));
    }

    [Theory] [InlineData(null)] [InlineData(17)]
    public void Exhausted_construction_is_bounded_and_cannot_refill_the_reference_visit(int? offsetSeed)
    {
        var input = F.Input(owners: 3, poolSize: 4);
        var party = TowerPartySelection.Choice("fixture", Enumerable.Range(1, 3).ToDictionary(s => s,
            _ => (IReadOnlyList<string>)["e00", "e01", "e02", "e03"]));
        var starts = Enumerable.Range(0, 3).Select(i => new BossDiscoveryStart("start-" + i, "anchor-" + i, party)).ToArray();
        var schedule = new TowerReferenceExploration.Schedule(starts, offsetSeed);
        for (var opportunity = 0; opportunity < 9; opportunity++)
        {
            var (choice, trace) = TowerReferenceExploration.Propose(input, party, schedule.Next(), new Random(17));
            Assert.Equal("construction-exhausted", choice.Rejection); Assert.Null(choice.Party);
            Assert.Equal(32, trace.ConstructionChecks); Assert.Equal(opportunity / 3, trace.Visit);
            Assert.Equal("anchor-" + opportunity % 3, trace.ReferenceId);
        }
    }

    [Theory]
    [InlineData(17, false)] [InlineData(31, false)] [InlineData(47, false)]
    [InlineData(17, true)] [InlineData(31, true)] [InlineData(47, true)]
    public async Task Only_periodic_fresh_opportunities_change_and_proposals_are_legal_reproducible_and_bounded(int seed, bool offsets)
    {
        var d = Definition(policyVersion: offsets ? TowerReferenceExploration.OffsetVersion : TowerReferenceExploration.Version);
        d = d with { Generation = d.Generation with { Seeds = [seed] } };
        d = d with { AllowedEssences = d.AllowedEssences.Select(e => e.Id == "e39" ? e with { Family = "FAMILY00" } : e).ToArray(),
            OwnedCopies = d.AllowedEssences.ToDictionary(e => e.Id, _ => d.RequiredPartySize) };
        var input = TowerBossImprovement.Inputs(d);
        Task<BossGenerationResult> Run(TowerBossDiscoveryDefinition definition) => TowerSuppliedCompositionSearch.RunAsync(definition, F.Mechanics(input),
            (party, _, _) => Task.FromResult(I.Measure(input, party, 3)));
        var before = HarnessJson.Hash(d);
        var result = await Run(d); var repeat = await Run(d);
        Assert.Equal("Complete", result.Status); Assert.Equal(HarnessJson.Hash(result), HarnessJson.Hash(repeat));
        if (!offsets)
        {
            // Captured from the v1 implementation before owner offsets were introduced.
            var expected = seed switch {
                17 => "ed843864bd1f24d8be55ed6936c4f024ce2d93789d6e44f1e7cfc8393e2fcd52",
                31 => "9d72f2a5277b2460639baff5bebe8ddd6a0a86a0384cafca5d68063b57a120ac",
                _ => "fa54ebd5ebfac7d4e12f9e37c670cce09573a99a73fcb3db1e75c8aef9cf6765" };
            Assert.Equal(expected, Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(result, HarnessJson.Options))));
        }
        Assert.Equal(before, HarnessJson.Hash(d));
        var legacy = await Run(d with { Generation = d.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.ThreeReferenceVersion } });
        var arm = Assert.Single(result.Arms); var old = Assert.Single(legacy.Arms);
        Assert.Equal(HarnessJson.Hash(old.Proposals.Take(12)), HarnessJson.Hash(arm.Proposals.Take(12)));
        Assert.Equal(6, arm.Proposals.Count(p => p.Provenance.Operator == "fresh-legal"));
        Assert.Equal(d.Generation.CandidatesPerArm, arm.Evaluations.Count);
        Assert.Equal(arm.Evaluations.Count, arm.Evaluations.Select(r => r.Id).Distinct().Count());
        Assert.InRange(arm.Proposals.Count, d.Generation.CandidatesPerArm, d.Generation.MaximumAttemptsPerArm);
        var visits = 0;
        for (var i = 0; i < arm.Proposals.Count; i++)
        {
            var p = arm.Proposals[i];
            if (p.Party is not null && (p.Result == "evaluated" || p.Provenance.Operator == TowerReferenceExploration.Operator))
                TowerBossDiscovery.ValidateParty(d, p.Party);
            if (i < 9 || (i - 9) % 4 != 3) { Assert.Null(p.Supplied!.Exploration); continue; }
            Assert.Equal(TowerReferenceExploration.Operator, p.Provenance.Operator);
            var trace = Assert.IsType<BossReferenceExplorationTrace>(p.Supplied!.Exploration);
            Assert.Equal("anchor-" + visits % 3, trace.ReferenceId); Assert.Equal(visits++ / 3, trace.Visit);
            Assert.Equal(2 + trace.Visit % 2, trace.Radius); Assert.InRange(trace.ConstructionChecks, 1, 32);
            var parent = Assert.Single(arm.Proposals, candidate => candidate.Provenance.Id == p.Provenance.ParentIds.Single());
            Assert.Equal("supplied", parent.Provenance.Operator);
            Assert.Equal(new[] { trace.ReferenceId }, p.Provenance.ReferenceIds);
            if (p.Party is null) continue;
            Assert.Equal(trace.ScheduledSlots, p.Supplied.ChangedSlots);
            Assert.Equal(trace.Radius * 2, TowerSuppliedCompositionSearch.Distance(parent.Party!, p.Party));
            Assert.All(trace.ScheduledSlots, slot => Assert.Single(parent.Party!.Builds[slot].Except(p.Party.Builds[slot])));
        }
        Assert.True(visits >= 6);
        Assert.Equal(5, result.DiscoveryShortlist.Count);
        Assert.All(d.Starts, start => Assert.Contains(result.DiscoveryShortlist, p => p.Id == start.Party.Id));
        var expectedChallengers = TowerBossGeneration.Rank(arm.Evaluations).Where(r => d.Starts.All(s => s.Party.Id != r.Id)).Take(2).Select(r => r.Id);
        Assert.Equal(expectedChallengers.Order(), result.DiscoveryShortlist.Where(p => d.Starts.All(s => s.Party.Id != p.Id)).Select(p => p.Id).Order());
        Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.ValidateProvenance(
            d with { Generation = d.Generation with { PolicyVersion = TowerSuppliedCompositionSearch.ThreeReferenceVersion } },
            arm.Proposals.Select(p => p.Provenance).ToArray()));
    }

    [Theory]
    [InlineData("improved", 4, "DemonstratedImprovement")]
    [InlineData("retain-third", 3, "IncumbentRetained")]
    [InlineData("improved", 4, "DemonstratedImprovement", TowerReferenceExploration.OffsetVersion)]
    [InlineData("retain-third", 3, "IncumbentRetained", TowerReferenceExploration.OffsetVersion)]
    public async Task Study_keeps_all_controls_confirmation_recipes_and_ten_quantity_intervals(string mode, int recipes, string decision, string policyVersion = TowerReferenceExploration.Version)
    {
        var d = Definition(policyVersion: policyVersion); var report = await BalanceHarnessThreeReferenceTests.Study(d, mode);
        Assert.Equal("Complete", report.Status); Assert.Equal(5, report.Selection.Count);
        Assert.Equal(recipes, report.Confirmation!.Members.Count);
        var result = TowerPracticalSearch.Assess(d, report, new string('a', 64));
        Assert.Equal(TowerPracticalSearch.ThreeReferenceVersion, result.Version); Assert.Equal(decision, result.StrengthDecision);
        Assert.Equal(3, result.Contrasts.Count); Assert.Equal(recipes, result.Rates!.Count);
        Assert.All(result.Rates, r => Assert.Equal(TowerBalanceEvaluator.Wilson(r.Wins, 256, 10), r.Estimate));
        Assert.Equal(1696, TowerBossDiscovery.Validate(d).Total);
    }

    [Theory] [InlineData(TowerReferenceExploration.Version)] [InlineData(TowerReferenceExploration.OffsetVersion)]
    public void Radius_three_rejects_two_character_definitions_before_execution(string policyVersion)
        => Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(Definition(2, policyVersion)));

    [Theory] [InlineData(TowerReferenceExploration.Version)] [InlineData(TowerReferenceExploration.OffsetVersion)]
    public async Task Finite_space_stops_at_attempt_cap_without_refilling_rejected_or_duplicate_exploration(string policyVersion)
    {
        var d = Definition(3, policyVersion);
        var starts = d.Starts.Select((s, i) => s with { Party = TowerPartySelection.Choice("fixture",
            Enumerable.Range(1, 3).ToDictionary(slot => slot, slot => (IReadOnlyList<string>)
                new[] { "e00", "e01", "e02", slot == 1 ? new[] { "e03", "e08", "e09" }[i] : "e03" })) }).ToArray();
        d = d with { Starts = starts, AllowedEssences = d.AllowedEssences.Where(e => new[] { "e00", "e01", "e02", "e03", "e08", "e09" }.Contains(e.Id))
            .Select(e => e.Id is "e08" or "e09" ? e with { Family = "family3" } : e).ToArray(),
            References = d.References.Select(r => r with { Scenario = TowerBossDiscovery.Scenario(d, "fixture",
                starts.Single(s => s.ReferenceId == r.Id).Party, []) }).ToArray() };
        var input = TowerBossImprovement.Inputs(d); var calls = 0;
        var result = await TowerSuppliedCompositionSearch.RunAsync(d, F.Mechanics(input), (p, _, _) => {
            calls++; return Task.FromResult(I.Measure(input, p)); });
        Assert.Equal("Incomplete", result.Status); Assert.Empty(result.DiscoveryShortlist);
        var arm = Assert.Single(result.Arms);
        Assert.Equal("ProposalBudgetExhausted", arm.StopReason); Assert.Equal(256, arm.Proposals.Count);
        Assert.Equal(arm.Evaluations.Count, calls); Assert.InRange(calls, 3, 27);
        var exploration = arm.Proposals.Where(p => p.Provenance.Operator == TowerReferenceExploration.Operator).ToArray();
        Assert.Equal(61, exploration.Length);
        Assert.Contains(exploration, p => p.Result == "duplicate");
        Assert.Contains(exploration, p => p.Result == "construction-exhausted");
        Assert.Equal(calls, arm.Proposals.Count(p => p.Result == "evaluated"));
        for (var i = 0; i < exploration.Length; i++)
        {
            var trace = exploration[i].Supplied!.Exploration!;
            Assert.Equal("anchor-" + i % 3, trace.ReferenceId); Assert.Equal(i / 3, trace.Visit);
        }
    }
}
