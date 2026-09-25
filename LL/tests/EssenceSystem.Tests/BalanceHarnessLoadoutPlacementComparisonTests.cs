using System.Buffers.Binary;
using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessLoadoutPlacementComparisonTests : IDisposable
{
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Placement study entered combat.")).Activate();
    public void Dispose() => guard.Dispose();
    private static (TowerProposalContext Context, TowerProposalComparisonPlan Plan, int[] History) Fixture() =>
        BalanceHarnessProposalStudyTests.Fixture(placement: true);
    private static int[] Values() => Enumerable.Range(200000, 4380).ToArray();

    [Fact]
    public void Binding_keeps_all_roles_disjoint_and_inventory_explicitly_asymmetric()
    {
        var (c, p, _) = Fixture(); var before = HarnessJson.Hash(c);
        var binding = TowerProposalComparison.Bind(p, c, Values());
        Assert.Equal(before, HarnessJson.Hash(c)); Assert.Equal(12, binding.Pairs.Count);
        Assert.Equal(HarnessJson.Hash(binding), HarnessJson.Hash(TowerProposalComparison.Bind(p, c, Values())));
        Assert.Equal((12, 256, 528, 4380, 21888), (p.Roots, p.HeldoutSamples, p.SearchFightsPerArm, p.RequiredFreshValues, p.MaximumFights));
        foreach (var pair in binding.Pairs)
        {
            TowerProposalComparison.ValidatePair(pair); TowerProposalComparison.ValidatePair(pair, p.Version);
            Assert.Equal(TowerProposalPolicies.AlliedActionValidationRacingVersion, pair.Control.Version);
            Assert.Equal(TowerProposalPolicies.LoadoutPlacementRacingVersion, pair.Candidate.Version);
            Assert.NotNull(pair.Control.DamageAffinityInventory); Assert.Null(pair.Candidate.DamageAffinityInventory);
            Assert.Equal(HarnessJson.Hash(pair.Control.Racing), HarnessJson.Hash(pair.Candidate.Racing));
            Assert.Equal(new[] { 8, 8, 8, 8, 16, 60 }, pair.Control.Racing.Panels.Select(x => x.Seeds.Count));
            Assert.Equal(Values().Skip(1308 + (pair.Root-1)*256).Take(256), pair.HeldoutSeeds);
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair, TowerProposalComparison.AlliedActionVersion));
        }
        Assert.Equal(Values(), binding.Pairs.SelectMany(pair => pair.Control.Racing.Panels.SelectMany(x => x.Seeds)
            .Append(pair.Control.Racing.RootSeed).Concat(pair.HeldoutSeeds)).Order());
    }

    [Theory]
    [InlineData("control")]
    [InlineData("candidate")]
    [InlineData("absolute")]
    [InlineData("novelty")]
    [InlineData("order")]
    [InlineData("budget")]
    [InlineData("layout")]
    [InlineData("selector")]
    [InlineData("old-version")]
    public void Frozen_plan_rejects_scientific_or_policy_drift(string field)
    {
        var (_, p, _) = Fixture();
        p = field switch {
            "control" => p with { Control = TowerProposalPolicies.BenchmarkPreservingAffinityCreation(p.Control.CreatedDamageAffinityIds!) },
            "candidate" => p with { Candidate = p.Control },
            "absolute" => p with { Analysis = p.Analysis with { GoBenchmarkAtLeast = 0 } },
            "novelty" => p with { Analysis = p.Analysis with { GoPromisingNovelRootsAtLeast = 0 } },
            "order" => p with { Analysis = p.Analysis with { DecisionOrder = TowerProposalComparison.SelectorDecisionOrder } },
            "budget" => p with { MaximumFights = 21889 },
            "layout" => p with { RequiredFreshValues = 4381 },
            "selector" => p with { SelectionContrast = p.SelectionContrast! with { Candidate = TowerProposalPolicies.BenchmarkTieSelectionVersion } },
            _ => p with { Version = TowerProposalComparison.AlliedActionVersion }
        };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(p));
    }

    [Theory]
    [InlineData("candidate-inventory")]
    [InlineData("control-inventory")]
    [InlineData("nomination")]
    [InlineData("heldout")]
    [InlineData("scope")]
    public void Pair_rejects_inventory_panel_or_context_drift(string field)
    {
        var (c, p, _) = Fixture(); var pair = TowerProposalComparison.Bind(p, c, Values()).Pairs[0];
        var b = pair.Candidate; var panels = b.Racing.Panels.ToArray();
        if (field == "nomination") panels[4] = panels[4] with { Seeds = panels[4].Seeds.Reverse().ToArray() };
        pair = field switch {
            "candidate-inventory" => pair with { Candidate = b with { DamageAffinityInventory = c.DamageAffinityInventory } },
            "control-inventory" => pair with { Control = pair.Control with { DamageAffinityInventory = null } },
            "nomination" => pair with { Candidate = b with { Racing = b.Racing with { Panels = panels } } },
            "heldout" => pair with { HeldoutSeeds = panels[5].Seeds.Concat(pair.HeldoutSeeds.Skip(60)).ToArray() },
            _ => pair with { Candidate = b with { Racing = b.Racing with { Scope = b.Racing.Scope with { Id = "changed" } } } }
        };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair, p.Version));
    }

    [Theory]
    [InlineData(62,62,3,3,"LargerFreshEvaluationWarranted")]
    [InlineData(61,100,3,3,"Inconclusive")]
    [InlineData(100,61,3,3,"Inconclusive")]
    [InlineData(100,0,12,0,"Inconclusive")]
    [InlineData(62,62,2,3,"Inconclusive")]
    [InlineData(62,62,3,2,"Inconclusive")]
    [InlineData(-62,100,12,12,"AbandonThisConfiguration")]
    [InlineData(100,-62,12,12,"AbandonThisConfiguration")]
    [InlineData(-61,-61,12,0,"Inconclusive")]
    [InlineData(0,0,0,0,"NoObservedOutputDifferentiation")]
    public void Decision_matches_integer_design_boundaries_without_novelty_override(int method, int benchmark, int differing, int promising, string expected)
        => Assert.Equal(expected, TowerProposalStudy.Decide(Fixture().Plan.Analysis, method/3072d, benchmark/3072d, differing, promising));

    [Fact]
    public void Fresh_value_validation_and_complete_synthetic_exposure_are_versioned()
    {
        var (c, p, history) = Fixture(); var values = Values();
        foreach (var bad in new[] { values[..^1], values.Select((v,i) => i == 0 ? values[1] : v).ToArray(),
            values.Select((v,i) => i == 0 ? c.RootSeed : v).ToArray() })
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Bind(p, c, bad));
        var bytes = new byte[65536];
        for (var i=0;i<16384;i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4,4),200000+i);
        var a=TowerProposalStudy.Classify(bytes,history,p.Version);
        Assert.Equal(values,a.Selected); Assert.Equal(16384,a.Reserved.Count); Assert.Contains(216383,a.Reserved);
    }

    [Fact]
    public async Task Both_preflights_precede_dispatch_and_incomplete_search_cannot_measure_heldout()
    {
        var (c, p, _) = Fixture(); var pair = TowerProposalComparison.Bind(p,c,Values()).Pairs[0];
        var preflights=0; var dispatched=0; var measured=0;
        await Assert.ThrowsAsync<IOException>(() => TowerProposalComparison.ExecutePairAsync(p,pair,
            (_,_) => { if (++preflights==2) throw new IOException("candidate"); },
            (_,_) => { dispatched++; throw new InvalidOperationException(); }));
        Assert.Equal(0,dispatched);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalStudy.Execute(p,c,Values(),
            pair => Task.FromResult(new TowerProposalComparisonSearch(HarnessJson.Hash(p),HarnessJson.Hash(pair),"Incomplete",null!,null)),
            (_,_) => { measured++; throw new InvalidOperationException(); },(_,_) => {},() => new string('a',64),_ => {},default));
        Assert.Equal(0,measured);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(TowerProposalStudy.ResourceV1)]
    public void Placement_requires_v2_envelope_before_input_read(string? envelope)
    {
        var q=new ProposalStudyRequest(TowerProposalComparison.LoadoutPlacementVersion,null!,null!,null!,null!,null!,null!,null!,null!,null!,null!,null!,null!,envelope);
        Assert.Contains("frozen v2",Assert.Throws<InvalidDataException>(() => TowerProposalStudy.ValidateRequest(q,true)).Message);
    }

    [Fact]
    public async Task Full_literal_study_retains_all_catalogues_freezes_24_outputs_and_reconstructs_native_archive()
    {
        using var fixture=new BalanceHarnessProposalStudyTests();
        await fixture.FullNativeFixture(creation:true,placement:true);
    }
}
