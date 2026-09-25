using BalanceHarness;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityCreationNativeTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-creation-native-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Creation qualification entered combat.")).Activate();
    public BalanceHarnessAffinityCreationNativeTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    internal static TowerProposalRacingPlan Plan()
    {
        var (racing, inventory) = BalanceHarnessDamageAffinityTests.Fixture(allSlots: true);
        var ids = TowerDamageSourceAffinities.Create(inventory).Affinities
            .Where(a => new[] { "e10", "e11" }.Contains(a.ProducerEssenceId) && new[] { "e10", "e11" }.Contains(a.ModifierEssenceId))
            .Select(a => a.Id);
        return new(TowerProposalPolicies.CreationRacingVersion, racing, TowerProposalPolicies.BenchmarkAffinityCreation(ids), inventory);
    }
    private static TowerProposalContext Context(TowerProposalRacingPlan p) => new(p.Racing.Scope, p.Racing.Mechanics,
        p.Racing.BenchmarkReferenceId, p.Racing.RootSeed, p.DamageAffinityInventory);
    private static int[] Values() => Enumerable.Range(200000, TowerProposalComparison.RequiredFreshValues).ToArray();

    [Fact]
    public void Creation_design_has_a_separate_identity_and_retains_all_paired_resource_rules()
    {
        var p = Plan(); var context = Context(p);
        var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        Assert.Equal(TowerProposalComparison.CreationVersion, design.Version);
        Assert.Equal((12, 256, 528, 3948, 21888, 10800, 6L * 1073741824),
            (design.Roots, design.HeldoutSamples, design.SearchFightsPerArm, design.RequiredFreshValues,
                design.MaximumFights, design.MaximumSeconds, design.MaximumBytes));
        var bound = TowerProposalComparison.Bind(design, context, Values());
        Assert.Equal(design.Version, bound.Version); Assert.Equal(12, bound.Pairs.Count);
        Assert.Equal(HarnessJson.Hash(bound), HarnessJson.Hash(TowerProposalComparison.Bind(design, context, Values())));
        Assert.Equal(3948, bound.Pairs.SelectMany(p => p.Control.Racing.Panels.SelectMany(x => x.Seeds)
            .Append(p.Control.Racing.RootSeed).Concat(p.HeldoutSeeds)).Distinct().Count());
        foreach (var pair in bound.Pairs)
        {
            Assert.Equal(TowerProposalPolicies.DamageRacingVersion, pair.Control.Version);
            Assert.Equal(TowerProposalPolicies.CreationRacingVersion, pair.Candidate.Version);
            Assert.Equal(HarnessJson.Hash(pair.Control.Racing), HarnessJson.Hash(pair.Candidate.Racing));
            Assert.All(pair.HeldoutSeeds, seed => Assert.Contains(seed, pair.Control.Racing.Scope.ExcludedCombatSeeds));
            TowerProposalComparison.ValidatePair(pair);
        }
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(design with { Version = TowerProposalComparison.Version }));
        var (oldRacing, oldInventory) = BalanceHarnessDamageAffinityTests.Fixture();
        var oldContext = new TowerProposalContext(oldRacing.Scope, oldRacing.Mechanics, oldRacing.BenchmarkReferenceId, oldRacing.RootSeed, oldInventory);
        var old = TowerProposalComparison.CreatePlan(oldContext,
            TowerProposalPolicies.BenchmarkDamageEdits(TowerDamageSourceAffinities.Create(oldInventory).Affinities.Select(a => a.Id)));
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(old with { Version = TowerProposalComparison.CreationVersion }));
        TowerProposalStudy.ValidateDesign(old);
        TowerProposalStudy.ValidateDesign(design);
        Assert.Throws<InvalidDataException>(() => TowerProposalStudy.ValidateDesign(design with { Version = TowerProposalStudy.Version }));
    }

    [Theory]
    [InlineData("old-racing")]
    [InlineData("old-policy")]
    [InlineData("missing-inventory")]
    [InlineData("unknown-route")]
    public async Task Cross_version_or_unbound_creation_never_dispatches(string fault)
    {
        var p = Plan();
        p = fault switch {
            "old-racing" => p with { Version = TowerProposalPolicies.DamageRacingVersion },
            "old-policy" => p with { Policy = TowerProposalComparison.Control() },
            "missing-inventory" => p with { DamageAffinityInventory = null },
            _ => p with { Policy = p.Policy with { CreatedDamageAffinityIds = [new string('f', 64)] } }
        };
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalPolicies.RunAsync(p,
            (_, _) => throw new InvalidOperationException("No evaluator dispatch")));
    }

    [Theory]
    [InlineData("selector")]
    [InlineData("bound")]
    [InlineData("preservation")]
    [InlineData("schedule")]
    [InlineData("history")]
    [InlineData("heldout")]
    public void Frozen_design_and_paired_allocation_reject_drift(string fault)
    {
        var p = Plan(); var context = Context(p); var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        var values = Values();
        if (fault == "bound")
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(design with { MaximumFights = design.MaximumFights + 1 }));
        else if (fault is "preservation" or "schedule")
        {
            var changed = fault == "preservation" ? p.Policy with { PreservedDamageAffinityIds = p.Policy.CreatedDamageAffinityIds }
                : p.Policy with { FirstWave = TowerProposalPolicies.BenchmarkSmallEdits().FirstWave };
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Validate(design with { Candidate = changed }));
        }
        else if (fault == "history")
        {
            values[0] = context.RootSeed;
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Bind(design, context, values));
        }
        else if (fault == "selector")
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Bind(design, context with { Scope = context.Scope with {
                Stages = context.Scope.Stages with { SelectionPrimaryReferenceId = context.BenchmarkReferenceId } } }, values));
        else
        {
            var pair = TowerProposalComparison.Bind(design, context, values).Pairs[0];
            Assert.Throws<InvalidDataException>(() => TowerProposalComparison.ValidatePair(pair with {
                HeldoutSeeds = pair.HeldoutSeeds.Select((seed, i) => i == 0 ? pair.Control.Racing.RootSeed : seed).ToArray() }));
        }
    }

    [Fact]
    public void Structural_feasibility_rejects_unavailable_copies_without_previewing_roots()
    {
        var p = Plan(); var context = Context(p);
        var copies = context.Scope.AllowedEssences.ToDictionary(e => e.Id, _ => context.Scope.RequiredPartySize);
        copies["e10"] = 0;
        context = context with { Scope = context.Scope with { OwnedCopies = copies } };
        var error = Assert.Throws<InvalidDataException>(() => TowerProposalComparison.CreatePlan(context, p.Policy));
        Assert.Contains("17 distinct legal", error.Message);
    }

    [Fact]
    public async Task Both_native_archives_preflight_before_any_trial_and_failed_control_stops_pair()
    {
        var p = Plan(); var context = Context(p); var design = TowerProposalComparison.CreatePlan(context, p.Policy);
        var pair = TowerProposalComparison.Bind(design, context, Values()).Pairs[0];
        var events = new List<string>();
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalComparison.ExecutePairAsync(design, pair,
            (arm, _) => { events.Add("check-" + arm); if (arm == "candidate") throw new InvalidDataException("changed creation archive"); },
            (_, _) => throw new InvalidOperationException("Must not dispatch")));
        Assert.Equal(new[] { "check-control", "check-candidate" }, events); events.Clear();
        var result = await TowerProposalComparison.ExecutePairAsync(design, pair,
            (arm, _) => events.Add("check-" + arm), async (arm, plan) => {
                events.Add("run-" + arm);
                return await TowerProposalPolicies.RunAsync(plan, (_, _) => throw new IOException("literal failure"));
            });
        Assert.Equal(new[] { "check-control", "check-candidate", "run-control" }, events);
        Assert.Equal("Incomplete", result.Status); Assert.Null(result.Candidate);
        Assert.Equal(1, result.Control.Evaluation.ChargedEvaluations);
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalStudy.Execute(design with { Version = TowerProposalStudy.Version }, context, Values(),
            _ => throw new InvalidOperationException("Old owner must not search"),
            (_, _) => throw new InvalidOperationException("Old owner must not measure"),
            (_, _) => throw new InvalidOperationException("Old owner must not write"), () => "unused", _ => { }, default));
    }

    [Fact]
    public async Task Insufficient_generation_and_cancelled_dispatch_keep_no_selected_output()
    {
        var p = Plan(); var copies = p.Racing.Scope.AllowedEssences.ToDictionary(e => e.Id, _ => p.Racing.Scope.RequiredPartySize);
        copies["e10"] = 0;
        var exhausted = await TowerProposalPolicies.RunAsync(p with { Racing = p.Racing with { Scope = p.Racing.Scope with { OwnedCopies = copies } } },
            (_, _) => throw new InvalidOperationException("No trials for incomplete generation"));
        Assert.Equal("Incomplete", exhausted.Evaluation.Status); Assert.Equal(0, exhausted.Evaluation.ChargedEvaluations);
        Assert.Null(exhausted.Evaluation.RawSelectedId); Assert.Equal(128, exhausted.Batches[0].Proposals.Count);
        using var cancellation = new CancellationTokenSource(); var calls = 0;
        var cancelled = await TowerProposalPolicies.RunAsync(p, (r, token) => {
            if (++calls == 3) { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
            return Task.FromResult(new TowerPanelOutcome(HarnessJson.Hash(r), "literal-" + r.Ordinal, r.Seed, BattleOutcome.Victory, 0, 50, 1));
        }, cancellation.Token);
        Assert.Equal("Cancelled", cancelled.Evaluation.Status); Assert.Equal(3, cancelled.Evaluation.ChargedEvaluations);
        Assert.Null(cancelled.Evaluation.RawSelectedId); Assert.Equal(2, cancelled.Evaluation.Panels[0].Observations.Count);
    }

    [Fact]
    public void Creation_runtime_inventory_and_archive_identity_are_bound()
    {
        var p = Plan(); var settings = new TowerSettings(new(), 10); var execution = ExecutionIdentity.Current();
        p = p with { Racing = p.Racing with { Scope = p.Racing.Scope with {
            SettingsHash = HarnessJson.Hash(settings), ExecutionHash = HarnessJson.Hash(execution) } } };
        var scope = new LoadoutScope(TowerProposalRacingNative.ArchiveAlgorithm(p), settings, execution, p.Racing.Scope.ContentHashes, "gzip-json-v1");
        TowerProposalRacingNative.ValidateBinding(p, scope, 528, 0, 0);
        Assert.Throws<InvalidDataException>(() => TowerProposalRacingNative.ValidateBinding(p with { Version = TowerProposalPolicies.DamageRacingVersion }, scope, 528, 0, 0));
        Assert.Throws<InvalidDataException>(() => TowerProposalRacingNative.ValidateBinding(p, scope with { Execution = execution with {
            AssemblyHashes = new Dictionary<string, string>() } }, 528, 0, 0));
        var inventory = p.DamageAffinityInventory!;
        var changed = inventory with { Nodes = inventory.Nodes.Select((n, i) => i == 0 ? n with { Signals = ["invented"] } : n).ToArray() };
        TowerProposalPolicies.Validate(p with { DamageAffinityInventory = changed });
        Assert.Throws<InvalidDataException>(() => TowerProposalRacingNative.ValidateInventory(p with { DamageAffinityInventory = changed }, inventory));
    }

    [Fact]
    public async Task Public_plan_command_chooses_creation_explicitly_and_has_no_run_command()
    {
        var p = Plan(); var request = new TowerProposalExportRequest(TowerProposalPolicies.CreationExportVersion,
            Context(p), [TowerProposalComparison.Control(), TowerProposalPolicies.BenchmarkDamageEdits(p.Policy.CreatedDamageAffinityIds!), p.Policy]);
        var input = Path.Combine(root, "request.json"); HarnessJson.WriteNew(input, request);
        var output = Path.Combine(root, "creation.json");
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-proposal-comparison-plan", input, output]));
        Assert.Equal(TowerProposalComparison.CreationVersion, HarnessJson.Read<TowerProposalComparisonPlan>(output).Version);
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-proposal-comparison-check", output]));
        Assert.Throws<IOException>(() => TowerProposalComparison.Command(["tower-proposal-comparison-plan", input, output]));
        var racing = Path.Combine(root, "racing.json"); HarnessJson.WriteNew(racing, p);
        Assert.Equal(0, await BalanceHarness.Program.Main(["tower-proposal-racing-check", racing]));
        await Assert.ThrowsAsync<InvalidDataException>(() => TowerProposalRacingNative.Command(["tower-proposal-racing-run", racing]));
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Command(["tower-proposal-comparison-run", output]));
        var ambiguous = Path.Combine(root, "ambiguous.json");
        HarnessJson.WriteNew(ambiguous, request with { Policies = [p.Policy, p.Policy with { Name = "other-creation" }] });
        Assert.Throws<InvalidDataException>(() => TowerProposalComparison.Command(["tower-proposal-comparison-plan", ambiguous, Path.Combine(root, "never.json")]));
        Assert.False(File.Exists(Path.Combine(root, "never.json")));
    }
}
