using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using C = BalanceHarness.TowerReferenceExplorationComparison;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public class BalanceHarnessReferenceExplorationComparisonTests : IDisposable
{
    protected virtual string ComparisonVersion => C.Version;
    private string CandidateVersion => C.Policy(ComparisonVersion).Generator;
    private string SupportDecision => C.Policy(ComparisonVersion).SupportDecision;
    private string NegativeDecision => C.Policy(ComparisonVersion).NegativeDecision;
    protected bool Screened => C.Policy(ComparisonVersion).Screening;
    protected readonly string root = Path.Combine(Path.GetTempPath(), "tower-exploration-comparison-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Comparison fixture entered combat.")).Activate();
    public BalanceHarnessReferenceExplorationComparisonTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);
    private static byte[] Entropy()
    {
        var bytes = new byte[C.EntropyWords * 4];
        for (var i = 0; i < C.EntropyWords; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * 4, 4), i + 1);
        return bytes;
    }
    protected static TowerBossDiscoveryDefinition Template()
    {
        var d = BalanceHarnessThreeReferenceTests.Definition(5);
        return d with { SettingsHash = HarnessJson.Hash(DiagnosticFixtureHost.Settings), ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()),
            MaximumBattles = 4528, Generation = d.Generation with { CandidatesPerArm = 46, Seeds = [] },
            Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.IncumbentTieVersion, SelectionPrimaryReferenceId = d.Starts[0].ReferenceId,
                Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, _ => new BossDiscoverySchedule([], [], [], [])) } };
    }
    private ExplorationRequest Request(TowerBossDiscoveryDefinition d)
    {
        var file = Path.Combine(root, "template.json"); HarnessJson.WriteNew(file, d);
        var history = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(history, new { historical = d.ExcludedCombatSeeds });
        return new(ComparisonVersion, Path.Combine(root, "capture"), Path.Combine(root, "content"), file, HarnessJson.FileHash(file), root,
            Path.Combine(root, "output"), new Dictionary<string, string> { [history] = HarnessJson.FileHash(history) }, new Dictionary<string, string>(), new Dictionary<string, string>(),
            Path.Combine(root, "closeout"), Path.Combine(root, "plan.json"), C.Policy(ComparisonVersion).PlanHash, Path.Combine(root, "auditor.py"), new string('a', 64));
    }

    [Fact]
    public void Protocol_cannot_substitute_the_other_generator_plan_or_allocation()
    {
        var other = ComparisonVersion == C.Version ? C.OffsetVersion : C.Version;
        var d = Template(); var q = Request(d); C.ValidateRequest(q);
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { Version = other }));
        Assert.Throws<InvalidDataException>(() => C.ValidateRequest(q with { PlanHash = C.Policy(other).PlanHash }));
        Assert.Throws<InvalidDataException>(() => C.Policy("unknown"));
        var values = C.Classify(Entropy(), [], ComparisonVersion);
        Assert.Equal(ComparisonVersion, values.Version);
        Assert.Equal(CandidateVersion, C.Bind(d, values.Selected, 0, true, ComparisonVersion).Generation.PolicyVersion);
        var now = DateTimeOffset.UtcNow;
        var launch = new ExplorationLaunch(ComparisonVersion, new string('b', 64), now, now.AddSeconds(C.NativeSeconds),
            now.AddSeconds(C.ExecutionSeconds), C.ExecutionSeconds, C.ExecutionBytes, C.NativeSeconds, C.NativeBytes, 1, "suspended-owned-job-v1");
        C.ValidateLaunch(q, launch, new string('b', 64));
        Assert.Throws<InvalidDataException>(() => C.ValidateLaunch(q, launch with { Version = other }, new string('b', 64)));
    }

    [Fact]
    public void Allocation_pairs_both_generators_and_reserves_every_fresh_tail_value()
    {
        var bytes = Entropy(); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), 1);
        var a = C.Classify(bytes, [3], ComparisonVersion); Assert.Equal(1, a.Duplicates); Assert.Equal(1, a.HistoricalCollisions);
        Assert.Equal(16382, a.Reserved.Count); Assert.Equal(C.AssignedCount(ComparisonVersion), a.Selected.Count);
        var d = Template() with { ExcludedCombatSeeds = [3] };
        for (var i = 0; i < 12; i++)
        {
            var baseline = C.Bind(d, a.Selected, i, false, ComparisonVersion); var candidate = C.Bind(d, a.Selected, i, true, ComparisonVersion);
            Assert.Equal(Json(baseline), Json(candidate with { Generation = candidate.Generation with { PolicyVersion = baseline.Generation.PolicyVersion },
                Stages = candidate.Stages with { Schedules = baseline.Stages.Schedules } }));
            var bs = baseline.Stages.Schedules.Single().Value; var cs = candidate.Stages.Schedules.Single().Value;
            Assert.Equal(a.Selected.Skip(i * C.Policy(ComparisonVersion).ValuesPerRestart + 1).Take(8), bs.Discovery);
            Assert.Equal(bs.Discovery.Take(Screened ? 4 : 8), cs.Discovery);
            Assert.Equal(bs.Selection, cs.Selection); Assert.Equal(bs.Confirmation, cs.Confirmation);
            Assert.Equal(a.Selected.Skip(C.SearchCount(ComparisonVersion) + i * 1000).Take(1000), bs.Confirmation);
            if (Screened)
            {
                var screen = C.ScreeningPanel(a.Selected, i, ComparisonVersion);
                Assert.Equal(8, screen.Length);
                Assert.Empty(screen.Intersect(TowerPracticalSearch.Reserved(baseline)));
                Assert.Empty(screen.Intersect(TowerPracticalSearch.Reserved(candidate)));
                Assert.Equal(588, C.SearchCount(ComparisonVersion)); Assert.Equal(12588, a.Selected.Count);
            }
            Assert.Equal(d.Stages.SelectionPrimaryReferenceId, candidate.Stages.SelectionPrimaryReferenceId);
            Assert.Equal(4528, TowerBossDiscovery.Validate(candidate).Total + (Screened ? 184 : 0));
        }
        Assert.Throws<InvalidDataException>(() => C.Classify(bytes[..^4], [], ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Classify(bytes, [2, 1], ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Bind(d, a.Selected, 12, false, ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Bind(d, a.Selected.Select(_ => 1).ToArray(), 0, true, ComparisonVersion));
    }

    private static ExplorationPair[] Pairs(int k, int[] nets, int size = 4) => Enumerable.Range(1, 12).Select(i => {
        var n = nets.ElementAtOrDefault(i - 1);
        return new ExplorationPair(i, "a", i <= k ? "b" : "a", i > k, 500, 500+n, Math.Max(0,n), Math.Max(0,-n), n/1000d, size);
    }).ToArray();

    [Theory]
    [InlineData(0, 0, "NoSelectedOutputDifferences")]
    [InlineData(6, 100, "DoNotPromoteReferenceExploration")]
    [InlineData(12, 49, "DoNotPromoteReferenceExploration")]
    [InlineData(12, 50, "SupportsReferenceExplorationForFrozenOutputs")]
    [InlineData(12, -50, "DoNotPromoteReferenceExploration")]
    public void Endpoint_keeps_all_roots_integer_gate_replication_and_physical_cost(int k, int gain, string decision)
    {
        var rows = Pairs(k, Enumerable.Repeat(gain, k).ToArray()); var result = C.Summarize(rows, 551408, ComparisonVersion);
        Assert.Equal(decision == "SupportsReferenceExplorationForFrozenOutputs" ? SupportDecision : decision == "DoNotPromoteReferenceExploration" ? NegativeDecision : decision, result.Decision); Assert.Equal(12000, result.Denominator); Assert.Equal(60672, result.Fights);
        Assert.Equal(k*gain/12000d, result.MeanDifference);
        Assert.Equal(k*1000d*(k*1000-1)/((4294967296d-551408-C.SearchCount(ComparisonVersion))*12000), result.Depletion);
        Assert.Throws<InvalidDataException>(() => C.Summarize(rows.Reverse().ToArray(), 551408, ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Summarize(rows, C.MaximumHistory+1, ComparisonVersion));
        Assert.Throws<InvalidDataException>(() => C.Summarize(rows.Select((r,i) => i == 0 ? r with { Gains = 1001 } : r).ToArray(), 1, ComparisonVersion));
    }

    [Fact]
    public void Exact_seven_positive_and_600_net_boundaries()
    {
        var rows = Pairs(7, [86,86,86,86,86,86,84]);
        Assert.Equal(SupportDecision, C.Summarize(rows, 1, ComparisonVersion).Decision);
        rows[6] = rows[6] with { CandidateWins = 583, Gains = 83, Difference = .083 };
        Assert.Equal(NegativeDecision, C.Summarize(rows, 1, ComparisonVersion).Decision);
        Assert.Equal(48672, C.Summarize(Pairs(0, [], 3), 1, ComparisonVersion).Fights);
        Assert.Equal(72672, C.Summarize(Pairs(12, [], 5), 1, ComparisonVersion).Fights);
    }

    [Theory]
    [InlineData("selector")] [InlineData("generator")] [InlineData("offset-generator")] [InlineData("primary")] [InlineData("panel")] [InlineData("controls")] [InlineData("cap")]
    public void Changed_contract_fails_before_execution(string change)
    {
        var d = Template(); d = change switch {
            "selector" => d with { Stages = d.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.ZeroWinVersion } },
            "generator" => d with { Generation = d.Generation with { PolicyVersion = TowerReferenceExploration.Version } },
            "offset-generator" => d with { Generation = d.Generation with { PolicyVersion = TowerReferenceExploration.OffsetVersion } },
            "primary" => d with { Stages = d.Stages with { SelectionPrimaryReferenceId = null } },
            "panel" => d with { Stages = d.Stages with { Schedules = d.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value with { Selection = [1] }) } },
            "controls" => d with { Starts = d.Starts.Take(2).ToArray() },
            _ => d with { MaximumBattles = 4527 }
        };
        Assert.Throws<InvalidDataException>(() => C.ValidateTemplate(d, ComparisonVersion));
    }

    [Fact]
    public void Request_pins_new_plan_cumulative_charge_and_closed_launch_deadline()
    {
        var q = Request(Template()); C.ValidateRequest(q);
        foreach (var bad in new[] { q with { MaximumBytes = C.MaximumBytes+1 }, q with { PriorSeconds = 0 }, q with { PriorBytes = 0 },
            q with { PlanHash = new string('b',64) }, q with { Version = TowerIncumbentTieComparison.Version } })
            Assert.Throws<InvalidDataException>(() => C.ValidateRequest(bad));
        var now = DateTimeOffset.UtcNow;
        var launch = new ExplorationLaunch(ComparisonVersion, new string('b',64), now, now.AddSeconds(C.NativeSeconds), now.AddSeconds(C.ExecutionSeconds),
            C.ExecutionSeconds, C.ExecutionBytes, C.NativeSeconds, C.NativeBytes, 1, "suspended-owned-job-v1");
        C.ValidateLaunch(q, launch, new string('b',64));
        Assert.Throws<InvalidDataException>(() => C.ValidateLaunch(q, launch with { Deadline = now.AddSeconds(C.MaximumSeconds) }, new string('b',64)));
    }

    [Theory]
    [InlineData("pending", false)] [InlineData("entropy-written", true)] [InlineData("before-complete", true)]
    public void Interrupted_allocation_keeps_pending_and_all_exposed_entropy(string boundary, bool exposed)
    {
        var d = Template(); var q = Request(d); Directory.CreateDirectory(q.OutputRoot); using var stop = new CancellationTokenSource(); var draws = 0;
        Assert.Throws<OperationCanceledException>(() => C.Reserve(q, new(d, new(q.RequiredHistory, d.ExcludedCombatSeeds.ToArray())), () => { }, stop.Token,
            b => { draws++; Entropy().CopyTo(b, 0); }, name => { if (name == boundary) stop.Cancel(); }));
        Assert.Equal(exposed ? 1 : 0, draws);
        Assert.Equal("Pending", HarnessJson.Read<JsonElement>(C.P(q,"history-input.json")).GetProperty("reservationState").GetString());
        Assert.Equal(exposed, File.Exists(C.P(q,"entropy.bin")));
        if (exposed) Assert.Equal(Entropy(), File.ReadAllBytes(C.P(q,"entropy.bin")));
    }

    [Fact]
    public void Entropy_exhaustion_is_terminal_without_refill()
    {
        var d = Template(); var q = Request(d); Directory.CreateDirectory(q.OutputRoot); var draws = 0;
        Assert.Throws<ExplorationIncompleteException>(() => C.Reserve(q, new(d, new(q.RequiredHistory, d.ExcludedCombatSeeds.ToArray())), () => { }, default, _ => draws++));
        Assert.Equal(1, draws); Assert.Equal(new[] { 0 }, HarnessJson.Read<ExplorationReservation>(C.P(q,"allocation.json")).Reserved);
    }

    protected async Task<(ExplorationStudy Study, ExplorationRequest Request, ExplorationReservation Allocation, BossGenerationMechanics Mechanics)> Execute(
        bool different = true, bool archive = false, string? fail = null)
    {
        var d = Template(); var q = Request(d); Directory.CreateDirectory(q.OutputRoot);
        var allocation = C.Reserve(q, new(d, new(q.RequiredHistory, d.ExcludedCombatSeeds.ToArray())), () => { }, default, b => Entropy().CopyTo(b, 0));
        File.Copy(q.TemplatePath, C.P(q,"template.json")); var output = C.P(q,"study"); Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles" }) Directory.CreateDirectory(Path.Combine(output, folder));
        var scope = new LoadoutScope(ComparisonVersion, DiagnosticFixtureHost.Settings, ExecutionIdentity.Current(), d.ContentHashes, "gzip-json-v1");
        var mechanics = F.Mechanics(TowerBossImprovement.Inputs(C.Bind(d, allocation.Selected, 0, false, ComparisonVersion)));
        HarnessJson.WriteNew(Path.Combine(output,"scope.json"),scope); HarnessJson.WriteNew(Path.Combine(output,"generation-mechanics.json"),mechanics);
        var ordinal = 0; ExplorationFreeze? frozen = null; var attemptLines = new StringBuilder(); var started = 0; var completed = 0;
        void Attempt(bool complete)
        {
            if (complete) { Assert.Equal(completed+1,started); completed++; } else { Assert.Equal(started,completed); started++; }
            attemptLines.Append($"{{\"kind\":\"{(complete ? "Completed" : "Started")}\",\"ordinal\":{(complete ? completed : started)}}}\n");
        }
        string Prefix() => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(attemptLines.ToString())));
        var study = await C.Execute(d, allocation, mechanics, (arm, stage, scenario, seed, token) => {
            if (fail == "search" && ordinal == 5) throw new IOException("Injected search failure");
            if (fail == "confirmation" && stage == "confirmation") throw new IOException("Injected confirmation failure");
            if (stage == "screening")
            {
                Assert.True(File.Exists(Path.Combine(output, arm.Split('/')[0]+"-screening-freeze.json")));
                if (fail == "screening-battle") throw new IOException("Injected screening failure");
            }
            if (Screened && arm.Contains("candidate", StringComparison.Ordinal) && stage == "selection")
                Assert.True(File.Exists(Path.Combine(output, arm.Split('/')[0]+"-screening.json")));
            var party = TowerPartySelection.Choice("fixture",scenario.Party.ToDictionary(p=>p.PartySlot,p=>p.Build.EssenceIds));
            var anchor = d.Starts.Any(s=>s.Party.Id == party.Id); var primary = party.Id == d.Starts[0].Party.Id;
            var index = scenario.Seeds.ToList().IndexOf(seed); var candidate = arm.Contains("candidate",StringComparison.Ordinal);
            if (stage == "confirmation") { Assert.NotNull(frozen); Assert.Equal(12,frozen.Searches.Count); Assert.True(ordinal >= C.SearchFights); }
            var won = stage switch { "discovery" => index < (anchor ? 3 : 5),
                "screening" => index < (anchor ? 3 : 6),
                "selection" => index < (different && candidate ? anchor ? 20 : 28 : primary ? 28 : 20),
                _ => index < (!anchor ? 580 : primary ? 500 : 400) };
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome,outcome,"Literal exploration fixture",1,1,[new SimpleCombatEntity("f","f","",10,0)],[],[],new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1,scenario.Id,seed,1,JsonSerializer.SerializeToElement(new { fixture=true }),summary,null),won,50m,1);
            var trial = new LoadoutTrial($"trial-{++ordinal:D6}",stage,HarnessJson.Hash(scenario),seed,new string('a',64),HarnessJson.Hash(new { arm, scenario, seed }));
            if (archive)
            {
                var path = Path.Combine(output,"recipes",trial.Recipe+".json"); if (!File.Exists(path)) HarnessJson.WriteNew(path,scenario);
                TowerLoadoutArchive.WriteBattle(output,trial.Id,report,scope.ReportStorage);
                File.AppendAllText(Path.Combine(output,"trials.jsonl"),JsonSerializer.Serialize(trial,new JsonSerializerOptions(HarnessJson.Options) { WriteIndented=false })+"\n");
            }
            return Task.FromResult((trial,report));
        },(name,value)=> {
            if (fail == "freeze" && name == "outputs-freeze.json") throw new IOException("Injected freeze failure");
            if (fail == "screening-freeze" && name.EndsWith("-screening-freeze.json", StringComparison.Ordinal)
                || fail == "screening-nominees" && name.EndsWith("-screening.json", StringComparison.Ordinal))
                throw new IOException("Injected screening checkpoint failure");
            HarnessJson.WriteNew(Path.Combine(output,name),value); if (value is ExplorationFreeze f) frozen=f;
        },Attempt,Prefix,default);
        Assert.Equal(different ? 60672 : 48672,ordinal); File.WriteAllText(C.P(q,"attempts.jsonl"),attemptLines.ToString());
        if (archive) TowerSelectionDiagnostic.Seal(output);
        return (study,q,allocation,mechanics);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Independent_searches_freeze_all_outputs_and_measure_each_control_once_per_pair(bool different)
    {
        var (study,q,a,_) = await Execute(different); var d = HarnessJson.Read<TowerBossDiscoveryDefinition>(q.TemplatePath);
        var result = C.Assess(d,study,a); Assert.Equal(different ? 12 : 0,result.ActiveRestarts);
        Assert.Equal(different ? SupportDecision : "NoSelectedOutputDifferences",result.Decision);
        Assert.Equal(24,result.DescriptiveViews.Count);
        Assert.All(result.DescriptiveViews,v=> { Assert.Equal(10,v.IntervalFamily); Assert.Equal(3,v.Contrasts.Count); Assert.Equal("DescriptiveOnlyNoTeamAdoption",v.Interpretation); });
        Assert.All(study.Freeze.Families,f=> { Assert.Equal(different ? 4 : 3,f.Members.Count); Assert.Equal(3,f.Members.Sum(m=>m.ReferenceIds.Count)); });
        foreach (var search in study.Freeze.Searches)
        {
            Assert.Equal(TowerSuppliedCompositionSearch.ThreeReferenceVersion,search.Baseline.Discovery!.Version);
            Assert.Equal(CandidateVersion,search.Candidate.Discovery!.Version);
            Assert.Equal(search.Baseline.Output.Selector,search.Candidate.Output.Selector);
            Assert.Equal(46,search.Candidate.Discovery!.Arms.Single().Evaluations.Count);
            Assert.Null(search.Baseline.Screening);
            Assert.Equal(C.Pipeline(ComparisonVersion, false), search.Baseline.Output.Pipeline);
            Assert.Equal(C.Pipeline(ComparisonVersion, true), search.Candidate.Output.Pipeline);
            if (!Screened)
            {
                Assert.DoesNotContain("\"screening\"", Json(search));
                Assert.DoesNotContain("\"pipeline\"", Json(search));
            }
            else
            {
                Assert.Equal(23, search.Candidate.Screening!.Freeze.Candidates.Count);
                Assert.Equal(search.Candidate.Screening.Nominees.Select(p => p.Id), search.Candidate.Selection.Select(r => r.Id));
            }
        }
        var first = study.Freeze.Searches[0]; var bound = C.Bind(d,a.Selected,0,false,ComparisonVersion); var panel = C.Panel(a.Selected,0,ComparisonVersion);
        ExplorationOutput Output(ExplorationOutput original, PartyChoice party)
        {
            var scenario = TowerBossDiscovery.Scenario(bound,bound.Contexts.Single().Id,party,[]);
            return original with { Finalist=original.Finalist with { Party=party }, Scenario=scenario, RecipeHash=TowerBossDiscovery.RecipeHash(scenario.Party) };
        }
        ExplorationFamily Family(PartyChoice left, PartyChoice right) => C.Family(bound,first with {
            Baseline=first.Baseline with { Output=Output(first.Baseline.Output,left) },
            Candidate=first.Candidate with { Output=Output(first.Candidate.Output,right) } },panel,ComparisonVersion);
        var challengers=first.Baseline.Discovery!.DiscoveryShortlist.Where(p=>!d.Starts.Any(s=>s.Party.Id==p.Id)).ToArray();
        Assert.Equal(3,Family(d.Starts[0].Party,d.Starts[1].Party).Members.Count);
        Assert.Equal(4,Family(challengers[0],challengers[0]).Members.Count);
        Assert.Equal(5,Family(challengers[0],challengers[1]).Members.Count);
        Assert.Throws<InvalidDataException>(()=>C.Family(bound,first with { Candidate=first.Candidate with {
            Output=first.Candidate.Output with { RecipeHash=first.Baseline.Output.RecipeHash, Selector="changed" } } },panel,ComparisonVersion));
    }

    [Theory] [InlineData("search")] [InlineData("freeze")] [InlineData("confirmation")]
    public async Task Interrupted_work_cannot_release_an_incomplete_family_or_publish_a_study(string boundary)
    {
        await Assert.ThrowsAnyAsync<Exception>(()=>Execute(fail:boundary));
        Assert.False(File.Exists(Path.Combine(root,"output/study/study.json")));
        Assert.Equal(boundary == "confirmation",File.Exists(Path.Combine(root,"output/study/outputs-freeze.json")));
    }

    [Fact]
    public async Task Archive_reconstructs_both_trajectories_and_rejects_resealed_trace_and_union_changes()
    {
        var (study,q,a,mechanics)=await Execute(archive:true); var d=HarnessJson.Read<TowerBossDiscoveryDefinition>(q.TemplatePath);
        Task<ExplorationResult> Verify()=>C.VerifyStudy(q,default,(scope,trials)=> {
            var index=0;
            return(mechanics,(arm,_,scenario,seed,_)=> {
                var trial=trials[index++]; C.Match(C.P(q,"study"),"recipes/"+trial.Recipe+".json",scenario);
                Assert.Equal(HarnessJson.Hash(new { arm,scenario,seed }),trial.CacheKey);
                return Task.FromResult((trial,TowerLoadoutArchive.ReadBattle(C.P(q,"study"),trial.Id,scope.ReportStorage)));
            });
        });
        Assert.Equal(Json(C.Assess(d,study,a)),Json(await Verify()));
        if (Environment.GetEnvironmentVariable("BALANCE_HARNESS_EXPLORATION_FIXTURE_EXPORT") is { Length:>0 } export)
        {
            export = Path.Combine(export, ComparisonVersion);
            Assert.False(Path.Exists(export)); Directory.CreateDirectory(export);
            foreach(var file in Directory.EnumerateFiles(q.OutputRoot,"*",SearchOption.AllDirectories))
            { var target=Path.Combine(export,Path.GetRelativePath(q.OutputRoot,file)); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file,target); }
            HarnessJson.WriteNew(Path.Combine(export,"expected-result.json"),C.Assess(d,study,a));
        }
        var path=C.P(q,"study/pair-01-candidate-discovery.json"); var original=File.ReadAllText(path);
        var discovery=HarnessJson.Read<BossGenerationResult>(path); var arm=discovery.Arms.Single();
        var bad=discovery with { Arms=[arm with { Proposals=arm.Proposals.Select(p=>p.Supplied?.Exploration is not {} t ? p : p with {
            Supplied=p.Supplied with { Exploration=t with { Radius=1 } } }).ToArray() }] };
        if (Screened) bad = discovery with { Arms = [arm with { Seed = arm.Seed+1 }] };
        File.WriteAllText(path,Json(bad)); File.Delete(C.P(q,"study/files.json")); TowerSelectionDiagnostic.Seal(C.P(q,"study"));
        await Assert.ThrowsAsync<InvalidDataException>(Verify);
        File.WriteAllText(path,original); File.Delete(C.P(q,"study/files.json")); TowerSelectionDiagnostic.Seal(C.P(q,"study"));
        var family=study.Freeze.Families[0];
        Assert.Throws<InvalidDataException>(()=>C.Assess(d,study with { Freeze=study.Freeze with {
            Families=study.Freeze.Families.Select((f,i)=>i==0 ? family with { Members=family.Members.Skip(1).ToArray() } : f).ToArray() } },a));
        var lines=File.ReadAllLines(C.P(q,"attempts.jsonl")); (lines[0],lines[1])=(lines[1],lines[0]);
        File.WriteAllText(C.P(q,"attempts.jsonl"),string.Join('\n',lines)+"\n"); await Assert.ThrowsAsync<InvalidDataException>(Verify);
    }
}

public sealed class BalanceHarnessReferenceExplorationOffsetComparisonTests : BalanceHarnessReferenceExplorationComparisonTests
{
    protected override string ComparisonVersion => C.OffsetVersion;
}
