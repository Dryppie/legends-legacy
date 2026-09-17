using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessCurrentFamilyAdmissionTests
{
    private static string Hash(char c)=>new(c,64);
    private static IReadOnlyList<IReadOnlyList<string>> Vectors => Enumerable.Range(1,10)
        .Select(_=>(IReadOnlyList<string>)new[] {"e01","e02","e03","e04","e05"}).ToArray();
    private static string Control=>HarnessJson.Hash(Vectors.Select((ids,i)=>(ids,i)).ToDictionary(x=>x.i+1,x=>x.ids));
    private static TowerScenario Template
    {
        get
        {
            var original=BalanceHarnessTowerBossDiscoveryContractTests.UserScenario;
            var budget=TowerPartyProgression.Budget(5) with {PriorityFloor=5};
            return original with { Id="admission-fixture",FloorNumber=5,StartsAt=new(2000,1,1,0,0,0,TimeSpan.Zero),Seeds=[],
                Party=Enumerable.Range(1,10).Select(i=>new TowerPartyRecipe(i,original.Party[(i-1)%5].Build with {
                    Id=$"synthetic-{i}",CharacterLevel=budget.CharacterLevel,Tier=budget.Tier,Rank=budget.Rank,Quality=budget.Quality,
                    EssenceIds=Vectors[i-1],IdentityEssenceIds=Enumerable.Range(1,5).Select(n=>$"neutral-identity-slot-{n}").ToArray()
                })).ToArray() };
        }
    }
    private static TowerCurrentAdmissionEntry Entry(int i,string? key,string? anchor=null)=>new(i,
        JsonSerializer.SerializeToElement(new {file="fixture",row=i}),Hash('a'),"saved-fixture","2000-01-01T00:00:00+00:00",
        key,key is null ? "Incompatible":"StaticCompatibleProjection",key is null ? ["non-neutral-context"]:[],false,anchor);
    private static TowerCurrentAdmissionCell Cell(int i)=>new((i+1).ToString("x64"),Vectors,[i],i==0?[Control]:[],
        i==0?["required-control"]:[],"Pending",null);
    private static TowerCurrentAdmissionInventory Inventory(bool exceptions=false)
    {
        var cells=new[] {Cell(0),Cell(1)};
        var entries=new List<TowerCurrentAdmissionEntry> {Entry(0,cells[0].InputKey,"required-control"),Entry(1,cells[1].InputKey)};
        if(exceptions) entries.Add(Entry(2,null));
        return new(Template,cells,entries,[Control]);
    }
    private static JsonElement Participants(int health=100)=>JsonSerializer.SerializeToElement(Enumerable.Range(0,11)
        .Select(i=>new {slot=new {side=i<10?"Friendly":"Hostile",index=i},health}).ToArray());
    private static async Task<(TowerCurrentAdmissionResult Result,byte[] Rows)> Scan(TowerCurrentAdmissionInventory? input=null,
        Func<TowerScenario,CancellationToken,Task<(string,JsonElement)>>? prepare=null,Action? check=null,CancellationToken ct=default)
    {
        using var output=new MemoryStream();
        var result=await TowerCurrentFamilyAdmission.Scan(input??Inventory(),output,
            prepare??((s,_)=> {Assert.Empty(s.Seeds);return Task.FromResult((Hash('b'),Participants()));}),check??(()=>{}),ct);
        return (result,output.ToArray());
    }

    [Fact]
    public async Task Complete_scan_and_saved_audit_preserve_origins_and_native_aliases_without_preparing_again()
    {
        var inventory=Inventory();var calls=0;
        var (result,rows)=await Scan(inventory,(_,_)=> {calls++;return Task.FromResult((Hash('b'),Participants()));});
        Assert.True(result.ReadyForFamilyFreeze);Assert.Equal("Admitted",result.Status);
        Assert.Equal(1,result.DistinctNativeCells);Assert.Equal(1,result.AliasCells);Assert.Equal(1,result.ForcedNativeCells);
        Assert.Equal(new[] {Control},result.MatchedControls);Assert.Equal(0,result.Fights);Assert.Equal(0,result.NewSeeds);
        using var source=new MemoryStream(rows);
        var audited=await TowerCurrentFamilyAdmission.Audit(inventory,source,()=>{});
        Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(audited));Assert.Equal(2,calls);
        Assert.Equal(2,Encoding.UTF8.GetString(rows).Split('\n',StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public async Task Context_exceptions_remain_visible_and_block_family_freeze_after_successful_preparation()
    {
        var (result,_)=await Scan(Inventory(true));
        Assert.Equal("AdmittedWithContextExceptions",result.Status);Assert.Equal(1,result.IncompatibleOccurrences);
        Assert.Equal(3,result.SourceOccurrences);Assert.False(result.ReadyForFamilyFreeze);
    }

    [Theory]
    [InlineData("missing-origin")] [InlineData("duplicate-origin")] [InlineData("wrong-owner")]
    [InlineData("missing-control")] [InlineData("demoted-control")] [InlineData("unknown-control")]
    [InlineData("empty-source")] [InlineData("reordered-entry")] [InlineData("already-prepared")]
    public void Malformed_or_demoted_provenance_is_rejected_before_preparation(string defect)
    {
        var input=Inventory();var cells=input.Cells.ToArray();var entries=input.Entries.ToArray();
        switch(defect)
        {
            case "missing-origin":cells[1]=cells[1] with {EntryOrdinals=[]};break;
            case "duplicate-origin":cells[1]=cells[1] with {EntryOrdinals=[0]};break;
            case "wrong-owner":entries[1]=entries[1] with {InputKey=Hash('c')};break;
            case "missing-control":cells[0]=cells[0] with {RequiredControlIds=[]};break;
            case "demoted-control":cells[0]=cells[0] with {AnchorReasons=[]};break;
            case "unknown-control":cells[0]=cells[0] with {RequiredControlIds=[Hash('d')]};break;
            case "empty-source":entries[0]=entries[0] with {Source=JsonSerializer.SerializeToElement(new {})};break;
            case "reordered-entry":entries[0]=entries[0] with {Ordinal=1};break;
            case "already-prepared":cells[0]=cells[0] with {NativeAdmission="Passed"};break;
        }
        Assert.Throws<InvalidDataException>(()=>TowerCurrentFamilyAdmission.ValidateInventory(input with {Cells=cells,Entries=entries}));
    }

    [Theory]
    [InlineData("seeds")] [InlineData("time")] [InlineData("identity")] [InlineData("level")] [InlineData("floor")]
    public void Cohort_changes_cannot_hide_behind_a_projection(string defect)
    {
        var input=Inventory();var template=input.Template;
        if(defect=="seeds")template=template with {Seeds=[123]};
        if(defect=="time")template=template with {StartsAt=template.StartsAt.AddTicks(1)};
        if(defect=="floor")template=template with {FloorNumber=6};
        if(defect=="identity" || defect=="level")template=template with {Party=template.Party.Select((p,i)=>i!=0?p:
            p with {Build=defect=="identity"?p.Build with {IdentityEssenceIds=null}:p.Build with {CharacterLevel=41}}).ToArray()};
        Assert.Throws<InvalidDataException>(()=>TowerCurrentFamilyAdmission.ValidateInventory(input with {Template=template}));
    }

    [Theory]
    [InlineData("order")] [InlineData("duplicate")] [InlineData("missing-slot")]
    [InlineData("native-invalid")] [InlineData("alias-conflict")] [InlineData("control-replaced")]
    public async Task Bad_projections_and_preparation_failures_are_rows_not_silently_removed(string defect)
    {
        var input=Inventory();var cells=input.Cells.ToArray();var vectors=Vectors.Select(x=>x.ToArray()).ToArray();
        if(defect=="order")vectors[0]=vectors[0].Reverse().ToArray();
        if(defect=="duplicate")vectors[0][1]=vectors[0][0];
        if(defect=="missing-slot")vectors=vectors.Take(9).ToArray();
        if(defect=="control-replaced")vectors[0][4]="e06";
        var at=defect=="control-replaced"?0:1;
        cells[at]=cells[at] with {EssencesBySlot=vectors};input=input with {Cells=cells};
        var calls=0;
        var (result,rows)=await Scan(input,(_,_)=> {
            if(++calls==2 && defect=="native-invalid")throw new InvalidDataException("Duplicate source monster from production content.");
            return Task.FromResult((Hash('b'),Participants(defect=="alias-conflict"?calls:100)));
        });
        Assert.Equal(1,result.Invalid);Assert.Equal(2,result.Cells);Assert.False(result.ReadyForFamilyFreeze);
        Assert.Contains("\"status\":\"Invalid\"",Encoding.UTF8.GetString(rows));
        using var stream=new MemoryStream(rows);
        Assert.Equal(HarnessJson.Hash(result),HarnessJson.Hash(await TowerCurrentFamilyAdmission.Audit(input,stream,()=>{})));
    }

    [Theory]
    [InlineData("combat")] [InlineData("cancel")] [InlineData("bytes")] [InlineData("deadline")]
    public async Task Combat_and_resource_guards_abort_without_an_admitted_result(string defect)
    {
        using var stop=new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<Exception>(()=>Scan(prepare:(_,ct)=> {
            if(defect=="combat")TowerPerformanceTrace.BattleStarted();
            if(defect=="cancel"){stop.Cancel();ct.ThrowIfCancellationRequested();}
            return Task.FromResult((Hash('b'),Participants()));
        },check:()=> {
            if(defect=="bytes")TowerCurrentFamilyAdmission.Guard(0,100,10,1,false);
            if(defect=="deadline")TowerCurrentFamilyAdmission.Guard(10,0,10,100,false);
        },ct:stop.Token));
    }

    [Theory]
    [InlineData("short")] [InlineData("extra")] [InlineData("participant")]
    [InlineData("alias")] [InlineData("identity")] [InlineData("origin")] [InlineData("unknown-field")]
    public async Task Saved_audit_rejects_missing_extra_or_tampered_native_evidence(string defect)
    {
        var (_,bytes)=await Scan();var lines=Encoding.UTF8.GetString(bytes).Split('\n',StringSplitOptions.RemoveEmptyEntries).ToList();
        if(defect=="short")lines.RemoveAt(1);
        else if(defect=="extra")lines.Add(lines[0]);
        else
        {
            var row=JsonNode.Parse(lines[1])!;
            if(defect=="participant")row["participants"]![0]!["health"]=999;
            if(defect=="alias")row["aliasOf"]=Hash('d');
            if(defect=="identity")row["identity"]!["cellHash"]=Hash('d');
            if(defect=="origin")row["entryOrdinals"]=new JsonArray(0);
            if(defect=="unknown-field")row["newRule"]=true;
            lines[1]=row.ToJsonString();
        }
        using var stream=new MemoryStream(Encoding.UTF8.GetBytes(string.Join('\n',lines)+"\n"));
        await Assert.ThrowsAnyAsync<Exception>(()=>TowerCurrentFamilyAdmission.Audit(Inventory(),stream,()=>{}));
    }

    [Fact]
    public async Task Row_budget_rejects_growth_before_bytes_are_written()
    {
        using var output=new MemoryStream();using var writer=new TowerCurrentFamilyAdmission.RowStream(output,4,()=>{});
        writer.Write(new byte[3]);await writer.WriteAsync(new byte[1]);
        Assert.Throws<IOException>(()=>writer.Write(new byte[1]));Assert.Equal(4,output.Length);
    }

    [Theory]
    [InlineData("valid")] [InlineData("execution")] [InlineData("gameplay")] [InlineData("manifest")]
    [InlineData("sentinel")] [InlineData("combat")] [InlineData("preparation")] [InlineData("version")]
    [InlineData("missing-harness")] [InlineData("runtime")]
    public void Saved_producer_binds_the_new_harness_and_unchanged_gameplay_without_loading_runtime(string defect)
    {
        var hashes=new Dictionary<string,string> { ["BalanceHarness"]=Hash('a'),["Application"]=Hash('b'),
            ["Common"]=Hash('c'),["Domain"]=Hash('d'),["Services.LL"]=Hash('e') };
        var execution=new ExecutionIdentity("fixture","fixture","X64",hashes);
        var summary=JsonSerializer.SerializeToElement(new { execution=new {assemblyHashes=hashes} });
        // Only the harness may differ from the saved design runtime.
        execution=execution with {AssemblyHashes=new Dictionary<string,string>(hashes) { ["BalanceHarness"]=Hash('f') }};
        if(defect=="gameplay")execution=execution with {AssemblyHashes=new Dictionary<string,string>(hashes) { ["Domain"]=Hash('f') }};
        if(defect=="missing-harness")execution=execution with {AssemblyHashes=hashes.Where(p=>p.Key!="BalanceHarness").ToDictionary()};
        if(defect=="runtime")execution=execution with {Runtime=""};
        var request=new TowerCurrentAdmissionRequest(TowerCurrentFamilyAdmission.Version,"design","content",HarnessJson.Hash(execution),1800,1048576);
        var producer=new TowerCurrentAdmissionProducer(request.Version,TowerCurrentFamilyAdmission.DesignManifest,execution,request.ExecutionHash,0,true,false);
        if(defect=="execution")producer=producer with {ExecutionHash=Hash('f')};
        if(defect=="manifest")producer=producer with {DesignManifest=Hash('f')};
        if(defect=="sentinel")producer=producer with {PreparationSentinel=42};
        if(defect=="combat")producer=producer with {CombatScheduleCreated=true};
        if(defect=="preparation")producer=producer with {PreparationOnly=false};
        if(defect=="version")producer=producer with {Version="future"};
        if(defect=="valid")TowerCurrentFamilyAdmission.ValidateProducer(request,producer,summary);
        else Assert.Throws<InvalidDataException>(()=>TowerCurrentFamilyAdmission.ValidateProducer(request,producer,summary));
    }

    [Fact]
    public async Task Public_route_rejects_unknown_actions_and_old_limits_remain_unchanged()
    {
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerCurrentFamilyAdmission.Command(["tower-current-family-run","unused"]));
        Assert.Equal(43879,TowerCompleteFamily.Cells);Assert.Equal(560,TowerCompleteFamily.Anchors);
        Assert.Equal(2434784,TowerCompleteFamily.MaximumFights);
        var request=new TowerCurrentAdmissionRequest(TowerCurrentFamilyAdmission.Version,"design","content",Hash('a'),1801,1048576);
        Assert.Throws<InvalidDataException>(()=>TowerCurrentFamilyAdmission.ValidateRequest(request));
    }
}
