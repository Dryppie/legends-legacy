using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using C = BalanceHarness.TowerFixedFamilyConfirmation;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessAffinityNeighborhoodRecognitionTests
{
    private static string Repository()
    {
        for (var p = Directory.GetCurrentDirectory(); p is not null; p = Path.GetDirectoryName(p))
            if (File.Exists(Path.Combine(p,"Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json"))) return p;
        throw new IOException("Run through the repository test wrapper.");
    }
    private static string Plan => Path.Combine(Repository(),"Balance Harness/Tower-Affinity-Neighborhood-Recognition-Plan.json");
    private static JsonElement PlanValue => C.RecognitionPlan(Plan,C.NeighborhoodRecognitionVersion);
    private static TowerFixedFamilyDefinition Definition()
    {
        var plan=PlanValue; var runtime=plan.GetProperty("sourceContract").GetProperty("capturedRuntime");
        return new(C.NeighborhoodRecognitionVersion,C.RecognitionTeams(plan),
            runtime.GetProperty("contentHashes").Deserialize<Dictionary<string,string>>(HarnessJson.Options)!,
            runtime.GetProperty("settingsHash").GetString()!,HarnessJson.Hash(ExecutionIdentity.Current()),[-987]);
    }
    private static TowerFixedFamilyStudy Study(Func<int,int> wins)
    {
        var d=Definition(); var f=new TowerFixedFamilyFreeze(d.Version,new string('a',64),new string('b',64),d);
        return new(d.Version,f,d.Teams.Select((t,i)=>new TowerDiagnosticCell(t.PartyId,Enumerable.Range(0,2048)
            .Select(n=>new TowerBalanceTrial(n,n<wins(i)?BattleOutcome.Victory:BattleOutcome.Draw)).ToArray())).ToArray());
    }
    private static TowerRecognitionResult Assess(TowerFixedFamilyStudy study) => C.AssessRecognition(study,Plan,new string('c',64));

    [Fact]
    public void Exact_plan_family_and_three_transport_slices_share_one_panel()
    {
        var d=Definition(); C.ValidateDefinition(d);
        Assert.Equal(C.NeighborhoodTeamsHash,HarnessJson.Hash(d.Teams));
        var chunks=C.Chunks(d,Enumerable.Range(0,2048).ToArray());
        Assert.Equal(132,chunks.Count); Assert.Equal(90112,chunks.Sum(c=>c.Scenario.Seeds.Count));
        foreach (var group in chunks.GroupBy(c=>c.TeamOrdinal))
        {
            Assert.Equal(new[]{1000,1000,48},group.Select(c=>c.Scenario.Seeds.Count));
            Assert.Equal(Enumerable.Range(0,2048),group.SelectMany(c=>c.Scenario.Seeds));
            Assert.All(group,c=>Assert.Equal(d.Teams[c.TeamOrdinal].PartyId,c.PartyId));
        }
        Assert.Throws<InvalidDataException>(()=>C.Chunks(d,Enumerable.Range(0,3072).ToArray()));
        Assert.Throws<InvalidDataException>(()=>C.Chunks(d,Enumerable.Range(0,2048).Select(n=>n%1024).ToArray()));
        Assert.Throws<InvalidDataException>(()=>C.Chunks(d,Enumerable.Range(-987,2048).ToArray()));
    }

    [Theory]
    [InlineData("unresolved","RetireUnresolvedAtBudget")]
    [InlineData("below","RetireBelowPracticalThreshold")]
    [InlineData("advance","FreshConfirmationWarranted")]
    [InlineData("reference-only","RetireUnresolvedAtBudget")]
    public void Terminal_decisions_match_frozen_rule_without_promoting(string mode,string status)
    {
        int Wins(int i)=> mode switch { "below"=>i==2?2048:0,"advance"=>i is 3 or 43?237:0,"reference-only"=>i==0?2048:0,_=>0 };
        var s=Study(Wins); var r=Assess(s); var n=r.Neighborhood!;
        Assert.Equal(status,r.Decision); Assert.Equal(status,n.Status); Assert.Equal(46,n.Endpoints.Count); Assert.Equal(44,n.Wins.Count);
        Assert.False(n.Promoted); Assert.Equal(0,n.AdditionalSamples); Assert.False(r.PolicyDefaultsChanged);
        Assert.Equal(0,r.ApproximateWilsonFamily); Assert.Empty(r.Rates); Assert.Empty(r.Contrasts); Assert.Null(r.PairedPool);
        Assert.Equal(mode=="advance"?2:0,n.EligiblePartyIds.Count);
        if (mode=="advance") Assert.Equal(new[]{s.Freeze.Definition.Teams[3].PartyId,s.Freeze.Definition.Teams[43].PartyId}.Order(StringComparer.Ordinal),n.EligiblePartyIds);
        var markdown=C.RecognitionMarkdown(r);
        Assert.Contains(status,markdown);
        Assert.Equal(44,markdown.Split('\n').Count(line=>line.Contains(" / 2048 |",StringComparison.Ordinal)));
        foreach(var row in n.Wins)
            Assert.Contains(System.FormattableString.Invariant($"| {row.Key} | {row.Value} / 2048 | {100d*row.Value/2048:F6}% |"),markdown);
        var saved=Environment.GetEnvironmentVariable("NEIGHBORHOOD_RECOGNITION_TEST_OUTPUT");
        if (saved is not null)
        {
            Directory.CreateDirectory(saved);
            HarnessJson.WriteNew(Path.Combine(saved,mode+".json"),new { study=s,result=r });
        }
    }

    [Fact]
    public void Exact_integer_threshold_and_shared_coefficient_cancellation_are_preserved()
    {
        foreach (var count in new[]{236,237})
        {
            var r=Assess(Study(i=>i==3?count:0)).Neighborhood!;
            Assert.Equal(count==237,r.EligiblePartyIds.Count==1);
            Assert.Equal(Math.Sqrt(2*Math.Log(1840)/2048),r.Endpoints[0].Upper-r.Endpoints[0].Mean,14);
        }
        var plan=PlanValue;
        var index=Array.FindIndex(plan.GetProperty("teams").EnumerateArray().ToArray(),t=>t.GetProperty("membership").GetString()=="shared");
        var n=Assess(Study(i=>i==index?2048:0)).Neighborhood!;
        Assert.Equal(1d/41,n.Endpoints.Single(e=>e.Id=="controlMean").Mean,14);
        Assert.Equal(1d/26,n.Endpoints.Single(e=>e.Id=="candidateMean").Mean,14);
        var delta=n.Endpoints.Single(e=>e.Id=="candidateMinusControlMean");
        Assert.Equal(1d/26-1d/41,delta.Mean,14);
        Assert.Equal((30d/41)*Math.Sqrt(Math.Log(1840)/4096),delta.Upper-delta.Mean,14);
    }

    [Theory]
    [InlineData("order")] [InlineData("missing-team")] [InlineData("missing-trial")]
    [InlineData("seed")] [InlineData("fault")] [InlineData("identity")]
    public void Incomplete_or_misbound_evidence_has_no_decision(string mode)
    {
        var s=Study(_=>0);
        s=mode switch {
            "order"=>s with { Evidence=s.Evidence.Reverse().ToArray() },
            "missing-team"=>s with { Evidence=s.Evidence.Skip(1).ToArray() },
            _=>s with { Evidence=s.Evidence.Select((e,i)=>i!=3?e:mode switch {
                "missing-trial"=>e with { Trials=e.Trials.Skip(1).ToArray() },
                "identity"=>e with { PartyId=s.Evidence[0].PartyId },
                _=>e with { Trials=e.Trials.Select((t,n)=>n!=0?t:mode=="seed"?t with { Seed=2048 }:t with { Outcome=(BattleOutcome)999 }).ToArray() }
            }).ToArray() }
        };
        Assert.Throws<InvalidDataException>(()=>Assess(s));
    }

    [Fact]
    public void Legacy_profiles_cannot_accept_the_new_plan_or_family()
    {
        foreach (var version in new[]{C.Version,C.ThreeReferenceVersion,C.RecognitionVersion,C.AffinityRecognitionVersion,C.PreservationRecognitionVersion})
        {
            Assert.Throws<InvalidDataException>(()=>C.ValidateDefinition(Definition() with { Version=version }));
            if (C.IsRecognition(version)) Assert.Throws<InvalidDataException>(()=>C.RecognitionPlan(Plan,version));
        }
        Assert.Throws<InvalidDataException>(()=>C.Assess(Study(_=>0),new string('c',64)));
        Assert.Equal(37120,C.Policy(C.PreservationRecognitionVersion).Fights);
        Assert.Equal(10800,C.RecognitionLimits(C.PreservationRecognitionVersion).Seconds);
        Assert.Equal(90112,C.Policy(C.NeighborhoodRecognitionVersion).Fights);
        Assert.Equal(2048,C.Policy(C.NeighborhoodRecognitionVersion).PanelValues);
        Assert.Equal(18000,C.RecognitionLimits(C.NeighborhoodRecognitionVersion).Seconds);
        Assert.Throws<InvalidDataException>(()=>C.RecognitionRoot(C.NeighborhoodRecognitionVersion,44));
    }

    [Fact]
    public void One_literal_entropy_batch_reserves_its_complete_unused_tail()
    {
        var bytes=new byte[65536];FixedFamilyFixtureHost.Entropy(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0,4),-987);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4,4),int.MinValue);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8,4),int.MinValue);
        var p=C.Classify(bytes,[-987],new string('a',64),C.NeighborhoodRecognitionVersion);
        Assert.Equal(2048,p.Panel.Count);Assert.Equal(16382,p.NewReservations.Count);
        Assert.Equal(14334,p.Words.Count(w=>w.Classification=="ReservedUnused"));
        Assert.Equal(int.MinValue,p.Panel[0]);
        Assert.Single(C.Classify(new byte[65536],[-987],new string('a',64),C.NeighborhoodRecognitionVersion).Panel);
        Assert.Throws<InvalidDataException>(()=>C.Classify(new byte[24576],[-987],new string('a',64),C.NeighborhoodRecognitionVersion));
    }

    private sealed class Temporary : IDisposable
    {
        internal string Root { get; }=Path.Combine(Path.GetTempPath(),"tower-neighborhood-recognition-test-"+Guid.NewGuid().ToString("N"));
        public void Dispose()
        {
            var full=Path.GetFullPath(Root);var temp=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))+Path.DirectorySeparatorChar;
            if (!full.StartsWith(temp,StringComparison.OrdinalIgnoreCase)) throw new IOException("Unsafe fixture cleanup.");
            if (Directory.Exists(full)) Directory.Delete(full,true);
        }
    }
    private static (TowerFixedFamilyRequest Q,TowerFixedFamilyInputs Input) Fixture(string root)
    {
        var d=Definition();Directory.CreateDirectory(root);var content=Path.Combine(root,"content");Directory.CreateDirectory(content);
        var prior=Path.Combine(root,"prior-seed-ledger.json");HarnessJson.WriteNew(prior,new { historical=d.ExcludedCombatSeeds });
        var definition=Path.Combine(root,"definition.json");HarnessJson.WriteNew(definition,d);
        var history=new Dictionary<string,string>{{prior,HarnessJson.FileHash(prior)}};
        var auditor=Path.Combine(Repository(),"Balance Harness/analysis/audit-affinity-neighborhood-recognition.py");
        var q=new TowerFixedFamilyRequest(d.Version,content,definition,HarnessJson.FileHash(definition),root,Path.Combine(root,"result"),history,
            19800,18790481920,new Dictionary<string,TowerDiagnosticPhaseLimit>(),1800,536870912,
            [new("Literal neighborhood admission",1800,536870912,prior,HarnessJson.FileHash(prior))])
            { Recognition=new(Plan,auditor,HarnessJson.FileHash(auditor)) };
        C.ValidateRequest(q,true);Directory.CreateDirectory(q.OutputRoot);HarnessJson.WriteNew(C.P(q,"request.json"),q);
        File.Copy(Plan,C.P(q,"plan.json"));File.Copy(auditor,C.P(q,"auditor.py"));
        return(q,new(d,new(history,[-987])));
    }

    [Fact]
    public void Nontransferable_envelope_and_launch_are_exact()
    {
        using var temp=new Temporary();var(q,_)=Fixture(temp.Root);
        var saved=Environment.GetEnvironmentVariable("NEIGHBORHOOD_RECOGNITION_TEST_OUTPUT");
        if(saved is not null)
        {
            Directory.CreateDirectory(saved);
            HarnessJson.WriteNew(Path.Combine(saved,"request-binding.json"),new { request=q,canonicalHash=HarnessJson.Hash(q) });
        }
        foreach (var bad in new[]{q with { PriorSeconds=600 },q with { PriorBytes=1073741824 },q with { MaximumSeconds=23400 },
            q with { MaximumBytes=q.MaximumBytes+1 },q with { Version=C.PreservationRecognitionVersion }})
            Assert.Throws<InvalidDataException>(()=>C.ValidateRequest(bad));
        var now=DateTimeOffset.UtcNow;
        var launch=new IncumbentTieLaunch(q.Version,new string('a',64),now,now.AddSeconds(14400),now.AddSeconds(18000),
            18000,18253611008,14400,17179869184,Environment.ProcessId,"suspended-owned-job-v1");
        C.ValidateRecognitionLaunch(q,launch,new string('a',64));
        Assert.Throws<InvalidDataException>(()=>C.ValidateRecognitionLaunch(q,launch with { MaximumSeconds=18001 },new string('a',64)));
        Assert.Throws<InvalidDataException>(()=>C.ValidateRecognitionLaunch(q,launch with { NativeMaximumBytes=18253611008 },new string('a',64)));
    }

    [Theory]
    [InlineData("entropy-pending")] [InlineData("entropy-start")] [InlineData("entropy-drawn")] [InlineData("entropy-written")]
    [InlineData("entropy-complete")] [InlineData("confirmation-binding")] [InlineData("confirmation-ledger")]
    [InlineData("confirmation-reserved")] [InlineData("chunks-bound")]
    public void Interrupted_reservation_never_refills_and_keeps_exposed_values(string boundary)
    {
        using var temp=new Temporary();var(q,input)=Fixture(temp.Root);var freeze=C.Freeze(q,input,()=>{},default);
        using var stop=new CancellationTokenSource();var draws=0;
        Assert.ThrowsAny<OperationCanceledException>(()=>C.Reserve(q,freeze,input.History.Files,stop.Token.ThrowIfCancellationRequested,stop.Token,
            stage=>{if(stage==boundary)stop.Cancel();},bytes=>{draws++;FixedFamilyFixtureHost.Entropy(bytes);}));
        Assert.Equal(boundary is "entropy-pending" or "entropy-start"?0:1,draws);
        if(draws==1){Assert.Equal(65536,new FileInfo(C.P(q,"entropy.bin")).Length);Assert.True(File.Exists(C.P(q,"entropy-complete.json")));}
        Assert.Throws<IOException>(()=>C.Reserve(q,freeze,input.History.Files,()=>{},default,entropy:_=>throw new Exception("Redraw")));
        Assert.False(File.Exists(C.P(q,"attempts.jsonl")));
    }

    [Fact]
    public void Batch_shortfall_remains_reserved_and_cannot_be_retried()
    {
        using var temp=new Temporary();var(q,input)=Fixture(temp.Root);var freeze=C.Freeze(q,input,()=>{},default);
        Assert.Throws<InvalidDataException>(()=>C.Reserve(q,freeze,input.History.Files,()=>{},default,entropy:bytes=>Array.Clear(bytes)));
        var ledger=HarnessJson.Read<JsonElement>(C.P(q,"seed-ledger.json"));
        Assert.Equal(0,ledger.GetProperty("reserved")[0].GetInt32());
        Assert.Equal("Complete",ledger.GetProperty("reservationState").GetString());
        Assert.Throws<IOException>(()=>C.Reserve(q,freeze,input.History.Files,()=>{},default,entropy:_=>throw new Exception("Redraw")));
        Assert.False(File.Exists(C.P(q,"chunks.json")));
    }

    [Fact]
    public async Task Wrong_command_profile_rejects_before_side_effects()
    {
        using var temp=new Temporary();var(q,_)=Fixture(temp.Root);
        var before=Directory.GetFiles(temp.Root,"*",SearchOption.AllDirectories).ToDictionary(p=>p,HarnessJson.FileHash);
        foreach(var action in new[]{"check","run","audit","publication-check","verify"})
            await Assert.ThrowsAsync<InvalidDataException>(()=>C.RecognitionCommand(
                [C.RecognitionCommandPrefix(C.PreservationRecognitionVersion)+"-"+action,action=="check"?C.P(q,"request.json"):q.OutputRoot],
                default,C.PreservationRecognitionVersion));
        Assert.Equal(before,Directory.GetFiles(temp.Root,"*",SearchOption.AllDirectories).ToDictionary(p=>p,HarnessJson.FileHash));
    }
}
