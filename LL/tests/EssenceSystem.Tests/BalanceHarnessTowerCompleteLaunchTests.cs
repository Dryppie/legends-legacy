using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessTowerCompleteLaunchTests
{
    private static string Hash(object v) => HarnessJson.Hash(v);
    private static TowerCompleteProtocol Protocol => new(TowerCompleteFamily.Version, "fixture-source", TowerCompleteFamily.SourceSeal,
        TowerCompleteFamilyInputs.HarnessHash, Hash(ExecutionIdentity.Current()), TowerCompleteFamily.MaximumFights, TowerCompleteFamily.MaximumSeconds,
        TowerCompleteFamily.MaximumBytes, 0, TowerStorageAccountant.Mode, 2000, 1000000,
        new Dictionary<string,string>{{"fixture-setup",Hash("setup")}}, new Dictionary<string,string>{{"fixture-history",Hash("history")}},
        new Dictionary<string,string>{{"fixture-input",Hash("input")}});
    private static TowerCompleteRunContext Context => TowerCompleteFamilyLaunch.Envelope(Protocol,
        new(Protocol.HarnessHash, Protocol.ExecutionHash), Hash("request"), 123, 456);

    [Fact]
    public void Extension_deducts_all_late_costs_without_rewriting_reservation()
    {
        var p = Protocol; var before = Hash(p); var c = Context;
        Assert.Equal(4523, c.SetupSeconds); Assert.Equal(p.MaximumBytes-p.PriorSetupBytes-456-67108864, c.StudyBytes);
        Assert.Equal(before, Hash(p)); Assert.Equal(p.MaximumAttempts, c.Protocol.MaximumAttempts);
        Assert.Equal(2400, TowerCompleteFamilyLaunch.LateSeconds); Assert.Equal(0, c.Protocol.Retries);
    }

    [Theory]
    [InlineData("negative-seconds")] [InlineData("nan-seconds")] [InlineData("exhausted-seconds")]
    [InlineData("negative-bytes")] [InlineData("overflow-bytes")] [InlineData("exhausted-bytes")] [InlineData("hash")]
    public void Extension_rejects_missing_identity_and_exhausted_or_invalid_budgets(string change)
    {
        var p = Protocol;
        var seconds = change switch { "negative-seconds" => -1, "nan-seconds" => double.NaN, "exhausted-seconds" => 84000, _ => 1d };
        var bytes = change switch { "negative-bytes" => -1, "overflow-bytes" => long.MaxValue, "exhausted-bytes" => p.MaximumBytes, _ => 1L };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyLaunch.Envelope(p, new(p.HarnessHash,p.ExecutionHash), change=="hash"?"bad":Hash("q"), seconds, bytes));
    }

    [Fact]
    public void Only_the_harness_may_change_and_legacy_identity_remains_strict()
    {
        var current = ExecutionIdentity.Current(); var hashes = current.AssemblyHashes.ToDictionary(x=>x.Key,x=>x.Value);
        hashes["BalanceHarness"] = Hash("previous-harness"); var p = Protocol with { HarnessHash = hashes["BalanceHarness"], ExecutionHash = Hash(current with { AssemblyHashes = hashes }) };
        var identity = TowerCompleteFamilyLaunch.PreviousIdentity(p, current);
        TowerCompleteFamilyInputs.Limits(p, identity);
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyInputs.Limits(p));
    }

    [Theory]
    [InlineData("runtime")] [InlineData("os")] [InlineData("architecture")] [InlineData("Application")]
    [InlineData("Common")] [InlineData("Domain")] [InlineData("Services.LL")] [InlineData("missing-harness")]
    public void Captured_gameplay_and_environment_changes_are_rejected(string change)
    {
        var e = ExecutionIdentity.Current(); var hashes = e.AssemblyHashes.ToDictionary(x=>x.Key,x=>x.Value);
        if (change == "missing-harness") hashes.Remove("BalanceHarness");
        else if (hashes.ContainsKey(change)) hashes[change] = Hash("other");
        e = e with { AssemblyHashes = hashes, Runtime = change=="runtime"?"other":e.Runtime,
            OperatingSystem = change=="os"?"other":e.OperatingSystem, Architecture = change=="architecture"?"other":e.Architecture };
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyLaunch.PreviousIdentity(Protocol, e));
    }

    private static Dictionary<string,object> Start(TowerCompleteRunContext c) => new() {
        ["protocolHash"]=Hash("protocol"), ["maximum"]=c.Protocol.MaximumAttempts, ["capacity"]=TowerCompleteFamily.MaximumSecondCells,
        ["seconds"]=c.Protocol.MaximumSeconds, ["setup"]=c.SetupSeconds, ["bytes"]=c.StudyBytes, ["launchHash"]=c.LaunchHash! };

    [Theory]
    [InlineData("protocolHash")] [InlineData("launchHash")] [InlineData("maximum")] [InlineData("capacity")]
    [InlineData("seconds")] [InlineData("setup")] [InlineData("bytes")] [InlineData("missing-launch")]
    public void Verification_binds_the_extended_start_and_rejects_legacy_budget_substitution(string change)
    {
        var c = Context; var s = Start(c); TowerCompleteFamilyRun.VerifyStart(JsonSerializer.SerializeToElement(s), Hash("protocol"), c);
        if(change=="missing-launch") s.Remove("launchHash"); else s[change]=change.Contains("Hash")?Hash("tampered"):0;
        Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyRun.VerifyStart(JsonSerializer.SerializeToElement(s),Hash("protocol"),c));
    }

    [Fact]
    public void Legacy_start_remains_valid_only_under_legacy_context()
    {
        var p = Protocol; var c = new TowerCompleteRunContext(p,p.PriorSetupSeconds,p.MaximumBytes-p.PriorSetupBytes);
        var s = Start(c); s.Remove("launchHash"); TowerCompleteFamilyRun.VerifyStart(JsonSerializer.SerializeToElement(s),Hash("protocol"),c);
        s["launchHash"]=Hash("request"); Assert.Throws<InvalidDataException>(() => TowerCompleteFamilyRun.VerifyStart(JsonSerializer.SerializeToElement(s),Hash("protocol"),c));
    }

    private static TowerCompleteLaunchRequest Request(Temp t) => new(TowerCompleteFamilyLaunch.Version,t.P("study"),t.P("reservation"),t.P("producing"),Hash("seal"),
        TowerCompleteFamilyInputs.HarnessHash,Hash(ExecutionIdentity.Current()),1,new Dictionary<string,string>{{t.P("input.json"),Hash("fixture")}},t.P("previous-launch"));

    [Fact]
    public void Request_binds_raw_bytes_without_self_reference()
    {
        using var t = new Temp(); var path=t.P("control/launch-request.json");HarnessJson.WriteNew(path,Request(t));
        var before=TowerCompleteFamilyLaunch.ReadRequest(path); Assert.Equal(HarnessJson.FileHash(path),before.Hash);
        File.AppendAllText(path," ");var after=TowerCompleteFamilyLaunch.ReadRequest(path);
        Assert.NotEqual(before.Hash,after.Hash);Assert.Equal(Hash(before.Request),Hash(after.Request));
    }

    [Theory]
    [InlineData("version")] [InlineData("harness")] [InlineData("execution")] [InlineData("seconds")]
    [InlineData("self")] [InlineData("alias")] [InlineData("inside-study")] [InlineData("inside-control")]
    public void Request_rejects_drift_aliases_and_overlapping_accounting(string change)
    {
        using var t=new Temp();var q=Request(t);var path=t.P("control/launch-request.json");
        q=change switch {
            "version"=>q with {Version="other"}, "harness"=>q with{HarnessHash=Hash("other")}, "execution"=>q with{ExecutionHash=Hash("other")},
            "seconds"=>q with{AdditionalSetupSeconds=-1}, "inside-study"=>q with{StudyRoot=t.Root}, "inside-control"=>q with{StudyRoot=t.P("control/study")},
            _=>q with{AdditionalFiles=new Dictionary<string,string>{{change=="self"?path:t.P("control/../control/launch-request.json"),Hash("self")}}}};
        HarnessJson.WriteNew(path,q);Assert.Throws<InvalidDataException>(()=>TowerCompleteFamilyLaunch.ReadRequest(path));
    }

    [Fact]
    public void Control_writes_are_bounded_and_failure_space_is_preserved()
    {
        using var t=new Temp();var root=t.P("control");
        using(var f=File.Create(t.P("control/fill.bin"))) f.SetLength(TowerCompleteFamilyLaunch.LateBytes-TowerCompleteFamilyLaunch.DocumentBytes-65536);
        Assert.Throws<InvalidDataException>(()=>TowerCompleteFamilyLaunch.PublishControl(root,"result.json",new{status="complete"}));
        Assert.False(File.Exists(t.P("control/result.json")));
        TowerCompleteFamilyLaunch.PublishControl(root,"failure.json",new{status="failed"},true);
        Assert.Throws<IOException>(()=>TowerCompleteFamilyLaunch.PublishControl(root,"failure.json",new{status="failed"},true));
    }

    [Fact]
    public async Task Failed_check_is_durable_forbids_retry_and_never_touches_the_study()
    {
        using var t=new Temp();var path=t.P("control/launch-request.json");HarnessJson.WriteNew(path,Request(t));
        File.WriteAllText(t.P("study/protocol.json"),"{}");var before=HarnessJson.FileHash(t.P("study/protocol.json"));
        await Assert.ThrowsAsync<InvalidDataException>(()=>TowerCompleteFamilyLaunch.Check(path));
        Assert.True(File.Exists(t.P("control/check-started.json")));Assert.True(File.Exists(t.P("control/check-failure.json")));
        await Assert.ThrowsAsync<IOException>(()=>TowerCompleteFamilyLaunch.Check(path));
        Assert.Equal(before,HarnessJson.FileHash(t.P("study/protocol.json")));Assert.Single(Directory.GetFiles(t.P("study")));
    }

    [Fact]
    public async Task Shared_controller_durably_binds_launch_costs_before_cancelled_preflight()
    {
        using var t=new Temp();var root=t.P("study");File.WriteAllText(t.P("study/protocol.json"),"{}");
        using var stop=new CancellationTokenSource();var hash=Hash("request");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>TowerCompleteFamilyRun.Execute(root,288,1,12,60,4*1048576,
            ct=>{var start=HarnessJson.Read<JsonElement>(t.P("study/started.json"));Assert.Equal(hash,start.GetProperty("launchHash").GetString());
                Assert.Equal(12,start.GetProperty("setup").GetDouble());stop.Cancel();ct.ThrowIfCancellationRequested();return Task.FromResult<IReadOnlyList<TowerConfirmationCell>>([]);},
            (_,_,_,_)=>throw new Exception("No batch permitted"),(_,_,_)=>throw new Exception("No batch permitted"),(_,_,_)=>Task.CompletedTask,stop.Token,launchHash:hash));
        Assert.Equal(0,HarnessJson.Read<JsonElement>(t.P("study/failure.json")).GetProperty("started").GetInt32());
        Assert.False(File.Exists(t.P("study/complete-family-files.json")));
    }

    private sealed class Temp : IDisposable
    {
        internal string Root {get;}=Path.Combine(Path.GetTempPath(),"tower-launch-tests-"+Guid.NewGuid().ToString("N"));
        internal Temp(){Directory.CreateDirectory(P("control"));Directory.CreateDirectory(P("study"));}
        internal string P(string name)=>Path.Combine(Root,name);
        public void Dispose()=>Directory.Delete(Root,true);
    }
}
