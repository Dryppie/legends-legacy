using System.Diagnostics;
using System.Text.Json;
using BalanceHarness;
using BalanceHarness.ProcessFixture;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessResourceProbeTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "tower-resource-probe-fixture-"+Guid.NewGuid().ToString("N"));
    public BalanceHarnessResourceProbeTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true); // Constructor-owned temporary directory only.
    private ResourceProbeSpec Spec(string mode = "literal", int seconds = 20) => new(ResourceProbeHost.Version,"",Path.Combine(root,"result"),mode,seconds,8L*1048576);

    [Theory]
    [InlineData("seed-ledger.json")]
    [InlineData("payload/history-input.json")]
    [InlineData("../outside.json")]
    [InlineData("payload/prior-seed-ledger.json")]
    public void CannotEmitAuthoritativeHistoryOrEscape(string name) => Assert.Throws<InvalidDataException>(() => ResourceProbeHost.SafeArtifact(name));

    [Fact]
    public void CapsAndModesAreFixed()
    {
        var q = Spec(); ResourceProbeHost.Validate(q);
        Assert.Throws<InvalidDataException>(() => ResourceProbeHost.Validate(q with { Seconds = 181 }));
        Assert.Throws<InvalidDataException>(() => ResourceProbeHost.Validate(q with { Bytes = 257L*1048576 }));
        Assert.Throws<InvalidDataException>(() => ResourceProbeHost.Validate(q with { Mode = "native" }));
        Assert.Throws<InvalidDataException>(() => ResourceProbeHost.Validate(q with { Mode = "resume" }));
    }

    [Fact]
    public void NativeLocationsNormalizeBothSidesBeforeComparison()
    {
        var registry = Path.GetFullPath(Path.Combine(root,"TestResults/balance"));
        var content = Path.GetFullPath(Path.Combine(root,"LL/src/API/API.LL"));
        ResourceProbeWork.ValidateSourceLocations(root,registry,content);
        ResourceProbeWork.ValidateSourceLocations(root,registry.Replace(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),content);
        if (OperatingSystem.IsWindows()) ResourceProbeWork.ValidateSourceLocations(root,registry.ToUpperInvariant(),content.ToUpperInvariant());
        Assert.Throws<InvalidDataException>(() => ResourceProbeWork.ValidateSourceLocations(root,registry+"-other",content));
        Assert.Throws<InvalidDataException>(() => ResourceProbeWork.ValidateSourceLocations(root,registry,Path.Combine(root,"outside")));
    }

    [Fact]
    public void SourcePathsAlsoNormalizeHistoryDictionaryKeys()
    {
        var expected = Path.Combine(root,"TestResults","balance","retained","seed-ledger.json");
        var native = ResourceProbeWork.SourcePath(root,"TestResults/balance/retained");
        var required = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) { [Path.Combine(native,"seed-ledger.json")] = "retained hash" };
        Assert.Equal("retained hash",required[Path.GetFullPath(expected)]);
        Assert.Equal(expected,ResourceProbeWork.SourcePath(root,"TestResults/balance/retained/seed-ledger.json"));
    }

    [Fact]
    public void FreshScopeCannotTargetPreviousAttempt()
    {
        var q = ResourceProbeHost.NativeSpec(root);
        var project = Path.Combine(root,"LL","tools","BalanceHarness","BalanceHarness.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(project)!); File.WriteAllText(project,"Synthetic location fixture; never executed.");
        Directory.CreateDirectory(Path.Combine(root,"TestResults")); ResourceProbeHost.Validate(q);
        Assert.Equal("selection-diagnostic-resource-probe-20260917-02",Path.GetFileName(q.Output));
        Assert.Equal(180,q.Seconds); Assert.Equal(256L*1048576,q.Bytes);
        Assert.Throws<InvalidDataException>(() => ResourceProbeHost.Validate(q with {
            Output = Path.Combine(root,"TestResults","selection-diagnostic-resource-probe-20260917") }));
    }

    [Fact]
    public void RepetitionsAndLabelsCannotBecomeNewEvaluationEvidence()
    {
        var trials = Enumerable.Range(0,1408).Select(i => new LoadoutTrial($"trial-{i+1:D6}",i < 512 ? "discovery" : i < 640 ? "selection" : "confirmation",
            "recipe", i < 640 ? i : 2000+(i-640)%256,"input","cache")).ToArray();
        var plan = ResourceProbeWork.Plan(trials);
        Assert.Equal(4640,plan.Length); Assert.Equal(trials.Take(640),plan.Take(640));
        Assert.Equal(trials[640],plan[1408]); Assert.All(plan, row => Assert.Contains(row,trials));
        var history = Enumerable.Range(0,3000).ToArray();
        var labels = ResourceProbeWork.HistoricalLabels(trials.Skip(640).Select(t => t.Seed),history);
        Assert.Equal(1000,labels.Distinct().Count()); Assert.All(labels, n => Assert.Contains(n,history));
        Assert.Throws<InvalidDataException>(() => ResourceProbeWork.HistoricalLabels([9999],history));
        Assert.Throws<InvalidDataException>(() => ResourceProbeWork.Plan(trials.Skip(1).ToArray()));
    }

    [Fact]
    public async Task LiteralFixtureSealsAndRefusesReuse()
    {
        var q = Spec(); var result = await ResourceProbeHost.RunOwned(q);
        Assert.True(result.Completed,result.Error); Assert.InRange(result.SecondsAfterPublication,0,q.Seconds);
        Assert.InRange(result.FinalBytes,1,q.Bytes); Assert.True(result.SampledHighWaterBytes >= result.FinalBytes);
        var receipt = HarnessJson.Read<JsonElement>(Path.Combine(q.Output,"resource-receipt.json"));
        Assert.Equal("NotApplicable",receipt.GetProperty("diagnosticDecision").GetString());
        Assert.Equal(0,receipt.GetProperty("entropyCalls").GetInt32());
        var files = HarnessJson.Read<Dictionary<string,string>>(Path.Combine(q.Output,"probe-files.json"));
        Assert.Equal(files.Keys.Order(),Directory.EnumerateFiles(q.Output).Select(Path.GetFileName)
            .Where(n => n is not ("probe-files.json" or "resource-receipt.json")).Order());
        foreach (var p in files) Assert.Equal(p.Value,HarnessJson.FileHash(Path.Combine(q.Output,p.Key)));
        await Assert.ThrowsAsync<InvalidDataException>(() => ResourceProbeHost.RunOwned(q));
        Assert.Empty(Directory.EnumerateFiles(q.Output,"seed-ledger.json",SearchOption.AllDirectories));
        Assert.False(File.Exists(Path.Combine(q.Output,"result.json")));
    }

    [Theory]
    [InlineData("combat",20)]
    [InlineData("hang",5)]
    [InlineData("storage",20)]
    public async Task UnsafeWorkerStopsWithoutPublication(string mode,int seconds)
    {
        var q = Spec(mode,seconds); var elapsed = Stopwatch.StartNew();
        var result = await ResourceProbeHost.RunOwned(q);
        Assert.False(result.Completed); Assert.True(elapsed.Elapsed.TotalSeconds < seconds+5);
        Assert.True(File.Exists(Path.Combine(q.Output,"probe-failure.json")));
        Assert.False(File.Exists(Path.Combine(q.Output,"resource-receipt.json")));
        if (mode == "combat") Assert.Contains("Resource probe forbids combat", File.ReadAllText(Path.Combine(q.Output,"worker-failure.json")));
        if (mode == "storage") Assert.Contains("storage ceiling",result.Error);
        var ready = HarnessJson.Read<JsonElement>(Path.Combine(q.Output,"fixture-ready.json"));
        try { using var child = Process.GetProcessById(ready.GetProperty("pid").GetInt32()); Assert.True(child.HasExited); }
        catch (ArgumentException) { }
    }

    [Fact]
    public async Task WorkerExitsAfterOwnedParentDies()
    {
        var q = Spec("hang",20); var request = Path.Combine(root,"fixture.json"); HarnessJson.WriteNew(request,q);
        using var parent = Process.Start(ResourceProbeHost.Start("resource-probe-fixture",request))!;
        Process? worker = null;
        try
        {
            var marker = Path.Combine(q.Output,"fixture-ready.json"); var clock = Stopwatch.StartNew();
            while (!File.Exists(marker) && clock.Elapsed.TotalSeconds < 10) await Task.Delay(25);
            Assert.True(File.Exists(marker)); worker = Process.GetProcessById(HarnessJson.Read<JsonElement>(marker).GetProperty("pid").GetInt32());
            parent.Kill(); await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(File.Exists(Path.Combine(q.Output,"resource-receipt.json")));
        }
        finally
        {
            if (!parent.HasExited) { parent.Kill(true); await parent.WaitForExitAsync(); }
            if (worker is not null) { if (!worker.HasExited) { worker.Kill(true); await worker.WaitForExitAsync(); } worker.Dispose(); }
        }
    }
}
