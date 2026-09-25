using BalanceHarness;
using BalanceHarness.ProcessFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProposalAuditCostProbeTests
{
    [Fact]
    public void Workload_covers_all_arms_with_only_historical_values_and_retains_full_exclusions()
    {
        var (context,design,old) = BalanceHarnessProposalStudyTests.Fixture();
        var history = old.Concat(Enumerable.Range(-5000,3000)).Distinct().Order().ToArray();
        var plans = ProposalAuditCostProbe.Workloads(context,design,history);
        Assert.Equal(24,plans.Length);
        var used = new HashSet<int>(); var legacy = TowerProposalStudy.LegacySeeds(context);
        for (var i = 0; i < plans.Length; i += 2)
        {
            TowerProposalPolicies.Validate(plans[i]); TowerProposalPolicies.Validate(plans[i+1]);
            Assert.Equal(HarnessJson.Hash(plans[i].Racing),HarnessJson.Hash(plans[i+1].Racing));
            Assert.Equal(HarnessJson.Hash(context.DamageAffinityInventory),HarnessJson.Hash(plans[i].DamageAffinityInventory));
            var racing = plans[i].Racing; var values = racing.Panels.SelectMany(p => p.Seeds).Append(racing.RootSeed).ToArray();
            Assert.Equal(73,values.Distinct().Count());
            foreach (var value in values) { Assert.Contains(value,history); Assert.True(used.Add(value)); }
            Assert.Equal(history.Except(legacy).Except(values),racing.Scope.ExcludedCombatSeeds);
            Assert.Equal(HarnessJson.Hash(context.Scope.References),HarnessJson.Hash(racing.Scope.References));
        }
        Assert.Equal(876,used.Count);
        // A resource workload deliberately reuses history; it is no scientific allocation.
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Bind(design,context,
            history.Take(3948).ToArray()));
    }

    [Fact]
    public void Workload_refuses_missing_or_unsorted_historical_inputs()
    {
        var (context,design,history) = BalanceHarnessProposalStudyTests.Fixture();
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.Workloads(context,design,[]));
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.Workloads(context,design,[2,1]));
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.Workloads(context,design,[1,1]));
    }

    [Fact]
    public async Task Literal_native_proposal_replay_is_deterministic_without_entering_combat()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Probe entered combat.")).Activate();
        var (context,design,history) = BalanceHarnessProposalStudyTests.Fixture();
        var plan = ProposalAuditCostProbe.Workloads(context,design,history.Concat(Enumerable.Range(-5000,3000)).Distinct().Order().ToArray())[1];
        var report = await TowerProposalPolicies.RunAsync(plan,(q,_) => Task.FromResult(ProposalAuditCostProbe.Literal(q)));
        var replay = await TowerProposalPolicies.ReconstructAsync(plan,report);
        Assert.Equal(528,report.Evaluation.ChargedEvaluations);
        Assert.Equal(HarnessJson.Hash(report),HarnessJson.Hash(replay));
        Assert.All(report.Evaluation.Panels.SelectMany(p => p.Observations), o => Assert.Equal(100,o.Outcome.GuardianHealth));
    }

    [Fact]
    public void Historical_bridge_allows_only_harness_identity_change_with_matching_settings_content_and_platform()
    {
        var (context,_,_) = BalanceHarnessProposalStudyTests.Fixture(); var execution = ExecutionIdentity.Current();
        var old = execution with { AssemblyHashes = execution.AssemblyHashes.ToDictionary(p => p.Key,p => p.Key == "BalanceHarness" ? new string('a',64) : p.Value) };
        var scope = new LoadoutScope("historical",new(new(),10),old,context.Scope.ContentHashes,"gzip-json-v1");
        ProposalAuditCostProbe.ValidateSourceCompatibility(execution,scope,context);
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.ValidateSourceCompatibility(execution with { Runtime = "different" },scope,context));
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.ValidateSourceCompatibility(execution,scope with { Execution = old with { AssemblyHashes = old.AssemblyHashes.Where(p => p.Key != "Domain").ToDictionary() } },context));
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.ValidateSourceCompatibility(execution,scope with { Settings = new(new(),11) },context));
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.ValidateSourceCompatibility(execution,scope with { ContentHashes = new Dictionary<string,string>() },context));
    }

    [Fact]
    public void Native_probe_rejects_scientific_output_and_changed_allowance()
    {
        var now = DateTimeOffset.UtcNow; var repo = Path.GetFullPath(".");
        var request = new ProposalAuditProbeRequest(ProposalAuditCostProbe.Version,"native",repo,
            Path.Combine(repo,"TestResults",ProposalAuditCostProbe.OutputName),now,now.AddSeconds(480),1,1,new string('a',64));
        ProposalAuditCostProbe.Validate(request);
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.Validate(request with { Output = Path.Combine(repo,"TestResults/balance/probe") }));
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.Validate(request with { Deadline = now.AddSeconds(600) }));
        Assert.Throws<InvalidDataException>(() => ProposalAuditCostProbe.Validate(request with { Mode = "literal" }));
    }
}
