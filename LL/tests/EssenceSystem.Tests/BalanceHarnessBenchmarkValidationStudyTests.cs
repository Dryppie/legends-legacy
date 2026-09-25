using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessBenchmarkValidationStudyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "validation-study-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Validation study entered combat/preparation.")).Activate();
    public BalanceHarnessBenchmarkValidationStudyTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static int[] Values() => Enumerable.Range(200000, 4668).ToArray();
    private static byte[] Entropy()
    {
        var bytes = new byte[65536];
        for (var i = 0; i < 16384; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4,4),200000+i);
        return bytes;
    }

    [Fact]
    public void Versioned_design_binds_all_4668_values_with_shared_prefix_and_disjoint_validation()
    {
        var (context, plan, _) = BalanceHarnessProposalStudyTests.Fixture(validation: true);
        Assert.Equal(S.ValidationVersion, plan.Version);
        Assert.Equal((4668, 21888, 10800, 6442450944L), (plan.RequiredFreshValues, plan.MaximumFights, plan.MaximumSeconds, plan.MaximumBytes));
        Assert.Equal(new TowerProposalSelectionContrast(TowerProposalPolicies.BenchmarkTieSelectionVersion, TowerBenchmarkValidation.Version), plan.SelectionContrast);
        Assert.Equal(133, plan.ValidationProtocol!.ValuesPerPair);
        var binding = TowerProposalComparison.Bind(plan, context, Values());
        Assert.Equal(HarnessJson.Hash(binding), HarnessJson.Hash(TowerProposalComparison.Bind(plan, context, Values())));
        Assert.Equal(12, binding.Pairs.Count);
        foreach (var pair in binding.Pairs)
        {
            var a = pair.Control.Racing; var b = pair.Candidate.Racing;
            TowerProposalComparison.ValidatePair(pair, S.ValidationVersion);
            Assert.Equal(TowerProposalPolicies.BenchmarkTieRacingVersion, pair.Control.Version);
            Assert.Equal(TowerProposalPolicies.BenchmarkValidationRacingVersion, pair.Candidate.Version);
            Assert.Equal(HarnessJson.Hash(a.Scope), HarnessJson.Hash(b.Scope));
            Assert.Equal(HarnessJson.Hash(a.Panels.Take(4)), HarnessJson.Hash(b.Panels.Take(4)));
            Assert.Equal(a.Panels[4].Seeds.Take(16), b.Panels[4].Seeds);
            Assert.Empty(a.Panels.SelectMany(p => p.Seeds).Intersect(b.Panels[5].Seeds));
            Assert.Empty(pair.HeldoutSeeds.Intersect(a.Panels.Concat(b.Panels).SelectMany(p => p.Seeds)));
            Assert.All(pair.HeldoutSeeds, seed => Assert.Contains(seed, a.Scope.ExcludedCombatSeeds));
            Assert.Equal(Values().Skip(1596+(pair.Root-1)*256).Take(256), pair.HeldoutSeeds);
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair, S.SelectorVersion));
        }
        var used = binding.Pairs.SelectMany(p => p.Control.Racing.Panels.Concat(p.Candidate.Racing.Panels)
            .SelectMany(s => s.Seeds).Append(p.Control.Racing.RootSeed).Concat(p.HeldoutSeeds)).Distinct().Order();
        Assert.Equal(Values(), used);
        foreach (var bad in new[] { plan with { RequiredFreshValues = 3948 }, plan with { ValidationProtocol = null },
            plan with { SelectionContrast = plan.SelectionContrast! with { Control = TowerBossStudyPolicy.IncumbentTieVersion } },
            plan with { ValidationProtocol = plan.ValidationProtocol with { NominationValues = 17 } },
            plan with { Pairing = "same-proposal-root-and-search-panels-separate-charges" }, plan with { Version = S.SelectorVersion },
            plan with { MaximumFights = 21889 }, plan with { MaximumBytes = plan.MaximumBytes+1 } })
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(bad));
        foreach (var values in new[] { Values()[..3948], Values().Select((v,i) => i==1 ? 200000 : v).ToArray(),
            Values().Select((v,i) => i==0 ? context.RootSeed : v).ToArray() })
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Bind(plan, context, values));
        var old = TowerProposalComparison.CreateSelectorPlan(context, plan.Candidate);
        Assert.DoesNotContain("validationProtocol", JsonSerializer.Serialize(old, HarnessJson.Options));
        Assert.Equal(3948, old.RequiredFreshValues);
    }

    [Theory]
    [InlineData("nomination")]
    [InlineData("validation")]
    [InlineData("heldout")]
    public void Pair_rejects_legal_but_unpaired_or_cross_arm_reused_values(string fault)
    {
        var (context, plan, _) = BalanceHarnessProposalStudyTests.Fixture(validation: true);
        var pair = TowerProposalComparison.Bind(plan, context, Values()).Pairs[0];
        var panels = pair.Candidate.Racing.Panels.ToArray();
        if (fault == "nomination") panels[4] = panels[4] with { Seeds = panels[4].Seeds.Reverse().ToArray() };
        if (fault == "validation") panels[5] = panels[5] with { Seeds = panels[5].Seeds.Select((s,i) => i==0 ? pair.Control.Racing.Panels[4].Seeds[20] : s).ToArray() };
        if (fault == "heldout") pair = pair with { HeldoutSeeds = pair.HeldoutSeeds.Select((s,i) => i==0 ? panels[5].Seeds[0] : s).ToArray() };
        pair = pair with { Candidate = pair.Candidate with { Racing = pair.Candidate.Racing with { Panels = panels } } };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair, S.ValidationVersion));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("entropy-written")]
    [InlineData("before-complete")]
    public void Interrupted_new_allocation_retains_exposure_without_refill(string boundary)
    {
        var (context,plan,history) = BalanceHarnessProposalStudyTests.Fixture(validation: true);
        var inputs = new ProposalStudyInputs(plan,context,new(new(),10),history,new(new Dictionary<string,string>(),history));
        using var cts = new CancellationTokenSource(); var draws = 0;
        Assert.Throws<OperationCanceledException>(() => S.Reserve(root,inputs,()=>{},()=>{},cts.Token,
            b => { draws++; Entropy().CopyTo(b,0); }, s => { if (s==boundary) cts.Cancel(); }));
        Assert.Equal(boundary == "pending" ? 0 : 1, draws);
        Assert.Equal("Pending", HarnessJson.Read<JsonElement>(Path.Combine(root,"history-input.json")).GetProperty("reservationState").GetString());
        if (draws == 1) Assert.Equal(Entropy(), File.ReadAllBytes(Path.Combine(root,"entropy.bin")));
        Assert.Throws<IOException>(() => S.Reserve(root,inputs,()=>{},()=>{},default,_=>throw new Exception("refill")));
    }

    [Fact]
    public void Reservation_and_launch_require_new_version_and_larger_selected_prefix()
    {
        var (context,plan,history) = BalanceHarnessProposalStudyTests.Fixture(validation: true);
        var inputs = new ProposalStudyInputs(plan,context,new(new(),10),history,new(new Dictionary<string,string>(),history));
        var allocation = S.Reserve(root,inputs,()=>{},()=>{},default,b=>Entropy().CopyTo(b,0));
        Assert.Equal(S.ValidationVersion, allocation.Version); Assert.Equal(4668, allocation.Selected.Count); Assert.Equal(16384,allocation.Reserved.Count);
        Assert.Equal(HarnessJson.Hash(allocation),HarnessJson.Hash(S.VerifyReservation(root,inputs)));
        Assert.Throws<InvalidDataException>(() => S.VerifyReservation(root,inputs with { Plan = TowerProposalComparison.CreateSelectorPlan(context,plan.Candidate) }));
        var launch = new ExplorationLaunch(S.ValidationVersion,new string('a',64),DateTimeOffset.UnixEpoch,DateTimeOffset.UnixEpoch.AddSeconds(9000),
            DateTimeOffset.UnixEpoch.AddSeconds(10800),10800,6442450944,9000,5905580032,1,"suspended-owned-job-v1");
        S.ValidateLaunch(launch,launch.RequestFileHash,S.ResourceV2,S.ValidationVersion);
        Assert.Throws<InvalidDataException>(() => S.ValidateLaunch(launch,launch.RequestFileHash,S.ResourceV2,S.SelectorVersion));
    }

    [Fact]
    public async Task Shared_nomination_results_are_checked_and_failure_prevents_heldout()
    {
        var (context,plan,_) = BalanceHarnessProposalStudyTests.Fixture(validation: true);
        var pair = TowerProposalComparison.Bind(plan,context,Values()).Pairs[0];
        Task<TowerProposalRacingReport> Run(TowerProposalRacingPlan p) => TowerProposalPolicies.RunAsync(p,(r,_) => Task.FromResult(
            new TowerPanelOutcome(HarnessJson.Hash(r),$"trial-{r.Ordinal:D6}",r.Seed,BattleOutcome.Victory,0,100,1)));
        var a = await Run(pair.Control); var b = await Run(pair.Candidate);
        TowerProposalComparison.ValidateValidationTrajectories(a,b);
        var panels = b.Evaluation.Panels.ToArray(); var rows = panels[4].Observations.ToArray();
        rows[0] = rows[0] with { Outcome = rows[0].Outcome with { GuardianHealth = 1 } };
        panels[4] = panels[4] with { Observations = rows }; var altered = b with { Evaluation = b.Evaluation with { Panels = panels } };
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidateValidationTrajectories(a,altered));
        var calls = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => S.Execute(plan,context,Values(),
            p => Task.FromResult(new TowerProposalComparisonSearch(HarnessJson.Hash(plan),HarnessJson.Hash(p),"Complete",a,altered)),
            (_,_)=>{calls++;throw new Exception();},(_,_)=>{},()=>new string('a',64),_=>{},default));
        Assert.Equal(0,calls);
        await Assert.ThrowsAsync<InvalidDataException>(() => S.Execute(plan,context,Values(),
            p => TowerProposalComparison.ExecutePairAsync(plan,p,(_,_)=>{},(_,q)=>TowerProposalPolicies.RunAsync(q,(_,_)=>throw new IOException("fail"))),
            (_,_)=>{calls++;throw new Exception();},(_,_)=>{},()=>new string('a',64),_=>{},default));
        Assert.Equal(0,calls);
    }

    [Fact]
    public async Task Complete_validation_study_fixture_reconstructs_all_roots_and_exports_evidence()
    {
        using var fixture = new BalanceHarnessProposalStudyTests();
        await fixture.FullNativeFixture(creation:true,validation:true);
    }

    [Fact]
    public async Task Public_plan_command_creates_only_the_new_design()
    {
        var (context,plan,_) = BalanceHarnessProposalStudyTests.Fixture(validation:true);
        var input = Path.Combine(root,"input.json"); var output = Path.Combine(root,"plan.json");
        HarnessJson.WriteNew(input,new TowerProposalExportRequest(TowerProposalPolicies.CreationExportVersion,context,[TowerProposalComparison.Control(),plan.Candidate]));
        Assert.Equal(0,await BalanceHarness.Program.Main(["tower-benchmark-validation-comparison-plan",input,output]));
        Assert.Equal(HarnessJson.Hash(plan),HarnessJson.Hash(HarnessJson.Read<TowerProposalComparisonPlan>(output)));
        Assert.Equal(0,await BalanceHarness.Program.Main(["tower-proposal-comparison-check",output]));
        Assert.Throws<IOException>(() => TowerProposalComparison.Command(["tower-benchmark-validation-comparison-plan",input,output]));
    }
}
