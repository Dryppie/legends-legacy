using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using BalanceHarness.ProcessFixture;
using Domain.Models.Combat;
using Xunit.Abstractions;
using F = EssenceSystem.Tests.BalanceHarnessCompositionSearchFixture;
using I = EssenceSystem.Tests.BalanceHarnessIncumbentSelectionTests;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessSelectionDiagnosticTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-diagnostic-fixture-"+Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Diagnostic tests entered combat.")).Activate();
    private readonly ITestOutputHelper output;
    public BalanceHarnessSelectionDiagnosticTests(ITestOutputHelper output) { this.output = output; Directory.CreateDirectory(root); }
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }
    private static string Json(object value) => JsonSerializer.Serialize(value, HarnessJson.Options);

    internal static TowerBossDiscoveryDefinition Template()
    {
        var original = I.Definition(owners: 10);
        var budget = original.Budget with { CharacterLevel = 40, EssenceSlots = 5, PriorityFloor = 5 };
        var contexts = original.Contexts.Select(c => c with { CharacterTemplates = c.CharacterTemplates.Select(p => p with {
            Build = p.Build with { CharacterLevel = 40 } }).ToArray() }).ToArray();
        var references = original.References.Select(r => r with { Scenario = r.Scenario with { FloorNumber = 5,
            Party = r.Scenario.Party.Select(p => p with { Build = p.Build with { CharacterLevel = 40,
                EssenceIds = p.Build.EssenceIds.Append("e39").Order(StringComparer.Ordinal).ToArray(),
                IdentityEssenceIds = Enumerable.Range(1, 5).Select(i => "neutral-identity-slot-"+i).ToArray() } }).ToArray() } }).ToArray();
        var starts = original.Starts.Select(s => s with { Party = TowerPartySelection.Choice("fixture",
            references.Single(r => r.Id == s.ReferenceId).Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)) }).ToArray();
        return original with { Budget = budget, Contexts = contexts, References = references, Starts = starts, ExcludedCombatSeeds = [-987],
            Generation = original.Generation with { Seeds = [] }, Stages = original.Stages with {
                Schedules = new Dictionary<string, BossDiscoverySchedule> { ["fixture"] = new([], [], [], []) } },
            MaximumBattles = 4640, SettingsHash = HarnessJson.Hash(DiagnosticFixtureHost.Settings), ExecutionHash = HarnessJson.Hash(ExecutionIdentity.Current()) };
    }

    private (TowerSelectionDiagnosticRequest Q, TowerPracticalInputs Input, BossGenerationMechanics Mechanics) Input()
    {
        var d = Template(); TowerSelectionDiagnostic.ValidateDefinition(d, true);
        var prior = Path.Combine(root, "prior-seed-ledger.json"); HarnessJson.WriteNew(prior, new { historical = d.ExcludedCombatSeeds });
        var source = Path.Combine(root, "template.json"); HarnessJson.WriteNew(source, d);
        var content = Path.Combine(root, "content"); Directory.CreateDirectory(content);
        var history = new Dictionary<string, string> { [prior] = HarnessJson.FileHash(prior) };
        var operation = new TowerPracticalRequest(TowerPracticalSearch.AllocationVersion, content, source, HarnessJson.FileHash(source), root,
            Path.Combine(root, "result"), history, 240, 128L*1048576, Allocation: new(17, "literal-diagnostic", 8, 32, 1000));
        var q = new TowerSelectionDiagnosticRequest(TowerSelectionDiagnostic.Version, operation, new Dictionary<string, TowerDiagnosticPhaseLimit> {
            ["admission"] = new(30, 8L*1048576), ["search"] = new(60, 32L*1048576), ["confirmation"] = new(60, 64L*1048576), ["audit"] = new(60, 16L*1048576) });
        TowerSelectionDiagnostic.ValidateRequest(q);
        return (q, new(d, new(history, [-987])), F.Mechanics(TowerBossDiscovery.CopyGenerationInputs(d)));
    }

    private static TowerPracticalLaunch Launch(TowerSelectionDiagnosticRequest q)
    {
        Directory.CreateDirectory(q.Operation.OutputRoot); var now = DateTimeOffset.UtcNow; using var parent = Process.GetCurrentProcess();
        var launch = new TowerPracticalLaunch(HarnessJson.Hash(q), now, now.AddSeconds(q.Operation.MaximumSeconds), parent.Id, parent.StartTime.ToUniversalTime().Ticks);
        HarnessJson.WriteNew(TowerSelectionDiagnostic.P(q, "request.json"), q); HarnessJson.WriteNew(TowerSelectionDiagnostic.P(q, "launch.json"), launch); return launch;
    }

    private async Task<(TowerSelectionDiagnosticRequest Q, BossGenerationMechanics Mechanics, TowerDiagnosticResult Result)> Completed(string mode = "complete")
    {
        var (q, input, mechanics) = Input(); var launch = Launch(q); var clock = Stopwatch.StartNew();
        var result = await TowerSelectionDiagnostic.RunOperation(q, launch, _ => input,
            (binding, history, attempt, hash, phase, check, token) => DiagnosticFixtureHost.Study(q, binding, mechanics, mode, history, attempt, hash, phase, check, token),
            token => DiagnosticFixtureHost.Verify(q, mechanics, token), default, candidate: DiagnosticFixtureHost.Candidate);
        HarnessJson.WriteNew(TowerSelectionDiagnostic.P(q, "worker-result.json"), result);
        TowerSelectionDiagnostic.Publish(q, launch, clock, default);
        return (q, mechanics, result);
    }

    [Fact]
    public async Task Complete_literal_archive_reconstructs_both_audits_and_preserves_primary_and_anchors()
    {
        var (q, mechanics, result) = await Completed();
        Assert.Equal("SelectionMissDemonstrated", result.DiagnosticDecision); Assert.Equal(4, result.Rates.Count); Assert.Equal(3, result.Contrasts.Count);
        Assert.Equal(2, result.RecommendedPartyIds.Count);
        Assert.Equal("NotAssessed", result.BalanceAssessment);
        var verified = await TowerSelectionDiagnostic.VerifyPublication(q.Operation.OutputRoot, ct => DiagnosticFixtureHost.Verify(q, mechanics, ct));
        Assert.Equal(Json(result), Json(verified));
        var study = HarnessJson.Read<TowerDiagnosticStudy>(TowerSelectionDiagnostic.P(q, "study/study.json"));
        Assert.Equal(study.Freeze.PrimaryId, result.PrimaryId); Assert.Equal(640, study.Freeze.CompletedAttempts);
        var panel = HarnessJson.Read<TowerDiagnosticPanel>(TowerSelectionDiagnostic.P(q, "confirmation-binding.json"));
        Assert.Equal(1000, panel.Panel.Count); Assert.Equal(2048, panel.NewReservations.Count); Assert.Equal(1048, panel.Words.Count(w => w.Classification == "ReservedUnused"));
        Assert.Equal(2090, TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(TowerSelectionDiagnostic.P(q, "seed-ledger.json"))).Length);
        var exported = HarnessJson.Read<JsonElement>(TowerSelectionDiagnostic.P(q, "teams.json"));
        Assert.All(exported.GetProperty("teams").EnumerateArray(), e => Assert.Empty(e.GetProperty("scenario").GetProperty("seeds").EnumerateArray()));
        // The real native verifier cannot authenticate synthetic content/runtime preparation.
        await Assert.ThrowsAnyAsync<Exception>(() => TowerSelectionDiagnostic.Verify(q.Operation.OutputRoot));
        output.WriteLine("Literal archive resource observation: " + Json(new {
            completion = HarnessJson.Read<JsonElement>(TowerSelectionDiagnostic.P(q, "completion.json")),
            retainedBytes = TowerBulkCampaign.StorageBytes(q.Operation.OutputRoot, default),
            phases = TowerSelectionDiagnostic.PhaseNames.Select(name =>
                HarnessJson.Read<TowerDiagnosticPhaseReceipt>(TowerSelectionDiagnostic.P(q, name+"-phase.json"))).ToArray(),
            actualCombat = 0, productionEntropyDraws = 0, scope = "Synthetic reports; no content/executable copy or combat-cost qualification" }));
    }

    [Theory]
    [InlineData("negative", "NoSelectionMissDemonstrated")]
    [InlineData("incumbent", "SelectionMissDemonstrated")]
    public async Task Complete_negative_and_incumbent_primary_are_valid_without_reselection(string mode, string expected)
    {
        var (_, _, result) = await Completed(mode); Assert.Equal(expected, result.DiagnosticDecision);
        if (mode == "incumbent") Assert.Contains(result.PrimaryId!, result.RecommendedPartyIds);
    }

    [Fact]
    public async Task Diagnostic_search_has_exact_old_kernel_nomination_and_primary_parity()
    {
        var d = TowerSelectionDiagnostic.BindSearch(Template(), new Dictionary<string, int[]> {
            ["construction"] = [17], ["discovery"] = Enumerable.Range(101,8).ToArray(), ["selection"] = Enumerable.Range(201,32).ToArray() });
        var ordinary = d with { Stages = d.Stages with { Schedules = new Dictionary<string, BossDiscoverySchedule> {
            ["fixture"] = new(d.Stages.Schedules["fixture"].Discovery, d.Stages.Schedules["fixture"].Selection, Enumerable.Range(301,1000).ToArray(), []) } } };
        TowerBossDiscovery.Validate(ordinary); Assert.Throws<InvalidDataException>(() => TowerBossDiscovery.Validate(d));
        var input = TowerBossDiscovery.CopyGenerationInputs(d); var mechanics = F.Mechanics(input);
        Task<BossDiscoveryMeasurement> Measure(PartyChoice p, string _, CancellationToken __) => Task.FromResult(I.Measure(input,p,3,Convert.ToUInt32(p.Id[..6],16)/(double)0xffffff*100));
        var prior = await TowerSuppliedCompositionSearch.RunAsync(ordinary, mechanics, Measure);
        var diagnostic = await TowerSuppliedCompositionSearch.RunDiagnosticAsync(new(TowerSelectionDiagnostic.Version,new string('a',64),new string('b',64),d),mechanics,Measure,default);
        Assert.Equal("Complete", diagnostic.Status); Assert.Equal(Json(prior),Json(diagnostic));
        var selection = diagnostic.DiscoveryShortlist.Select(p => I.Measure(input with { DiscoverySeeds = d.Stages.Schedules.ToDictionary(p => p.Key,p => p.Value.Selection) },p,25,50)).ToArray();
        var primary = TowerBossStudyPolicy.Select(input,mechanics,prior.DiscoveryShortlist,selection,d.Stages.Schedules.ToDictionary(p=>p.Key,p=>p.Value.Selection),1,d.Stages.SelectionPolicyVersion).Single();
        var frozen = TowerSelectionDiagnostic.Freeze(new(TowerSelectionDiagnostic.Version,new string('a',64),new string('b',64),d),diagnostic,selection,mechanics,new string('c',64));
        Assert.Equal(primary.Party.Id,frozen.PrimaryId); Assert.Equal(4,frozen.Nominees.Count);
    }

    [Theory]
    [InlineData("version")] [InlineData("family-shape")] [InlineData("cohort")] [InlineData("owned")]
    [InlineData("confirmation")] [InlineData("history")] [InlineData("phase")] [InlineData("budget")]
    public void Invalid_contracts_fail_before_sampling(string change)
    {
        var (q,input,_) = Input(); var d = input.Definition;
        if (change is "version" or "phase" or "budget")
        {
            q = change switch { "version" => q with { Version="unknown" }, "phase" => q with { Phases=new Dictionary<string,TowerDiagnosticPhaseLimit>() },
                _ => q with { Operation=q.Operation with { MaximumSeconds=20 } } };
            Assert.Throws<InvalidDataException>(()=>TowerSelectionDiagnostic.ValidateRequest(q)); return;
        }
        d = change switch { "family-shape" => d with { Stages=d.Stages with { GeneratedFinalists=4 } }, "cohort" => d with { RequiredPartySize=9 },
            "owned" => d with { OwnedCopies=d.AllowedEssences.ToDictionary(e=>e.Id,_=>50) },
            "confirmation" => d with { Stages=d.Stages with { Schedules=new Dictionary<string,BossDiscoverySchedule>{{"fixture",new([],[],[3],[])}} } },
            _ => d with { ExcludedCombatSeeds=[2,1] } };
        Assert.Throws<InvalidDataException>(()=>TowerSelectionDiagnostic.ValidateDefinition(d,true));
    }

    [Fact]
    public void Entropy_mapping_keeps_signed_values_rejections_and_every_unused_tail()
    {
        var bytes=new byte[8192]; DiagnosticFixtureHost.Entropy(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0,4),int.MinValue);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4,4),-987);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8,4),17);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12,4),int.MinValue);
        var search = new[]{17}.Concat(Enumerable.Range(101,8)).Concat(Enumerable.Range(201,32)).ToArray();
        var panel=TowerSelectionDiagnostic.Classify(bytes,[-987],search,new string('a',64));
        Assert.Equal(int.MinValue,panel.Panel[0]); Assert.Equal("AlreadyReserved",panel.Words[1].Classification);
        Assert.Equal("AlreadyReserved",panel.Words[2].Classification); Assert.Equal("DuplicateBatch",panel.Words[3].Classification);
        Assert.Equal(2045,panel.NewReservations.Count); Assert.Equal(1045,panel.Words.Count(w=>w.Classification=="ReservedUnused"));
        Assert.Throws<InvalidDataException>(()=>TowerSelectionDiagnostic.Classify(bytes[..8191],[-987],search,new string('a',64)));
        Assert.DoesNotContain(TowerSelectionDiagnostic.Classify(new byte[8192],[-987],search,new string('a',64)).Words,w=>w.Classification=="ReservedUnused");
    }

    [Theory]
    [InlineData(500,49,0,false)] [InlineData(500,50,0,true)]
    [InlineData(500,500,450,false)] [InlineData(0,100,0,false)]
    public void Decision_requires_observed_margin_paired_support_and_viability(int primaryWins,int gains,int losses,bool qualifies)
    {
        var ids=new[]{"primary","other","anchor-a","anchor-b"};
        var nominees=ids.Select((id,i)=>new TowerDiagnosticNominee(i,id,new string((char)('a'+i),64),null!,i>=2 ? new[]{id}:[],new string('f',64))).ToArray();
        var freeze=new TowerDiagnosticFreeze(TowerSelectionDiagnostic.Version,new string('a',64),new string('b',64),new string('c',64),new string('d',64),new string('e',64),640,ids[0],nominees);
        var evidence=ids.Select((id,member)=>new TowerDiagnosticCell(id,Enumerable.Range(0,1000).Select(i=>new TowerBalanceTrial(i,
            (member==1 ? i>=losses && i<primaryWins || i>=primaryWins && i<primaryWins+gains : i<primaryWins) ? BattleOutcome.Victory:BattleOutcome.Defeat)).ToArray())).ToArray();
        var result=TowerSelectionDiagnostic.Assess(new(TowerSelectionDiagnostic.Version,null!,[],freeze,evidence),new string('a',64));
        var contrast=result.Contrasts.Single(c=>c.OtherPartyId=="other");
        Assert.Equal(gains,contrast.Gains);Assert.Equal(losses,contrast.Losses);Assert.Equal(qualifies,contrast.Qualifies);
        Assert.Equal(qualifies ? "SelectionMissDemonstrated":"NoSelectionMissDemonstrated",result.DiagnosticDecision);
    }

    [Theory]
    [InlineData("entropy-pending")] [InlineData("entropy-start")] [InlineData("entropy-drawn")] [InlineData("entropy-written")]
    [InlineData("entropy-complete")] [InlineData("confirmation-binding")] [InlineData("confirmation-ledger")] [InlineData("confirmation-reserved")]
    public async Task Entropy_interruption_never_refills_resumes_or_starts_confirmation(string boundary)
    {
        var(q,input,mechanics)=Input(); var launch=Launch(q); var entropyCalls=0;
        var failure = await Assert.ThrowsAnyAsync<Exception>(()=>TowerSelectionDiagnostic.RunOperation(q,launch,_=>input,
            (binding,history,attempt,hash,phase,check,ct)=>DiagnosticFixtureHost.Study(q,binding,mechanics,"complete",history,attempt,hash,phase,check,ct,
                stage=>{if(stage=="entropy-drawn")entropyCalls++;if(stage==boundary)throw new IOException("Injected boundary");}),
            _=>throw new InvalidOperationException("Interrupted run cannot audit"),default,candidate:DiagnosticFixtureHost.Candidate));
        Assert.Equal("Injected boundary", failure.Message);
        Assert.Equal(boundary is "entropy-pending" or "entropy-start" ? 0 : 1,entropyCalls);
        Assert.Equal(1280,File.ReadLines(TowerSelectionDiagnostic.P(q,"attempts.jsonl")).Count());
        var ledger=HarnessJson.Read<JsonElement>(TowerSelectionDiagnostic.P(q,"history-input.json"));
        Assert.Equal(boundary=="confirmation-reserved" ? "Complete" : "Pending",ledger.GetProperty("reservationState").GetString());
        Assert.False(File.Exists(TowerSelectionDiagnostic.P(q,"result.json")));
    }

    [Fact]
    public async Task Short_entropy_batch_preserves_every_exposed_value_and_cannot_audit()
    {
        var(q,input,mechanics)=Input(); var launch=Launch(q);
        var binding=TowerSelectionDiagnostic.ReserveSearch(q,input,()=>{},default,candidate:DiagnosticFixtureHost.Candidate);
        Directory.CreateDirectory(TowerSelectionDiagnostic.P(q,"study"));
        // Only reservation behavior is exercised; a dummy freeze cannot pass full reconstruction.
        var frozen=new TowerDiagnosticFreeze(TowerSelectionDiagnostic.Version,HarnessJson.Hash(q),HarnessJson.Hash(binding),new string('a',64),new string('b',64),new string('c',64),640,"literal",[]);
        HarnessJson.WriteNew(TowerSelectionDiagnostic.P(q,"study/nominees-freeze.json"),frozen);
        Assert.Throws<InvalidDataException>(()=>TowerSelectionDiagnostic.ReserveConfirmation(q,binding,frozen,input.History.Files,()=>{},default,entropy:_=>{}));
        var ledger=HarnessJson.Read<JsonElement>(TowerSelectionDiagnostic.P(q,"history-input.json"));
        Assert.Equal("Complete",ledger.GetProperty("reservationState").GetString());
        Assert.Contains(0,TowerSearchBenchmark.History(ledger));
        Assert.Throws<IOException>(()=>TowerSelectionDiagnostic.ReserveConfirmation(q,binding,frozen,input.History.Files,()=>{},default,entropy:_=>throw new Exception("No redraw")));
        await Assert.ThrowsAnyAsync<Exception>(()=>DiagnosticFixtureHost.Verify(q,mechanics,default));
    }

    [Fact]
    public async Task Ten_resigned_tampering_cases_still_fail_reconstruction_or_publication()
    {
        var(q,mechanics,_)=await Completed();
        var protectedNames=new[]{"study/nominees-freeze.json","confirmation-binding.json","seed-ledger.json","events.jsonl","attempts.jsonl","study/study.json","audit-phase.json","phase.json","study/files.json","files.json"};
        var original=protectedNames.ToDictionary(name=>name,name=>File.ReadAllBytes(TowerSelectionDiagnostic.P(q,name)));
        foreach(var change in new[]{"primary","nominee","panel","tail","event","attempt","outcome","phase","late-phase","active-phase"})
        {
        void Edit(string name,Action<JsonNode> edit) {var path=TowerSelectionDiagnostic.P(q,name);var value=JsonNode.Parse(File.ReadAllText(path))!;edit(value);File.WriteAllText(path,value.ToJsonString(HarnessJson.Options));}
        switch(change)
        {
            case "primary": Edit("study/nominees-freeze.json",n=>n["primaryId"]="wrong");break;
            case "nominee": Edit("study/nominees-freeze.json",n=>n["nominees"]![0]!["recipeHash"]=new string('a',64));break;
            case "panel": Edit("confirmation-binding.json",n=>n["panel"]![0]=-999999);break;
            case "tail": Edit("seed-ledger.json",n=>n["reserved"]!.AsArray().RemoveAt(1041));break;
            case "event": File.AppendAllText(TowerSelectionDiagnostic.P(q,"events.jsonl"),File.ReadLines(TowerSelectionDiagnostic.P(q,"events.jsonl")).First()+"\n");break;
            case "attempt": File.AppendAllText(TowerSelectionDiagnostic.P(q,"attempts.jsonl"),"{}\n");break;
            case "outcome": Edit("study/study.json",n=>n["evidence"]![0]!["trials"]![0]!["outcome"]=99);break;
            case "phase": Edit("audit-phase.json",n=>n["chargedSeconds"]=999);break;
            case "late-phase": Edit("audit-phase.json",n=> {
                var late = HarnessJson.Read<TowerPracticalLaunch>(TowerSelectionDiagnostic.P(q,"launch.json")).Deadline;
                n["phase"]!["startedAt"] = JsonValue.Create(late);
                n["phase"]!["deadline"] = JsonValue.Create(late.AddSeconds(q.Phases["audit"].Seconds));
            });break;
            case "active-phase": Edit("phase.json",n=>n["baseBytes"]=0);break;
        }
        foreach(var directory in new[]{TowerSelectionDiagnostic.P(q,"study"),q.Operation.OutputRoot})
        {File.Delete(Path.Combine(directory,"files.json"));TowerSelectionDiagnostic.Seal(directory);}
        await Assert.ThrowsAnyAsync<Exception>(()=>TowerSelectionDiagnostic.VerifyPublication(q.Operation.OutputRoot,ct=>DiagnosticFixtureHost.Verify(q,mechanics,ct)));
        foreach(var (name,bytes) in original)File.WriteAllBytes(TowerSelectionDiagnostic.P(q,name),bytes);
        }
    }

    [Theory]
    [InlineData("complete")] [InlineData("storage-hang")] [InlineData("entropy-start-hang")]
    public async Task Owned_process_completes_or_stops_at_resource_boundary(string mode)
    {
        var(q,_,mechanics)=Input();
        if(mode=="entropy-start-hang")q=q with {Phases=q.Phases.ToDictionary(p=>p.Key,p=>p.Key=="confirmation" ? p.Value with {Seconds=5}:p.Value)};
        var fixture=Path.Combine(root,"process.json");HarnessJson.WriteNew(fixture,new DiagnosticFixture(DiagnosticFixtureHost.Version,mode,q,mechanics));
        var start=FixtureHost.Start("diagnostic-parent",fixture);start.RedirectStandardOutput=true;start.RedirectStandardError=true;
        using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try {await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(100));}
        finally {if(!process.HasExited){process.Kill(entireProcessTree:true);await process.WaitForExitAsync();}}
        var output=await stdout;var error=await stderr;
        Assert.True(mode=="complete" ? process.ExitCode==0 : process.ExitCode!=0,output+error);
        Assert.Equal(mode=="complete",File.Exists(TowerSelectionDiagnostic.P(q,"completion.json")));
        if(mode!="complete")Assert.Equal("Pending",HarnessJson.Read<JsonElement>(TowerSelectionDiagnostic.P(q,"history-input.json")).GetProperty("reservationState").GetString());
    }

    [Theory]
    [InlineData("parent")] [InlineData("worker")]
    public async Task Owner_or_worker_death_retains_pending_and_cannot_publish(string killed)
    {
        var(q,_,mechanics)=Input();var fixture=Path.Combine(root,"process.json");
        HarnessJson.WriteNew(fixture,new DiagnosticFixture(DiagnosticFixtureHost.Version,"search-pending-hang",q,mechanics));
        var start=FixtureHost.Start("diagnostic-parent",fixture);start.RedirectStandardOutput=true;start.RedirectStandardError=true;
        using var parent=Process.Start(start)!;var stdout=parent.StandardOutput.ReadToEndAsync();var stderr=parent.StandardError.ReadToEndAsync();Process? worker=null;
        try
        {
            var timeout=Stopwatch.StartNew();var marker=TowerSelectionDiagnostic.P(q,"fixture-ready.json");
            while(!File.Exists(marker) && !parent.HasExited && timeout.Elapsed<TimeSpan.FromSeconds(15))await Task.Delay(50);
            Assert.True(File.Exists(marker),"Worker did not reach its Pending boundary.");
            var receipt=HarnessJson.Read<TowerPracticalWorkerStart>(TowerSelectionDiagnostic.P(q,"worker-start.json"));
            worker=Process.GetProcessById(receipt.ProcessId);Assert.Equal(receipt.ProcessStartedUtcTicks,worker.StartTime.ToUniversalTime().Ticks);
            if(killed=="parent")parent.Kill();else worker.Kill();
            await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(File.Exists(TowerSelectionDiagnostic.P(q,"completion.json")));
            Assert.Equal("Pending",HarnessJson.Read<JsonElement>(TowerSelectionDiagnostic.P(q,"history-input.json")).GetProperty("reservationState").GetString());
        }
        finally
        {
            if(worker is not null){if(!worker.HasExited)worker.Kill(entireProcessTree:true);await worker.WaitForExitAsync();worker.Dispose();}
            if(!parent.HasExited)parent.Kill(entireProcessTree:true);await parent.WaitForExitAsync();await stdout;await stderr;
        }
    }
}
