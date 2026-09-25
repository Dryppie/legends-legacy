using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using Xunit.Abstractions;
using C = BalanceHarness.TowerFixedFamilyConfirmation;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityCreationRecognitionTests(ITestOutputHelper output)
{
    private static string Repository()
    {
        for (var p = Directory.GetCurrentDirectory(); p is not null; p = Path.GetDirectoryName(p))
            if (File.Exists(Path.Combine(p, "Balance Harness/Tower-Affinity-Creation-Recognition-Plan.json"))) return p;
        throw new IOException("Run through the repository test wrapper.");
    }
    private static string Plan => Path.Combine(Repository(), "Balance Harness/Tower-Affinity-Creation-Recognition-Plan.json");
    internal static TowerFixedFamilyDefinition Definition()
    {
        var p = C.RecognitionPlan(Plan,C.AffinityRecognitionVersion); var runtime = p.GetProperty("capturedRuntime");
        return new(C.AffinityRecognitionVersion,C.RecognitionTeams(p),runtime.GetProperty("contentHashes").Deserialize<Dictionary<string,string>>(HarnessJson.Options)!,
            runtime.GetProperty("settingsHash").GetString()!,HarnessJson.Hash(ExecutionIdentity.Current()),[-987]);
    }
    private static TowerFixedFamilyStudy Study()
    {
        var d = Definition(); var f = new TowerFixedFamilyFreeze(d.Version,new string('a',64),new string('b',64),d);
        return new(d.Version,f,d.Teams.Select((t,i) => new TowerDiagnosticCell(C.CellId(d,i),
            Enumerable.Range(0,256).Select(n => new TowerBalanceTrial(i/9*256+n,
                n < new[] {70,80,100,140,140,90,90,180,180}[i%9] ? BattleOutcome.Victory : BattleOutcome.Draw)).ToArray())).ToArray());
    }
    [Fact]
    public void Root_panels_and_recipes_are_exact_disjoint_and_never_deduplicated()
    {
        var d = Definition(); C.ValidateDefinition(d); Assert.Equal(C.AffinityRecognitionTeamsHash,HarnessJson.Hash(d.Teams));
        var chunks = C.Chunks(d,Enumerable.Range(0,3072).ToArray()); Assert.Equal(108,chunks.Count);
        Assert.Equal(108,chunks.Select(c => c.PartyId).Distinct().Count());
        foreach (var chunk in chunks) Assert.Equal(Enumerable.Range(chunk.TeamOrdinal/9*256,256),chunk.Scenario.Seeds);
        Assert.Throws<InvalidDataException>(() => C.Chunks(d,Enumerable.Range(0,3072).Select(n => n%256).ToArray()));
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(d with { Teams = d.Teams.Reverse().ToArray() }));
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(d with { Version = C.ThreeReferenceVersion }));
        Assert.Throws<InvalidDataException>(() => C.Assess(Study(),new string('c',64)));
    }
    [Fact]
    public void Weighted_diagnosis_preserves_nulls_and_cannot_promote_a_team()
    {
        var s = Study(); var r = C.AssessRecognition(s,Plan,new string('c',64));
        Assert.Equal(108,r.Rates.Count); Assert.Equal(216,r.Contrasts.Count); Assert.Equal(36,r.Strata.Count); Assert.Equal(132,r.Unmeasured.Count);
        Assert.All(r.Unmeasured,u => Assert.Null(u.IndependentOutcome)); Assert.False(r.PolicyDefaultsChanged);
        Assert.Equal("CompleteDiagnosticOnly",r.Decision);
        Assert.Equal(C.AffinityRecognitionVersion,r.Version);
        Assert.All(r.Populations,p => Assert.Equal((2*40+2*(-10)+6.5*2*80)/(256d*17),p.EstimatedCandidateMeanGain,12));
        Assert.All(r.Strata.Where(s => s.Stratum == "lower"),s => Assert.Equal(6.5,s.InclusionWeight));
        foreach (var bad in new[] {
            s with { Evidence = s.Evidence.Reverse().ToArray() },
            s with { Evidence = s.Evidence.Select((e,i) => i == 9 ? e with { Trials = s.Evidence[0].Trials } : e).ToArray() },
            s with { Evidence = s.Evidence.Select((e,i) => i == 7 ? e with { Trials = e.Trials.Skip(1).ToArray() } : e).ToArray() },
            s with { Evidence = s.Evidence.Select(e => e with { Trials = e.Trials.Select(t => t with { Outcome = (BattleOutcome)999 }).ToArray() }).ToArray() }
        }) Assert.Throws<InvalidDataException>(() => C.AssessRecognition(bad,Plan,new string('c',64)));
    }
    [Fact]
    public void Full_batch_reserves_unused_tail_and_short_batch_never_becomes_complete()
    {
        var bytes = new byte[24576]; FixedFamilyFixtureHost.Entropy(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0,4),-987);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4,4),int.MinValue);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8,4),int.MinValue);
        var p = C.Classify(bytes,[-987],new string('a',64),C.AffinityRecognitionVersion);
        Assert.Equal(3072,p.Panel.Count); Assert.Equal(6142,p.NewReservations.Count); Assert.Equal(3070,p.Words.Count(w => w.Classification == "ReservedUnused"));
        Assert.Single(C.Classify(new byte[24576],[-987],new string('a',64),C.AffinityRecognitionVersion).Panel);
    }
    private static (TowerFixedFamilyRequest Q,TowerFixedFamilyInputs Input) Fixture(string root)
    {
        var d = Definition(); Directory.CreateDirectory(root); var content = Path.Combine(root,"content"); Directory.CreateDirectory(content);
        var prior = Path.Combine(root,"prior-seed-ledger.json"); HarnessJson.WriteNew(prior,new { historical = d.ExcludedCombatSeeds });
        var source = Path.Combine(root,"definition.json"); HarnessJson.WriteNew(source,d);
        var history = new Dictionary<string,string> { [prior] = HarnessJson.FileHash(prior) };
        var q = new TowerFixedFamilyRequest(d.Version,content,source,HarnessJson.FileHash(source),root,Path.Combine(root,"result"),history,
            7800,4L*1073741824,new Dictionary<string,TowerDiagnosticPhaseLimit>(),600,512L*1048576,
            [new("Literal admission fixture",600,512L*1048576,prior,HarnessJson.FileHash(prior))])
        { Recognition = new(Plan,Path.Combine(Repository(),"Balance Harness/analysis/audit-affinity-creation-recognition.py"),
            HarnessJson.FileHash(Path.Combine(Repository(),"Balance Harness/analysis/audit-affinity-creation-recognition.py"))) };
        C.ValidateRequest(q,true); Directory.CreateDirectory(q.OutputRoot);
        HarnessJson.WriteNew(C.P(q,"request.json"),q); File.Copy(Plan,C.P(q,"plan.json")); File.Copy(q.Recognition.AuditorPath,C.P(q,"auditor.py"));
        return (q,new(d,new(history,[-987])));
    }
    [Fact]
    public void Both_profiles_reject_the_other_frozen_plan_and_family()
    {
        var legacy = Path.Combine(Repository(),"Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json");
        Assert.Throws<InvalidDataException>(() => C.RecognitionPlan(Plan));
        Assert.Throws<InvalidDataException>(() => C.RecognitionPlan(legacy,C.AffinityRecognitionVersion));
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(Definition() with { Version = C.RecognitionVersion }));
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(BalanceHarnessFrozenPoolRecognitionTests.Definition() with { Version = C.AffinityRecognitionVersion }));
        Assert.Throws<InvalidDataException>(() => C.RecognitionPlanPin("unknown"));
        Assert.Throws<InvalidDataException>(() => C.AssessRecognition(Study(),legacy,new string('c',64)));
    }
    [Fact]
    public async Task Command_profiles_reject_cross_version_requests_before_side_effects()
    {
        var root = Path.Combine(Path.GetTempPath(),"tower-affinity-command-fixture-"+Guid.NewGuid().ToString("N"));
        try
        {
            var (q,_) = Fixture(root);
            foreach (var expected in new[] {C.RecognitionVersion,C.AffinityRecognitionVersion})
            {
                var other = expected == C.RecognitionVersion ? C.AffinityRecognitionVersion : C.RecognitionVersion;
                File.WriteAllText(C.P(q,"request.json"),JsonSerializer.Serialize(q with { Version = other },HarnessJson.Options));
                var before = Directory.GetFiles(root,"*",SearchOption.AllDirectories).ToDictionary(p => p,HarnessJson.FileHash);
                foreach (var action in new[] {"check","run","audit","publication-check","verify"})
                    await Assert.ThrowsAsync<InvalidDataException>(() => C.RecognitionCommand(
                        [C.RecognitionCommandPrefix(expected)+"-"+action,action == "check" ? C.P(q,"request.json") : q.OutputRoot],default,expected));
                Assert.Equal(before,Directory.GetFiles(root,"*",SearchOption.AllDirectories).ToDictionary(p => p,HarnessJson.FileHash));
            }
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact]
    public void Wrong_plan_is_rejected_during_inspection_before_entropy()
    {
        var root = Path.Combine(Path.GetTempPath(),"tower-affinity-plan-fixture-"+Guid.NewGuid().ToString("N"));
        try
        {
            var (q,_) = Fixture(root);
            var bad = q with { Recognition = q.Recognition! with { PlanPath = Path.Combine(Repository(),"Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json") } };
            Assert.Throws<InvalidDataException>(() => C.Inspect(bad,default));
            Assert.False(File.Exists(C.P(q,"entropy-intent.json")));
            Assert.False(File.Exists(C.P(q,"attempts.jsonl")));
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact]
    public void Explicit_failed_admission_charge_cannot_reset_or_expand_the_scientific_allowance()
    {
        var root = Path.Combine(Path.GetTempPath(),"tower-affinity-recognition-envelope-"+Guid.NewGuid().ToString("N"));
        try
        {
            var (q,_) = Fixture(root); var receipt = Path.Combine(root,"failed-admission.json"); File.WriteAllText(receipt,"{}");
            var revised = q with { MaximumSeconds = 8400, MaximumBytes = 4608L*1048576, PriorSeconds = 1200, PriorBytes = 1024L*1048576,
                PriorCharges = q.PriorCharges.Append(new("Failed preceding admission",600,512L*1048576,receipt,HarnessJson.FileHash(receipt))).ToArray() };
            C.ValidateRequest(revised,true);
            foreach (var bad in new[] { revised with { MaximumSeconds = 8401 }, revised with { PriorSeconds = 600 },
                revised with { MaximumBytes = 5L*1073741824 }, revised with { PriorCharges = q.PriorCharges },
                revised with { PriorSeconds = 1800, PriorBytes = 1536L*1048576, MaximumSeconds = 9000 } })
                Assert.Throws<InvalidDataException>(() => C.ValidateRequest(bad));
        }
        finally { Directory.Delete(root,true); }
    }
    [Theory]
    [InlineData("entropy-pending")] [InlineData("entropy-start")] [InlineData("entropy-drawn")] [InlineData("entropy-written")]
    [InlineData("entropy-complete")] [InlineData("confirmation-binding")] [InlineData("confirmation-ledger")]
    [InlineData("confirmation-reserved")] [InlineData("chunks-bound")]
    public void Interrupted_transaction_never_refills_or_reuses(string boundary)
    {
        var root = Path.Combine(Path.GetTempPath(),"tower-three-confirmation-fixture-"+Guid.NewGuid().ToString("N"));
        try
        {
            var (q,input) = Fixture(root); var freeze = C.Freeze(q,input,() => {},default); using var stop = new CancellationTokenSource();
            var draws = 0;
            Assert.ThrowsAny<OperationCanceledException>(() => C.Reserve(q,freeze,input.History.Files,stop.Token.ThrowIfCancellationRequested,stop.Token,
                stage => { if (stage == boundary) stop.Cancel(); }, bytes => { draws++; FixedFamilyFixtureHost.Entropy(bytes); }));
            Assert.Equal(boundary is "entropy-pending" or "entropy-start" ? 0 : 1,draws);
            if (draws == 1) { Assert.Equal(24576,new FileInfo(C.P(q,"entropy.bin")).Length); Assert.True(File.Exists(C.P(q,"entropy-complete.json"))); }
            Assert.Throws<IOException>(() => C.Reserve(q,freeze,input.History.Files,() => {},default,entropy: _ => throw new Exception("Redraw")));
            Assert.False(File.Exists(C.P(q,"attempts.jsonl")));
            if (boundary == "entropy-start")
            {
                var manifest = Path.Combine(root,"recovery-manifest.json");
                HarnessJson.WriteNew(manifest,Directory.GetFiles(q.OutputRoot).ToDictionary(Path.GetFileName,HarnessJson.FileHash));
                foreach (var version in new[] { TowerPracticalReservationRecovery.Version,TowerPracticalReservationRecovery.AllocationVersion })
                    Assert.ThrowsAny<Exception>(() => TowerPracticalReservationRecovery.Recover(new(version,q.OutputRoot,manifest,
                        HarnessJson.FileHash(manifest),Path.Combine(root,"recovery-receipt.json"),10)));
                Assert.False(File.Exists(Path.Combine(root,"recovery-receipt.json")));
                Assert.Throws<InvalidDataException>(() => TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot,Path.Combine(root,"next"),input.History.Files,[-987],default));
            }
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact]
    public async Task Full_27648_report_archive_reconstructs_without_combat_and_is_retained_for_python()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture entered combat.")).Activate();
        var saved = Environment.GetEnvironmentVariable("AFFINITY_CREATION_RECOGNITION_FIXTURE");
        var root = saved ?? Path.Combine(Path.GetTempPath(),"tower-affinity-recognition-fixture-"+Guid.NewGuid().ToString("N"));
        Assert.False(Path.Exists(root)); var clock = Stopwatch.StartNew();
        try
        {
            var (q,input) = Fixture(root); var freeze = C.Freeze(q,input,() => {},default);
            var settings = TowerBundle.ReadSettings(Path.Combine(Repository(),"LL/src/API/API.LL"));
            await FixedFamilyFixtureHost.Prepare(q,freeze,settings,() => {},default);
            var panel = C.Reserve(q,freeze,input.History.Files,() => {},default,entropy: FixedFamilyFixtureHost.Entropy);
            TowerFixedFamilyStudy study;
            using (var attempts = new TowerPracticalSearch.Attempts(C.P(q,"attempts.jsonl"),27648,() => {}))
            {
                study = await FixedFamilyFixtureHost.Study(q,freeze,panel,"complete",attempts.Event,() => {},default);
                Assert.Equal(27648,attempts.Started); Assert.Equal(27648,attempts.Completed);
            }
            var rebuilt = await FixedFamilyFixtureHost.Verify(q,default); Assert.Equal(HarnessJson.Hash(study),HarnessJson.Hash(rebuilt));
            var result = C.AssessRecognition(rebuilt,Plan,HarnessJson.FileHash(C.P(q,"study/files.json")));
            HarnessJson.WriteNew(C.P(q,"provisional-result.json"),result);
            HarnessJson.WriteNew(C.P(q,"proposed-teams.json"),C.RecognitionExport(study,result));
            File.WriteAllText(C.P(q,"proposed-recognition.md"),C.RecognitionMarkdown(result));
            output.WriteLine(JsonSerializer.Serialize(new { seconds = clock.Elapsed.TotalSeconds, bytes = TowerBulkCampaign.StorageBytes(root,default),
                literalReports = 27648, actualCombat = 0, productionEntropyDraws = 0, fixture = root }));
            // Native reconstruction rejects a root identity mutation even with a re-sealed study.
            var path = C.P(q,"study/study.json"); var bytes = File.ReadAllBytes(path);
            try {
                File.WriteAllText(path,JsonSerializer.Serialize(study with { Evidence = study.Evidence.Reverse().ToArray() },HarnessJson.Options));
                File.Delete(C.P(q,"study/files.json")); C.Seal(C.P(q,"study"));
                await Assert.ThrowsAsync<InvalidDataException>(() => FixedFamilyFixtureHost.Verify(q,default));
            } finally { File.WriteAllBytes(path,bytes); File.Delete(C.P(q,"study/files.json")); C.Seal(C.P(q,"study")); }
            Assert.Equal(result.ArchiveHash,HarnessJson.FileHash(C.P(q,"study/files.json")));
        }
        finally { if (saved is null) Directory.Delete(root,true); }
    }
}
