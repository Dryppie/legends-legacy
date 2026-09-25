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
public sealed class BalanceHarnessThreeReferenceConfirmationTests(ITestOutputHelper output)
{
    private static string Repository()
    {
        for (var p = Directory.GetCurrentDirectory(); p is not null; p = Path.GetDirectoryName(p))
            if (File.Exists(Path.Combine(p, "Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json"))) return p;
        throw new IOException("Run through the repository test wrapper.");
    }
    private static string Plan => Path.Combine(Repository(), "Balance Harness/Tower-Practical-Three-Reference-Confirmation-Plan.json");
    internal static TowerFixedFamilyDefinition Definition()
    {
        var plan = HarnessJson.Read<JsonElement>(Plan);
        var teams = plan.GetProperty("recipesInFixedExecutionOrder").EnumerateArray().Select(t => new TowerFixedFamily(
            t.GetProperty("role").GetString()!, t.GetProperty("partyId").GetString()!,
            t.TryGetProperty("referenceId", out var r) ? [r.GetString()!] : [], t.GetProperty("scenario").Deserialize<TowerScenario>(HarnessJson.Options)!)).ToArray();
        return new(C.ThreeReferenceVersion, teams, plan.GetProperty("contentHashes").Deserialize<Dictionary<string,string>>(HarnessJson.Options)!,
            plan.GetProperty("settingsHash").GetString()!, HarnessJson.Hash(ExecutionIdentity.Current()), [-987]);
    }
    private static TowerFixedFamilyStudy Study(Func<int,int,BattleOutcome> outcome)
    {
        var d = Definition(); var f = new TowerFixedFamilyFreeze(d.Version, new string('a',64), new string('b',64), d);
        return new(d.Version, f, d.Teams.Select((t,i) => new TowerDiagnosticCell(t.PartyId,
            Enumerable.Range(0,6500).Select(n => new TowerBalanceTrial(n,outcome(i,n))).ToArray())).ToArray());
    }
    [Fact]
    public void Frozen_family_and_56_transports_match_exact_recipes_and_common_panel()
    {
        var d = Definition(); Assert.Equal(C.ThreeReferencePlanHash, HarnessJson.FileHash(Plan));
        Assert.Equal(C.ThreeReferenceTeamsHash, HarnessJson.Hash(d.Teams)); C.ValidateDefinition(d);
        var chunks = C.Chunks(d, Enumerable.Range(0,6500).ToArray()); Assert.Equal(56,chunks.Count);
        foreach (var (team,i) in d.Teams.Select((t,i) => (t,i)))
        {
            var slices = chunks.Where(c => c.TeamOrdinal == i).ToArray();
            Assert.Equal(new[] {1000,1000,1000,1000,1000,1000,500},slices.Select(c => c.Scenario.Seeds.Count));
            Assert.Equal(Enumerable.Range(0,6500),slices.SelectMany(c => c.Scenario.Seeds));
            Assert.All(slices,c => Assert.Equal(HarnessJson.Hash(team.Scenario),HarnessJson.Hash(c.Scenario with { Seeds = [] })));
        }
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(d with { Teams = d.Teams.Reverse().ToArray() }));
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(d with { Version = C.Version }));
        Assert.Throws<InvalidDataException>(() => C.ValidateDefinition(d with { ExcludedCombatSeeds = Enumerable.Range(0,987001).ToArray() }));
        Assert.Throws<InvalidDataException>(() => C.Chunks(d, Enumerable.Range(0,5500).ToArray()));
    }
    [Theory]
    [InlineData(727,false)] [InlineData(728,true)]
    public void Viability_boundary_is_family38(int wins, bool pass)
    {
        var r = C.Assess(Study((t,n) => t == 0 && n < wins ? BattleOutcome.Victory : BattleOutcome.Draw),new string('c',64));
        Assert.Equal(pass,r.QualifyingPartyIds.Count == 1); Assert.Equal(15,r.Contrasts.Count);
        Assert.Equal(pass ? 4 : 3,r.RecommendedPartyIds.Count); Assert.Equal("NotAssessed",r.BalanceAssessment);
    }
    [Theory]
    [InlineData(324,false)] [InlineData(325,true)]
    public void Every_reference_requires_point_margin_and_positive_paired_lower(int margin, bool pass)
    {
        var r = C.Assess(Study((t,n) => n < (t == 0 ? 1000+margin : 1000) ? BattleOutcome.Victory : BattleOutcome.Defeat),new string('c',64));
        Assert.Equal(pass,r.QualifyingPartyIds.Count == 1);
        var thirdTies = C.Assess(Study((t,n) => n < (t is 0 or 7 ? 1600 : 1000) ? BattleOutcome.Victory : BattleOutcome.Defeat),new string('c',64));
        Assert.Empty(thirdTies.QualifyingPartyIds);
        var noisy = C.Assess(Study((t,n) => t == 0 ? n < 2325 ? BattleOutcome.Victory : BattleOutcome.Defeat
            : n >= 2325 && n < 4575 ? BattleOutcome.Victory : BattleOutcome.Defeat),new string('c',64));
        Assert.All(noisy.Contrasts.Where(c => c.CandidateId == noisy.CandidateIds[0]), c => { Assert.Equal(75,c.Gains-c.Losses); Assert.False(c.Qualifies); });
    }
    [Fact]
    public void All_qualifiers_precede_all_references_and_misaligned_or_faulted_evidence_fails()
    {
        var s = Study((t,n) => n < (t < 5 ? 2500 : 2000) ? BattleOutcome.Victory : BattleOutcome.Defeat);
        var r = C.Assess(s,new string('c',64)); Assert.Equal(5,r.QualifyingPartyIds.Count);
        Assert.Equal(s.Freeze.Definition.Teams.Select(t => t.PartyId),r.RecommendedPartyIds);
        foreach (var bad in new[] {
            s with { Evidence = s.Evidence.Reverse().ToArray() },
            s with { Evidence = s.Evidence.Select((e,i) => i == 7 ? e with { Trials = e.Trials.Reverse().ToArray() } : e).ToArray() },
            s with { Evidence = s.Evidence.Select((e,i) => i == 7 ? e with { Trials = e.Trials.Skip(1).ToArray() } : e).ToArray() },
            Study((_,_) => (BattleOutcome)999) }) Assert.Throws<InvalidDataException>(() => C.Assess(bad,new string('c',64)));
    }
    [Fact]
    public void Literal_entropy_reserves_every_fresh_tail_and_rejects_changed_batch()
    {
        var bytes = new byte[52000]; FixedFamilyFixtureHost.Entropy(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0,4),-987);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4,4),int.MinValue);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8,4),int.MinValue);
        var p = C.Classify(bytes,[-987],new string('a',64),C.ThreeReferenceVersion);
        Assert.Equal(6500,p.Panel.Count); Assert.Equal(12998,p.NewReservations.Count); Assert.Equal(6498,p.Words.Count(w => w.Classification == "ReservedUnused"));
        Assert.Equal(int.MinValue,p.Panel[0]); Assert.Equal("DuplicateBatch",p.Words[2].Classification);
        Assert.Throws<InvalidDataException>(() => C.Classify(bytes[..^1],[-987],new string('a',64),C.ThreeReferenceVersion));
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
        { ThreeReference = new(Plan,Path.Combine(Repository(),"Balance Harness/analysis/audit-three-reference-confirmation.py"),
            HarnessJson.FileHash(Path.Combine(Repository(),"Balance Harness/analysis/audit-three-reference-confirmation.py"))) };
        C.ValidateRequest(q,true); Directory.CreateDirectory(q.OutputRoot);
        HarnessJson.WriteNew(C.P(q,"request.json"),q); File.Copy(Plan,C.P(q,"plan.json")); File.Copy(q.ThreeReference.AuditorPath,C.P(q,"auditor.py"));
        return (q,new(d,new(history,[-987])));
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
            if (draws == 1) { Assert.Equal(52000,new FileInfo(C.P(q,"entropy.bin")).Length); Assert.True(File.Exists(C.P(q,"entropy-complete.json"))); }
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
    public async Task Envelope_launch_and_changed_frozen_auditor_fail_before_allocation()
    {
        var root = Path.Combine(Path.GetTempPath(),"tower-three-confirmation-fixture-"+Guid.NewGuid().ToString("N"));
        try
        {
            var (q,input) = Fixture(root); var now = DateTimeOffset.UtcNow;
            var launch = new IncumbentTieLaunch(q.Version,new string('a',64),now,now.AddSeconds(6000),now.AddSeconds(7200),
                7200,3584L*1048576,6000,3L*1073741824,Environment.ProcessId,"suspended-owned-job-v1");
            C.ValidateThreeReferenceLaunch(q,launch,new string('a',64));
            foreach (var bad in new[] { q with { MaximumSeconds = 7801 }, q with { PriorSeconds = 0 }, q with { PriorBytes = 0 },
                q with { MaximumBytes = 5L*1073741824 }, q with { ThreeReference = null }, q with { Version = C.Version } })
                Assert.Throws<InvalidDataException>(() => C.ValidateRequest(bad));
            Assert.Throws<InvalidDataException>(() => C.ValidateThreeReferenceLaunch(q,launch with { NativeMaximumSeconds = 6001 },new string('a',64)));
            Assert.Throws<InvalidDataException>(() => C.ValidateThreeReferenceLaunch(q,launch with { Deadline = now.AddSeconds(7201) },new string('a',64)));
            await Assert.ThrowsAsync<InvalidDataException>(() => C.Run(q));
            var freeze = C.Freeze(q,input,() => {},default); File.AppendAllText(C.P(q,"auditor.py"),"\n# changed");
            Assert.Throws<InvalidDataException>(() => C.Reserve(q,freeze,input.History.Files,() => {},default,entropy: _ => throw new Exception("Unexpected draw")));
            Assert.False(File.Exists(C.P(q,"entropy-start.json")));
        }
        finally { Directory.Delete(root,true); }
    }
    [Fact]
    public async Task Literal_52000_report_archive_reconstructs_without_combat_and_is_retained_for_independent_python_audit()
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixture entered combat.")).Activate();
        var saved = Environment.GetEnvironmentVariable("THREE_REFERENCE_CONFIRMATION_FIXTURE");
        var root = saved ?? Path.Combine(Path.GetTempPath(),"tower-three-confirmation-fixture-"+Guid.NewGuid().ToString("N"));
        Assert.False(Path.Exists(root)); var clock = Stopwatch.StartNew();
        try
        {
            var (q,input) = Fixture(root); var freeze = C.Freeze(q,input,() => {},default);
            var settings = TowerBundle.ReadSettings(Path.Combine(Repository(),"LL/src/API/API.LL"));
            await FixedFamilyFixtureHost.Prepare(q,freeze,settings,() => {},default);
            var panel = C.Reserve(q,freeze,input.History.Files,() => {},default,entropy: FixedFamilyFixtureHost.Entropy);
            TowerFixedFamilyStudy study;
            using (var attempts = new TowerPracticalSearch.Attempts(C.P(q,"attempts.jsonl"),52000,() => {}))
            {
                study = await FixedFamilyFixtureHost.Study(q,freeze,panel,"complete",attempts.Event,() => {},default);
                Assert.Equal(52000,attempts.Started); Assert.Equal(52000,attempts.Completed);
            }
            var rebuilt = await FixedFamilyFixtureHost.Verify(q,default); Assert.Equal(HarnessJson.Hash(study),HarnessJson.Hash(rebuilt));
            var result = C.Assess(rebuilt,HarnessJson.FileHash(C.P(q,"study/files.json")));
            Assert.Equal(2,result.QualifyingPartyIds.Count); Assert.Equal(5,result.RecommendedPartyIds.Count);
            Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(C.IndependentAudit(q,study,default)));
            HarnessJson.WriteNew(C.P(q,"provisional-result.json"),result);
            output.WriteLine(JsonSerializer.Serialize(new { seconds = clock.Elapsed.TotalSeconds, bytes = TowerBulkCampaign.StorageBytes(root,default),
                literalReports = 52000, actualCombat = 0, productionEntropyDraws = 0, fixture = root,
                limitation = "Synthetic reports/inputs; captured-runtime and combat feasibility remain unestablished." }));
        }
        finally { if (saved is null) Directory.Delete(root,true); }
    }
}
