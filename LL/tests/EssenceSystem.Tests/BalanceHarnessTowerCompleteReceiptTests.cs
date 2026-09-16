using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BalanceHarness;
using Xunit.Abstractions;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompleteReceiptTests(ITestOutputHelper output)
{
    private static string Hash(object value) => HarnessJson.Hash(value);
    private static readonly string RequestHash = Hash("receipt-fixture-request");
    private static readonly Lazy<TowerCompleteAssessment> Full = new(() => {
        var cells = Enumerable.Range(0,TowerCompleteFamily.Cells).Select(i => {
            var id=i.ToString("x64");
            return new TowerCompleteCheck(id,new string('a',64),1,new(id,0,0,32,new string('b',64)),null);
        }).ToArray();
        return new(GoalOutcome.Fail,new("Proceed",cells.Take(4096).Select(c=>c.CellHash).ToArray(),[],Hash("synthetic-first-stage")),
            TowerCompleteFamily.MaximumFights,cells);
    });
    private static TowerCompleteAssessment Assessment(bool capacity) => capacity
        ? Full.Value with { Outcome=GoalOutcome.Inconclusive,LogicalTrials=1386208,
            Selection=new("SecondStageCapacityExceeded",Full.Value.Cells.Select(c=>c.CellHash).ToArray(),Full.Value.Cells.Select(c=>c.CellHash).ToArray(),Hash("synthetic-first-stage")) }
        : Full.Value;

    private static Task<TowerCompleteRunContext> Load(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var p=new TowerCompleteProtocol(TowerCompleteFamily.Version,"fixture",TowerCompleteFamily.SourceSeal,
            TowerCompleteFamilyInputs.HarnessHash,Hash(ExecutionIdentity.Current()),TowerCompleteFamily.MaximumFights,
            TowerCompleteFamily.MaximumSeconds,TowerCompleteFamily.MaximumBytes,0,TowerStorageAccountant.Mode,0,0,
            new Dictionary<string,string>(),new Dictionary<string,string>(),new Dictionary<string,string>());
        return Task.FromResult(new TowerCompleteRunContext(p,2400,p.MaximumBytes-67108864,LaunchHash:RequestHash));
    }
    private static Task<TowerCompleteAssessment> Produce(Temp t,TowerCompleteAssessment result,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Only serialized synthetic evidence. Never constructs a combat runner, input, roster or seed.
        Assert.True(File.Exists(t.ControlFile("run-started.json")));
        HarnessJson.WriteNew(t.StudyFile("assessment.json"),result);
        HarnessJson.WriteNew(t.StudyFile(TowerCompleteFamilyRun.FinalFiles),new Dictionary<string,string>{{"assessment.json",HarnessJson.FileHash(t.StudyFile("assessment.json"))}});
        return Task.FromResult(result);
    }
    private static Task<TowerCompleteAssessment> Reconstruct(Temp t,CancellationToken ct)
    {
        Assert.True(File.Exists(t.ControlFile("verify-started.json")));
        TowerBulkCampaign.VerifyFiles(t.Study,TowerCompleteFamilyRun.FinalFiles,true,ct);
        return Task.FromResult(HarnessJson.Read<TowerCompleteAssessment>(t.StudyFile("assessment.json")));
    }
    private static Task<object> Run(Temp t,TowerCompleteAssessment result,CancellationToken token=default) =>
        TowerCompleteFamilyLaunch.RunOperation(t.Study,t.Control,RequestHash,(_,ct)=>Load(ct),(_,ct)=>Produce(t,result,ct),token);
    private static Task<object> Verify(Temp t,CancellationToken token=default) =>
        TowerCompleteFamilyLaunch.VerifyOperation(t.Study,t.Control,RequestHash,Load,(_,ct)=>Reconstruct(t,ct),token);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Full_family_outer_run_and_verify_publish_small_hash_bound_receipts(bool capacity)
    {
        using var t=new Temp();var assessment=Assessment(capacity);var clock=Stopwatch.StartNew();
        var legacyBytes=JsonSerializer.SerializeToUtf8Bytes(new{status="Complete",requestHash=RequestHash,result=assessment},HarnessJson.Options).LongLength;
        Assert.True(legacyBytes>65536);
        await Run(t,assessment);var runSeconds=clock.Elapsed.TotalSeconds;
        var studyBefore=Directory.GetFiles(t.Study).ToDictionary(p=>p,HarnessJson.FileHash);
        clock.Restart();await Verify(t);var verifySeconds=clock.Elapsed.TotalSeconds;
        foreach(var (path,hash) in studyBefore) Assert.Equal(hash,HarnessJson.FileHash(path));
        var run=HarnessJson.Read<JsonElement>(t.ControlFile("run-result.json"));var verified=HarnessJson.Read<JsonElement>(t.ControlFile("verify-result.json"));
        var summary=run.GetProperty("result").Deserialize<TowerCompleteLaunchSummary>(HarnessJson.Options)!;
        Assert.Equal(Hash(summary),Hash(verified.GetProperty("result")));
        Assert.Equal(43879,summary.Cells);Assert.Equal(assessment.LogicalTrials,summary.LogicalTrials);Assert.Equal(assessment.Outcome.ToString(),summary.Outcome);
        Assert.Equal(capacity?43879:4096,summary.SecondCells);Assert.Equal(capacity?43879:0,summary.Breaches);
        Assert.Equal(HarnessJson.FileHash(t.StudyFile("assessment.json")),summary.AssessmentHash);
        Assert.Equal(HarnessJson.FileHash(t.StudyFile(TowerCompleteFamilyRun.FinalFiles)),summary.InventoryHash);
        var runBytes=new FileInfo(t.ControlFile("run-result.json")).Length;var verifyBytes=new FileInfo(t.ControlFile("verify-result.json")).Length;
        Assert.InRange(runBytes,1,65536);Assert.InRange(verifyBytes,1,65536);
        Assert.Equal(JsonValueKind.Number,run.GetProperty("result").GetProperty("cells").ValueKind);
        Assert.False(File.Exists(t.ControlFile("run-failure.json")));Assert.False(File.Exists(t.ControlFile("verify-failure.json")));
        await Assert.ThrowsAsync<IOException>(()=>Run(t,assessment));await Assert.ThrowsAsync<IOException>(()=>Verify(t));
        output.WriteLine("RECEIPT_METRICS "+JsonSerializer.Serialize(new {capacity,legacyBytes,runBytes,verifyBytes,runSeconds,verifySeconds,
            assessmentBytes=new FileInfo(t.StudyFile("assessment.json")).Length,cells=summary.Cells,engineCalls=0}));
    }

    [Theory]
    [InlineData("count")] [InlineData("assessment-hash")] [InlineData("inventory-hash")]
    [InlineData("request-hash")] [InlineData("status")] [InlineData("assessment-file")] [InlineData("inventory-file")]
    public async Task Verification_rejects_receipt_or_archive_tampering_and_retains_failure(string change)
    {
        using var t=new Temp();await Run(t,Full.Value);
        if(change=="assessment-file") File.AppendAllText(t.StudyFile("assessment.json")," ");
        else if(change=="inventory-file") File.AppendAllText(t.StudyFile(TowerCompleteFamilyRun.FinalFiles)," ");
        else
        {
            var path=t.ControlFile("run-result.json");var row=JsonNode.Parse(File.ReadAllText(path))!;
            switch(change)
            {
                case "count":row["result"]!["cells"]=1;break;
                case "assessment-hash":row["result"]!["assessmentHash"]=Hash("other");break;
                case "inventory-hash":row["result"]!["inventoryHash"]=Hash("other");break;
                case "request-hash":row["requestHash"]=Hash("other");break;
                case "status":row["status"]="Failed";break;
            }
            File.WriteAllText(path,row.ToJsonString(HarnessJson.Options));
        }
        await Assert.ThrowsAsync<InvalidDataException>(()=>Verify(t));
        Assert.True(File.Exists(t.ControlFile("verify-started.json")));Assert.True(File.Exists(t.ControlFile("verify-failure.json")));
        Assert.False(File.Exists(t.ControlFile("verify-result.json")));await Assert.ThrowsAsync<IOException>(()=>Verify(t));
    }

    [Theory]
    [InlineData("cancel")] [InlineData("changed-assessment")] [InlineData("record-budget")] [InlineData("controller-result")]
    public async Task Interrupted_outer_completion_keeps_study_and_durable_failure_without_success(string change)
    {
        using var t=new Temp();using var stop=new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<Exception>(()=>TowerCompleteFamilyLaunch.RunOperation(t.Study,t.Control,RequestHash,async(initial,ct)=>{
            if(!initial)
            {
                if(change=="cancel")stop.Cancel();
                if(change=="changed-assessment")File.AppendAllText(t.StudyFile("assessment.json")," ");
                if(change=="record-budget")
                {
                    var existing=TowerBulkCampaign.StorageBytes(t.Control);
                    using var f=File.Create(t.ControlFile("fill.bin"));f.SetLength(TowerCompleteFamilyLaunch.LateBytes-TowerCompleteFamilyLaunch.DocumentBytes-65536-existing);
                }
            }
            return await Load(ct);
        },async(_,ct)=>{var result=await Produce(t,Full.Value,ct);return change=="controller-result"?result with{Outcome=GoalOutcome.Pass}:result;},stop.Token));
        Assert.True(File.Exists(t.StudyFile("assessment.json")));Assert.True(File.Exists(t.StudyFile(TowerCompleteFamilyRun.FinalFiles)));
        Assert.True(File.Exists(t.ControlFile("run-started.json")));Assert.True(File.Exists(t.ControlFile("run-failure.json")));
        Assert.False(File.Exists(t.ControlFile("run-result.json")));
        // At capacity, admission rejects a new start before CreateNew checks the existing marker.
        if(change=="record-budget")await Assert.ThrowsAsync<InvalidDataException>(()=>Run(t,Full.Value));
        else await Assert.ThrowsAsync<IOException>(()=>Run(t,Full.Value));
    }

    [Fact]
    public async Task Prior_failed_check_blocks_the_outer_run_before_any_study_callback()
    {
        using var t=new Temp();File.WriteAllText(t.ControlFile("check-failure.json"),"{}");
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerCompleteFamilyLaunch.RunOperation(t.Study,t.Control,RequestHash,
            (_,_)=>throw new Exception("No load allowed"),(_,_)=>throw new Exception("No study allowed")));
        Assert.Empty(Directory.GetFiles(t.Study));Assert.True(File.Exists(t.ControlFile("run-failure.json")));
    }

    private sealed class Temp:IDisposable
    {
        private readonly string root=Path.Combine(Path.GetTempPath(),"tower-receipt-tests-"+Guid.NewGuid().ToString("N"));
        internal string Study=>Path.Combine(root,"study");internal string Control=>Path.Combine(root,"control");
        internal string StudyFile(string name)=>Path.Combine(Study,name);internal string ControlFile(string name)=>Path.Combine(Control,name);
        internal Temp()
        {
            Directory.CreateDirectory(Study);Directory.CreateDirectory(Control);
            HarnessJson.WriteNew(ControlFile("check-result.json"),new{status="Complete",requestHash=RequestHash});
        }
        public void Dispose()=>Directory.Delete(root,true);
    }
}
