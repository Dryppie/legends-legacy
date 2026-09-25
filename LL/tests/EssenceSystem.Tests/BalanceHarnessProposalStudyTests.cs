using System.Buffers.Binary;
using System.Text.Json;
using BalanceHarness;
using Domain.Models.Combat;
using A = EssenceSystem.Tests.BalanceHarnessAdaptiveRacingTests;
using S = BalanceHarness.TowerProposalStudy;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessProposalStudyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "proposal-study-" + Guid.NewGuid().ToString("N"));
    private readonly IDisposable guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Study fixture entered combat.")).Activate();
    public BalanceHarnessProposalStudyTests() => Directory.CreateDirectory(root);
    public void Dispose() { guard.Dispose(); Directory.Delete(root, true); }

    internal static (TowerProposalContext Context, TowerProposalComparisonPlan Plan, int[] History) Fixture(bool creation = false, bool selector = false, bool validation = false, bool preservation = false, bool alliedAction = false, bool placement = false)
    {
        var creationPlan = creation || selector || validation || preservation || alliedAction || placement ? BalanceHarnessAffinityCreationNativeTests.Plan() : null;
        var (p, inventory) = creationPlan is null ? BalanceHarnessDamageAffinityTests.Fixture()
            : (creationPlan.Racing, creationPlan.DamageAffinityInventory!);
        if (placement)
        {
            var placed = BalanceHarnessLoadoutPlacementTests.Plan().Racing;
            p = placed with { Mechanics = placed.Mechanics with { Essences = inventory.Essences } };
        }
        var literalHash=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("{}")));
        var hashes=p.Scope.ContentHashes.ToDictionary(x=>x.Key,_=>literalHash);
        p=p with { Scope=p.Scope with { ContentHashes=hashes,SettingsHash=HarnessJson.Hash(new TowerSettings(new(),10)),ExecutionHash=HarnessJson.Hash(ExecutionIdentity.Current()) },
            Mechanics=p.Mechanics with { SourceHashes=p.Mechanics.SourceHashes.ToDictionary(x=>x.Key,_=>literalHash) } };
        inventory=inventory with { SourceHashes=p.Mechanics.SourceHashes };
        if (alliedAction || placement) inventory = BalanceHarnessAlliedActionComparisonTests.AddProviders(inventory, ["e02"]);
        var context = new TowerProposalContext(p.Scope, p.Mechanics, p.BenchmarkReferenceId, p.RootSeed, inventory);
        var history = S.LegacySeeds(context).Concat(p.Scope.Generation.Seeds).Append(p.RootSeed).Concat(p.Scope.ExcludedCombatSeeds).Distinct().Order().ToArray();
        context = context with { Scope = context.Scope with { ExcludedCombatSeeds = history.Except(S.LegacySeeds(context)).ToArray() } };
        var policy = creationPlan?.Policy ?? TowerProposalPolicies.BenchmarkDamageEdits(TowerDamageSourceAffinities.Create(inventory).Affinities.Select(a => a.Id));
        if (preservation || alliedAction || placement) policy = TowerProposalPolicies.BenchmarkPreservingAffinityCreation(TowerDamageSourceAffinities.Create(inventory).Affinities
            .Where(a => a.ModifierEssenceId == "e10" && a.ProducerEssenceId is "e00" or "e01").Select(a => a.Id));
        if (alliedAction || placement) policy = TowerProposalPolicies.BenchmarkAlliedActionAffinityCreation(policy.CreatedDamageAffinityIds!);
        return (context, placement ? TowerProposalComparison.CreateLoadoutPlacementPlan(context, policy)
            : alliedAction ? TowerProposalComparison.CreateAlliedActionPlan(context, policy)
            : preservation ? TowerProposalComparison.CreatePreservationPlan(context, policy)
            : validation ? TowerProposalComparison.CreateValidationPlan(context, policy)
            : selector ? TowerProposalComparison.CreateSelectorPlan(context, policy) : TowerProposalComparison.CreatePlan(context, policy), history);
    }
    private static byte[] Entropy()
    {
        var bytes = new byte[65536];
        for (var i = 0; i < 16384; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4,4), 200000+i);
        return bytes;
    }

    [Theory]
    [InlineData(.02,0,3,3,"LargerFreshEvaluationWarranted")]
    [InlineData(.02,0,2,3,"Inconclusive")]
    [InlineData(.02,0,3,2,"Inconclusive")]
    [InlineData(.019,0,3,3,"Inconclusive")]
    [InlineData(0,0,0,0,"NoObservedOutputDifferentiation")]
    [InlineData(-.02,.1,3,3,"AbandonThisConfiguration")]
    [InlineData(.1,-.02,3,2,"AbandonThisConfiguration")]
    [InlineData(.1,-.02,3,3,"Inconclusive")]
    [InlineData(0,-.03,0,0,"AbandonThisConfiguration")]
    public void Decisions_apply_frozen_boundaries_and_never_adopt(double method, double benchmark, int differing, int promising, string expected)
        => Assert.Equal(expected, S.Decide(Fixture().Plan.Analysis, method, benchmark, differing, promising));

    [Fact]
    public void Classifier_reserves_the_entire_exposed_tail_and_detects_duplicates_and_history()
    {
        var bytes = Entropy(); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0,4),17);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4,4),200002);
        var allocation = S.Classify(bytes,[17]);
        Assert.Equal(1,allocation.HistoricalCollisions); Assert.Equal(1,allocation.Duplicates);
        Assert.Equal(3948,allocation.Selected.Count); Assert.Equal(16382,allocation.Reserved.Count);
        Assert.Equal(200002,allocation.Selected[0]); Assert.Contains(216383,allocation.Reserved);
        Assert.DoesNotContain(17,allocation.Reserved);
        Assert.Throws<InvalidDataException>(()=>S.Classify(bytes,[17,17]));
    }

    [Theory]
    [InlineData("pending", false)]
    [InlineData("entropy-written", false)]
    [InlineData("before-complete", false)]
    [InlineData("pending", true)]
    [InlineData("entropy-written", true)]
    [InlineData("before-complete", true)]
    public void Interrupted_reservation_keeps_pending_sentinel_and_exposed_bytes(string boundary, bool creation)
    {
        var (context,plan,history)=Fixture(creation);
        var inputs=new ProposalStudyInputs(plan,context,new(new(),10),history,new(new Dictionary<string,string>(),history));
        using var cancellation=new CancellationTokenSource(); var exposed=false;
        Assert.Throws<OperationCanceledException>(()=>S.Reserve(root,inputs,()=>{},()=>{},cancellation.Token,
            b=>{exposed=true; Entropy().CopyTo(b,0);},s=>{if(s==boundary)cancellation.Cancel();}));
        Assert.Equal("Pending",HarnessJson.Read<JsonElement>(Path.Combine(root,"history-input.json")).GetProperty("reservationState").GetString());
        Assert.Equal(boundary!="pending",exposed);
        if(exposed) Assert.Equal(Entropy(),File.ReadAllBytes(Path.Combine(root,"entropy.bin")));
        Assert.Throws<IOException>(()=>S.Reserve(root,inputs,()=>{},()=>{},default,b=>throw new InvalidOperationException("refill")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Incomplete_root_stops_the_study_before_any_heldout_measurement(bool creation)
    {
        var (context,plan,_)=Fixture(creation); var measured=0; var saved=new List<string>();
        await Assert.ThrowsAsync<InvalidDataException>(()=>S.Execute(plan,context,S.Classify(Entropy(),[]).Selected,
            async pair=>await TowerProposalComparison.ExecutePairAsync(plan,pair,(_,_)=>{},(_,p)=>TowerProposalPolicies.RunAsync(p,(_,_)=>throw new IOException("failed trial"))),
            (_,_)=>{measured++; throw new InvalidOperationException();},(name,_)=>saved.Add(name),()=>new string('a',64),_=>{},default));
        Assert.Equal(0,measured); Assert.Equal(new[]{"binding.json","pair-01.json"},saved);
    }

    [Fact]
    public void Barrier_can_read_the_durable_prefix_while_the_exclusive_writer_remains_open()
    {
        var path = Path.Combine(root, "attempts.jsonl");
        using var writer = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        var rows = string.Concat(Enumerable.Range(1, S.SearchFights).Select(n => $"{{\"kind\":\"Started\",\"ordinal\":{n}}}\n{{\"kind\":\"Completed\",\"ordinal\":{n}}}\n"));
        var bytes = System.Text.Encoding.UTF8.GetBytes(rows); writer.Write(bytes); writer.Flush(true);
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes)), S.AttemptPrefix(path));
        Assert.Throws<IOException>(() => File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Full_native_evidence_fixture_freezes_all_outputs_audits_and_rejects_resealed_tampering(bool creation)
        => await FullNativeFixture(creation);

    internal async Task FullNativeFixture(bool creation, bool selector = false, bool validation = false, bool preservation = false, bool alliedAction = false, bool placement = false,
        ProposalEvidenceStorage? evidenceStorage = null, string? storageExport = null, bool accountWrites = false)
    {
        var writeWork = accountWrites ? new TowerWorkAccounting() : null;
        using var writeScope = writeWork?.Activate();
        var (context,plan,history)=Fixture(creation, selector, validation, preservation, alliedAction, placement); var settings=new TowerSettings(new(),10);
        var inputs=new ProposalStudyInputs(plan,context,settings,history,new(new Dictionary<string,string>(),history),evidenceStorage);
        var allocation=S.Reserve(root,inputs,()=>{},()=>{},default,b=>Entropy().CopyTo(b,0));
        Assert.Equal(plan.Version, allocation.Version);
        Assert.Equal(HarnessJson.Hash(allocation),HarnessJson.Hash(S.VerifyReservation(root,inputs)));
        Directory.CreateDirectory(Path.Combine(root,"study"));
        var storage = evidenceStorage is null ? null : new TowerProposalEvidenceStorage.Writer(root,evidenceStorage,()=>{});
        var searches=new Dictionary<(int,string),TowerProposalRacingReport>();
        var nativeTrials=new Dictionary<(int,string),List<LoadoutTrial>>();
        var nativeReports=new Dictionary<string,TowerBattleReport>();
        var heldoutRequests=new List<TowerPanelObservation>();
        using var attempts=new TowerPracticalSearch.Attempts(Path.Combine(root,"attempts.jsonl"),21888,()=>{});
        var completed=0;
        async Task<TowerProposalRacingReport> Search(int number,string arm,TowerProposalRacingPlan p)
        {
            var folder=S.SearchRoot(root,number,arm); Directory.CreateDirectory(folder);
            var trials=new List<LoadoutTrial>(); nativeTrials[(number,arm)]=trials;
            (LoadoutTrial Trial,int MaximumTicks) prepared=default;
            TowerPanelTrial current = null!;
            var report=await TowerProposalRacingNative.ExecuteAsync(p,Path.Combine(folder,"racing"),64L*1048576,
                request=>{current=request; prepared=A.Binding(request); return prepared;},(_,_,scenario,seed,_)=>{
                    var battle=validation || preservation || alliedAction || placement ? ValidationLiteral(current, plan.BenchmarkPartyId, number % 2 == 1) : A.Report(scenario,seed);
                    if (placement && current.Role != TowerBenchmarkValidation.ValidationRole
                        && context.Scope.Starts.Any(s => s.Party.Id == current.PartyId))
                        battle = battle with { Succeeded = false, Battle = battle.Battle with {
                            Summary = battle.Battle.Summary with { ContentOutcome = BattleOutcome.Draw, EngineOutcome = BattleOutcome.Draw } } };
                    trials.Add(prepared.Trial!); nativeReports[folder+prepared.Trial!.Id]=battle;
                    return Task.FromResult((prepared.Trial!,battle));
                },()=>{},default,attempts.Event,storage);
            if(storage is not null) S.Seal(folder,default);
            searches[(number,arm)]=report; completed++; return report;
        }
        var result=await S.Execute(plan,context,allocation.Selected,
            pair=>TowerProposalComparison.ExecutePairAsync(plan,pair,(_,_)=>{},(arm,p)=>Search(pair.Root,arm,p)),
            (freeze,request)=>{
                Assert.Equal(24,completed); Assert.Equal(12,freeze.Families.Count);
                Assert.True(File.Exists(Path.Combine(root,"study/freeze.json")));
                Assert.Equal(S.SearchFights+request.Ordinal,attempts.Started);
                var outcome=new TowerPanelOutcome(HarnessJson.Hash(request),$"trial-{request.Ordinal:D6}",request.Seed,BattleOutcome.Victory,0,100,1);
                heldoutRequests.Add(new(request,outcome)); return Task.FromResult(outcome);
            },(name,value)=>{
                if(storage is not null && TowerProposalEvidenceStorage.Eligible(name)) storage.Put(Path.Combine(root,"study"),name,value);
                else HarnessJson.WriteNew(Path.Combine(root,"study",name),value);
            },
            ()=>S.AttemptPrefix(Path.Combine(root,"attempts.jsonl")),attempts.Event,default);
        attempts.Dispose();
        Assert.Equal(plan.Version, result.Version);
        Assert.Equal(selector || validation || placement ? "Inconclusive" : "NoObservedOutputDifferentiation",result.Decision);
        Assert.Equal(12,result.Roots.Count); Assert.All(result.Roots,r=>Assert.Equal(0,r.MethodDifference));
        Assert.Equal(12672+heldoutRequests.Count,result.Fights); Assert.Equal(result.Fights,attempts.Completed);
        // V4 selects the benchmark in the all-positive-tie fixture. The control
        // retains the primary; held-out physical membership remains two per root.
        Assert.Equal(placement ? 24*256 : validation || preservation || alliedAction ? 18*256 : 12*2*256,result.HeldoutFights); Assert.Equal(validation || placement ? 6 : selector ? 12 : 0,result.DifferingRoots);
        if (validation || preservation || alliedAction || placement)
        {
            Assert.Equal(6, result.Validation!.PassedRoots); Assert.Equal(6, result.Validation.FallbackRoots);
            Assert.Equal(12, result.Validation.Decisions.Count);
        }
        if (selector) Assert.All(result.Roots, r => Assert.Equal(new[] { 0, 0 }, r.ChangedPositionsPerWave));
        if (preservation || alliedAction || placement)
        {
            Assert.Equal(6, result.ControlValidation!.PassedRoots); Assert.Equal(6, result.ControlValidation.FallbackRoots);
            Assert.Equal(12, result.ControlValidation.Decisions.Count);
            Assert.Contains(result.Roots, r => r.ChangedPositionsPerWave.Any(n => n > 0));
        }
        if(storage is not null)
        {
            storage.SealDirectory(Path.Combine(root,"study"),TowerProposalEvidenceStorage.Names(HarnessJson.Read<ProposalStudyFreeze>(Path.Combine(root,"study/freeze.json"))),
                (name,value)=>HarnessJson.WriteNew(Path.Combine(root,"study",name),value));
            storage.Finish();
        }
        HarnessJson.WriteNew(Path.Combine(root,"provisional-result.json"),result); S.Seal(Path.Combine(root,"study"),default);
        writeScope?.Dispose();
        if (writeWork is not null)
        {
            Assert.True(writeWork.Snapshot()["applicationWriteBytes.journal"] > 0);
            Assert.True(writeWork.Snapshot()["trackedRetainedBytes"] > 0);
            Assert.Equal(0, writeWork.Snapshot()["trackedScratchBytes"]);
            Assert.False(writeWork.Snapshot().ContainsKey("storageLengthObservationFailures"));
            HarnessJson.WriteNew(Path.Combine(root,"fixture-native-write-work.json"),
                writeWork.Receipt("native",new('a',64),new('b',64),true));
        }
        TowerProposalEvidenceStorage.Reader? storageReader = null;
        Task<TowerProposalRacingReport> VerifySearch(int n,string arm)
        {
            var folder=S.SearchRoot(root,n,arm);
            return TowerProposalRacingNative.VerifyEvidenceAsync(Path.Combine(folder,"racing"),A.Binding,nativeTrials[(n,arm)],t=>nativeReports[folder+t.Id],default,storageReader);
        }
        var index=0;
        Task<ProposalStudyResult> Verify() {
            storageReader=TowerProposalEvidenceStorage.Open(root,evidenceStorage,default);
            return S.AuditStudy(root,inputs,allocation,VerifySearch,r=>{
            var observation=heldoutRequests[index++]; Assert.Equal(HarnessJson.Hash(r),HarnessJson.Hash(observation.Request)); return observation.Outcome;
        },default,storageReader); }
        var work = new TowerWorkAccounting();
        using (work.Activate()) Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(await Verify()));
        Assert.Equal(12,work.Snapshot()["reconstructedRoots"]);
        Assert.Equal(24,work.Snapshot()["reconstructedTrajectories"]);
        Assert.Equal(12672,work.Snapshot()["reconstructedTrialBindings"]);
        Assert.Equal(1,work.Snapshot()["reconstructedStudyEndpoints"]);
        if (placement) Assert.Equal(12,work.Snapshot()["reconstructedCatalogues"]);
        HarnessJson.WriteNew(Path.Combine(root,"fixture-native-work.json"),work.Receipt("nativeAudit",new('a',64),new('b',64),true));
        Assert.Equal(heldoutRequests.Count,index);
        if(storageReader is not null)
            HarnessJson.WriteNew(Path.Combine(root,"fixture-storage-audit-work.json"),new {
                storageReader.PhysicalBytesRead, storageReader.DecodedBytesProcessed, storageReader.DecodePasses });
        var path=Path.Combine(root,"study/freeze.json"); var freeze=HarnessJson.Read<ProposalStudyFreeze>(path);
        Assert.Equal(plan.Version, freeze.Version);
        File.WriteAllText(path,JsonSerializer.Serialize(freeze with { Version = creation ? S.Version : S.CreationVersion },HarnessJson.Options));
        File.Delete(Path.Combine(root,"study/files.json")); S.Seal(Path.Combine(root,"study"),default);
        index=0; await Assert.ThrowsAsync<InvalidDataException>(Verify);
        File.WriteAllText(path,JsonSerializer.Serialize(freeze with { Families=freeze.Families.Reverse().ToArray() },HarnessJson.Options));
        File.Delete(Path.Combine(root,"study/files.json")); S.Seal(Path.Combine(root,"study"),default);
        index=0; await Assert.ThrowsAsync<InvalidDataException>(Verify);
        var export=storageExport ?? Environment.GetEnvironmentVariable(placement ? "TOWER_PLACEMENT_STUDY_FIXTURE_EXPORT"
            : alliedAction ? "TOWER_ALLIED_ACTION_STUDY_FIXTURE_EXPORT"
            : preservation ? "TOWER_PRESERVATION_STUDY_FIXTURE_EXPORT"
            : validation ? "TOWER_VALIDATION_STUDY_FIXTURE_EXPORT" : selector ? "TOWER_SELECTOR_STUDY_FIXTURE_EXPORT"
            : creation ? "TOWER_CREATION_STUDY_FIXTURE_EXPORT" : "TOWER_PROPOSAL_STUDY_FIXTURE_EXPORT");
        if(!string.IsNullOrEmpty(export))
        {
            // Restore the verified fixture before exporting; retain literal reports
            // for a separate Python audit test, never as campaign evidence.
            File.WriteAllText(path,JsonSerializer.Serialize(freeze,HarnessJson.Options));
            File.Delete(Path.Combine(root,"study/files.json")); S.Seal(Path.Combine(root,"study"),default);
            ExportFixture(export,root,inputs,allocation,nativeTrials,nativeReports,heldoutRequests);
        }
    }

    internal static TowerBattleReport ValidationLiteral(TowerPanelTrial request, string benchmark, bool pass)
    {
        var report = A.Report(request.Scenario, request.Seed);
        if (request.Role != TowerBenchmarkValidation.ValidationRole) return report;
        var won = request.PartyId != benchmark && request.Scenario.Seeds.ToList().IndexOf(request.Seed) < (pass ? 5 : 4);
        var outcome = won ? BattleOutcome.Victory : BattleOutcome.Draw;
        return report with { Succeeded = won, Battle = report.Battle with {
            Summary = report.Battle.Summary with { ContentOutcome = outcome, EngineOutcome = outcome } } };
    }

    private static void ExportFixture(string export,string root,ProposalStudyInputs inputs,ExplorationReservation allocation,
        Dictionary<(int,string),List<LoadoutTrial>> trials,Dictionary<string,TowerBattleReport> reports,List<TowerPanelObservation> heldout)
    {
        Assert.False(Path.Exists(export)); Directory.CreateDirectory(export);
        foreach(var file in Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories))
        { var target=Path.Combine(export,Path.GetRelativePath(root,file)); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file,target); }
        HarnessJson.WriteNew(Path.Combine(export,"fixture-context.json"),inputs.Context);
        HarnessJson.WriteNew(Path.Combine(export,"fixture-plan.json"),inputs.Plan);
        HarnessJson.WriteNew(Path.Combine(export,"fixture-history.json"),inputs.History);
        HarnessJson.WriteNew(Path.Combine(export,"fixture-settings.json"),inputs.Settings);
        void Scope(string directory,string algorithm)
        {
            HarnessJson.WriteNew(Path.Combine(directory,"scope.json"),new LoadoutScope(algorithm,inputs.Settings,ExecutionIdentity.Current(),inputs.Context.Scope.ContentHashes,"gzip-json-v1"));
            foreach(var key in inputs.Context.Scope.ContentHashes.Keys)
            { var content=Path.Combine(directory,"content/Data",key); Directory.CreateDirectory(Path.GetDirectoryName(content)!); File.WriteAllText(content,"{}"); }
        }
        var encoded = TowerProposalEvidenceStorage.Open(root,inputs.EvidenceStorage,default);
        foreach(var pair in trials)
        {
            var directory=S.SearchRoot(export,pair.Key.Item1,pair.Key.Item2); Directory.CreateDirectory(Path.Combine(directory,"battles")); Directory.CreateDirectory(Path.Combine(directory,"recipes"));
            var report=encoded is null ? HarnessJson.Read<TowerProposalRacingReport>(Path.Combine(directory,"racing/search.json"))
                : encoded.Read<TowerProposalRacingReport>(Path.Combine(S.SearchRoot(root,pair.Key.Item1,pair.Key.Item2),"racing"),"search.json");
            var observations=report.Evaluation.Panels.SelectMany(p=>p.Observations).ToArray();
            foreach(var trial in pair.Value)
            {
                var scenario=observations.Single(o=>o.Outcome.TrialId==trial.Id).Request.Scenario;
                var recipe=Path.Combine(directory,"recipes",trial.Recipe+".json"); if(!File.Exists(recipe))HarnessJson.WriteNew(recipe,scenario);
                TowerLoadoutArchive.WriteBattle(directory,trial.Id,reports[S.SearchRoot(root,pair.Key.Item1,pair.Key.Item2)+trial.Id],"gzip-json-v1");
            }
            File.WriteAllLines(Path.Combine(directory,"trials.jsonl"),pair.Value.Select(t=>JsonSerializer.Serialize(t,new JsonSerializerOptions(HarnessJson.Options){WriteIndented=false})));
            var nativePlan=HarnessJson.Read<TowerProposalRacingPlan>(Path.Combine(directory,"racing/plan.json"));
            Scope(directory,TowerProposalRacingNative.ArchiveAlgorithm(nativePlan));
            if(encoded is not null) File.Delete(Path.Combine(directory,"files.json")); // Newly generated test export only.
            S.Seal(directory,default);
        }
        HarnessJson.WriteNew(Path.Combine(export,"fixture-heldout.json"),heldout);
        var heldoutRoot=Path.Combine(export,"heldout"); Directory.CreateDirectory(Path.Combine(heldoutRoot,"recipes")); Directory.CreateDirectory(Path.Combine(heldoutRoot,"battles"));
        var freeze=HarnessJson.Read<ProposalStudyFreeze>(Path.Combine(export,"study/freeze.json"));
        Scope(heldoutRoot,inputs.Plan.Version+"/heldout/"+HarnessJson.Hash(freeze));
        var heldoutTrials=new List<LoadoutTrial>();
        foreach(var row in heldout)
        {
            var binding=A.Binding(row.Request); heldoutTrials.Add(binding.Trial);
            var recipe=Path.Combine(heldoutRoot,"recipes",binding.Trial.Recipe+".json"); if(!File.Exists(recipe))HarnessJson.WriteNew(recipe,row.Request.Scenario);
            TowerLoadoutArchive.WriteBattle(heldoutRoot,binding.Trial.Id,A.Report(row.Request.Scenario,row.Request.Seed),"gzip-json-v1");
        }
        File.WriteAllLines(Path.Combine(heldoutRoot,"trials.jsonl"),heldoutTrials.Select(t=>JsonSerializer.Serialize(t,new JsonSerializerOptions(HarnessJson.Options){WriteIndented=false})));
        S.Seal(heldoutRoot,default);
    }

    [Fact]
    public async Task Owned_panel_snapshots_preserve_the_exact_full_checkpoint_archive_and_charge_before_preparation()
    {
        var (context, design, _) = Fixture();
        var plan = TowerProposalComparison.Bind(design, context, S.Classify(Entropy(), []).Selected).Pairs[0].Candidate;
        async Task<TowerProposalRacingReport> Run(bool owned)
        {
            var folder = Path.Combine(root, owned ? "owned" : "full"); var started = 0; var completed = 0;
            (LoadoutTrial Trial, int MaximumTicks) prepared = default;
            var result = await TowerProposalRacingNative.ExecuteAsync(plan, folder, 64L*1048576, request => {
                var charge = JsonSerializer.Deserialize<TowerRacingCharge>(File.ReadLines(Path.Combine(folder, "charges.jsonl")).Last(), HarnessJson.Options)!;
                Assert.Equal(new(request.Ordinal, request.PanelHash, request.PartyId, request.Seed), charge);
                Assert.True(File.Exists(Path.Combine(folder, $"panel-{Array.FindIndex(plan.Racing.Panels.ToArray(), p => p.Role == request.Role)+1:D2}.json")));
                if (owned) { Assert.Equal(request.Ordinal, started); Assert.Equal(request.Ordinal-1, completed); }
                prepared = A.Binding(request); return prepared;
            }, (_, _, scenario, seed, _) => Task.FromResult((prepared.Trial!, A.Report(scenario, seed))), () => { }, default,
                owned ? done => { if (done) completed++; else started++; } : null);
            Assert.Equal("Complete", result.Evaluation.Status);
            if (owned) { Assert.Equal(528, started); Assert.Equal(528, completed); }
            return result;
        }
        Assert.Equal(HarnessJson.Hash(await Run(false)), HarnessJson.Hash(await Run(true)));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "full")))
            Assert.Equal(File.ReadAllBytes(file), File.ReadAllBytes(Path.Combine(root, "owned", Path.GetFileName(file))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Owned_failed_or_cancelled_preparation_retains_the_unmatched_charge(bool cancel)
    {
        var (context, design, _) = Fixture();
        var plan = TowerProposalComparison.Bind(design, context, S.Classify(Entropy(), []).Selected).Pairs[0].Candidate;
        var folder = Path.Combine(root, "interrupted"); var started = 0; var completed = 0;
        using var stop = new CancellationTokenSource();
        var result = await TowerProposalRacingNative.ExecuteAsync(plan, folder, 64L*1048576, _ => {
            if (cancel) { stop.Cancel(); stop.Token.ThrowIfCancellationRequested(); }
            throw new IOException("Literal preparation failure");
        }, (_, _, _, _, _) => throw new InvalidOperationException("Failed preparation dispatched a battle"), () => { }, stop.Token,
            done => { if (done) completed++; else started++; });
        Assert.Equal(cancel ? "Cancelled" : "Failed", result.Evaluation.Status);
        Assert.Equal(1, started); Assert.Equal(0, completed); Assert.Equal(1, result.Evaluation.ChargedEvaluations);
        Assert.Single(File.ReadAllLines(Path.Combine(folder, "charges.jsonl")));
        Assert.Null(result.Evaluation.RawSelectedId);
    }

    [Fact]
    public void Root_intervals_and_seed_covariance_use_their_separate_sampling_units()
    {
        var (context, plan, _) = Fixture();
        var references = context.Scope.Starts.Select(s => s.Party).ToArray();
        var families = new List<ProposalStudyFamily>(); var evidence = new List<ProposalStudyEvidence>();
        var searches = new List<TowerProposalComparisonSearch>();
        for (var rootNumber = 1; rootNumber <= 12; rootNumber++)
        {
            var seeds = Enumerable.Range(100000 + rootNumber*256, 256).ToArray();
            var members = references.Select((party, i) => {
                var scenario = TowerBossDiscovery.Scenario(context.Scope, context.Scope.Contexts.Single().Id, party, seeds);
                return new ProposalStudyMember(TowerBossDiscovery.RecipeHash(scenario.Party), party, scenario,
                    [new[] { "control", "candidate", "benchmark" }[i]]);
            }).ToArray();
            families.Add(new(rootNumber, seeds, members));
            foreach (var member in members)
            {
                var rows = seeds.Select((seed, index) => {
                    var win = member.Roles[0] switch { "control" => index%2 == 0, "benchmark" => index%4 == 0,
                        _ => rootNumber <= 6 ? index%4 != 3 : index%2 == 0 };
                    var request = new TowerPanelTrial("scope", "panel", "heldout", member.Party.Id, index+1, seed, member.Scenario);
                    return new TowerPanelObservation(request, new("request", "trial", seed, win ? BattleOutcome.Victory : BattleOutcome.Defeat, 0, 100, 1));
                }).ToArray();
                evidence.Add(new(rootNumber, member.RecipeHash, rows));
            }
            TowerProposalRacingReport Search(PartyChoice party) => new("fixture", "plan", "policy",
                [new(1, 0, [], [], [], [], [party]), new(2, 0, [], [], [], [], [party])], null!);
            searches.Add(new("plan", "pair", "Complete", Search(references[0]), Search(references[1])));
        }
        var result = S.Summarize(plan, context, new(S.Version, "plan", "context", "values", "searches", "attempts", families), searches, evidence);
        Assert.Equal(.125, result.Method.Mean); Assert.Equal(.375, result.Benchmark.Mean);
        Assert.Equal(Math.Sqrt(12*.125*.125/11), result.Method.StandardDeviation, 12);
        Assert.Equal(.125 - 2.200985160082949*Math.Sqrt(12*.125*.125/11)/Math.Sqrt(12), result.Method.Lower, 12);
        Assert.Equal(Math.Sqrt(.1875/255), result.Roots[0].MethodSeedStandardError, 12);
        Assert.Equal(Math.Sqrt(.25/255), result.Roots[0].BenchmarkSeedStandardError, 12);
        Assert.Equal(.125/255, result.Roots[0].SeedCovariance, 12);
        Assert.Equal(0, result.Roots[6].MethodSeedStandardError); Assert.Equal(0, result.Roots[6].SeedCovariance);
        Assert.Equal(64, result.Roots[0].MethodGains); Assert.Equal(0, result.Roots[0].MethodLosses);
        Assert.Equal(12, result.DifferingRoots); Assert.Equal(0, result.NovelRoots); Assert.Equal(21888, result.Fights);
        Assert.Equal(.125, result.MethodMedian); Assert.Equal(0, result.MethodWorst); Assert.Equal(.25, result.BenchmarkWorst);
        Assert.Equal("Inconclusive", result.Decision);
        var tiny = result with { Benchmark = result.Benchmark with { Mean = 1e-7 } };
        var nativeJson = JsonSerializer.Serialize(tiny, HarnessJson.Options);
        Assert.Contains("1E-07", nativeJson);
        using var pythonJson = JsonDocument.Parse(nativeJson.Replace("1E-07", "1e-7"));
        Assert.NotEqual(HarnessJson.Hash(tiny), HarnessJson.Hash(pythonJson.RootElement));
        Assert.True(S.SameIndependentResult(pythonJson.RootElement, tiny));
        using var changedJson = JsonDocument.Parse(nativeJson.Replace("1E-07", "2e-7"));
        Assert.False(S.SameIndependentResult(changedJson.RootElement, tiny));
    }

    [Fact]
    public void Launch_contract_rejects_time_storage_or_ownership_drift()
    {
        var start=DateTimeOffset.UtcNow;
        var launch=new ExplorationLaunch(S.Version,new string('a',64),start,start.AddSeconds(9600),start.AddSeconds(10800),10800,6L*1073741824,9600,5905580032,1,"suspended-owned-job-v1");
        S.ValidateLaunch(launch,new string('a',64));
        foreach(var bad in new[]{launch with{NativeMaximumSeconds=9601},launch with{ParentProcessId=0},launch with{MaximumBytes=7L*1073741824},launch with{Deadline=start.AddSeconds(10801)}})
            Assert.Throws<InvalidDataException>(()=>S.ValidateLaunch(bad,new string('a',64)));
    }
}
