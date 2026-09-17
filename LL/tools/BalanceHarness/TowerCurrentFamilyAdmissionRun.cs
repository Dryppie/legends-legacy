using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerCurrentAdmissionRequest(string Version, string DesignRoot, string ContentRoot,
    string ExecutionHash, int MaximumSeconds, long MaximumBytes);
public sealed record TowerCurrentAdmissionProducer(string Version, string DesignManifest, ExecutionIdentity Execution,
    string ExecutionHash, int PreparationSentinel, bool PreparationOnly, bool CombatScheduleCreated);

public static partial class TowerCurrentFamilyAdmission
{
    public const string DesignManifest = "bf454b4228637406847c10186e75a277bb47d825cc77a10fa6011221adf656b6";
    public const int MaximumSeconds = 1800;
    public const long MaximumBytes = 2L*1024*1024*1024;
    private static readonly string[] Inputs = ["target-template.json", "cells.jsonl.gz", "entries.jsonl.gz",
        "incompatibilities.json", "inventory-summary.json"];

    internal static void ValidateRequest(TowerCurrentAdmissionRequest q)
        => Require(q.Version == Version && !string.IsNullOrWhiteSpace(q.DesignRoot) && !string.IsNullOrWhiteSpace(q.ContentRoot)
            && TowerContractJson.Hash(q.ExecutionHash) && q.MaximumSeconds is > 0 and <= MaximumSeconds
            && q.MaximumBytes is >= 1048576 and <= MaximumBytes, "Invalid current-family admission request or limits.");

    internal static void Guard(double seconds, long bytes, int secondsLimit, long bytesLimit, bool reserve = true)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || seconds >= secondsLimit) throw new TimeoutException("Admission deadline.");
        if (bytes < 0 || bytes > bytesLimit-(reserve ? 65536 : 0)) throw new IOException("Admission output limit.");
    }

    private static bool Inside(string path, string root) => Path.GetFullPath(path).Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)
        || Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))+Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static void Unlinked(string path)
    {
        for (var p = Path.GetFullPath(path); p is not null; p = Path.GetDirectoryName(p))
            if (Path.Exists(p)) Require((File.GetAttributes(p)&FileAttributes.ReparsePoint)==0, "Linked admission path.");
    }

    private static void CheckDesign(string root, string manifestName, Action check)
    {
        var manifestPath = Path.Combine(root, manifestName); Unlinked(manifestPath);
        Require(HarnessJson.FileHash(manifestPath)==DesignManifest, "Changed completed calibration design.");
        var manifest = HarnessJson.Read<Dictionary<string,string>>(manifestPath);
        foreach (var name in Inputs)
        {
            check(); var path = Path.Combine(root,name); Unlinked(path);
            Require(manifest.TryGetValue(name,out var digest) && HarnessJson.FileHash(path)==digest, "Changed admission input: "+name);
        }
    }

    internal static async Task<TowerCurrentAdmissionInventory> Load(string root, Action check, CancellationToken token)
    {
        var template = TowerContractJson.Read<TowerScenario>(Path.Combine(root,"target-template.json"));
        async Task<List<T>> Read<T>(string name)
        {
            var list = new List<T>();
            using var file = File.OpenRead(Path.Combine(root,name)); using var zip = new GZipStream(file,CompressionMode.Decompress);
            await foreach (var row in ReadLines<T>(zip,token)) { check(); list.Add(row); Require(list.Count<=51624,"Extra input rows."); }
            return list;
        }
        var cells = await Read<TowerCurrentAdmissionCell>("cells.jsonl.gz");
        var entries = await Read<TowerCurrentAdmissionEntry>("entries.jsonl.gz");
        var required = new[] {
            "399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b",
            "8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50",
            "6825709c4bcd84b0c72c60c552530007bed67128eb518c7c721d7a0188e0172b" };
        Require(cells.Count==46077 && entries.Count==51624 && cells.Count(c=>c.AnchorReasons.Count>0)==973,
            "Changed designed coverage or mandatory controls.");
        var exceptions = TowerContractJson.Read<TowerCurrentAdmissionEntry[]>(Path.Combine(root,"incompatibilities.json"));
        Require(exceptions.Length==162 && HarnessJson.Hash(exceptions)==HarnessJson.Hash(entries.Where(e=>e.Classification=="Incompatible").ToArray()),
            "Context exceptions must be retained exactly.");
        var inventory = new TowerCurrentAdmissionInventory(template,cells,entries,required);
        ValidateInventory(inventory); check(); return inventory;
    }

    internal static void ValidateProducer(TowerCurrentAdmissionRequest request, TowerCurrentAdmissionProducer producer, JsonElement summary)
    {
        ValidateRequest(request);
        Require(producer.Version==Version && producer.DesignManifest==DesignManifest && producer.PreparationSentinel==0
            && producer.PreparationOnly && !producer.CombatScheduleCreated
            && producer.ExecutionHash==request.ExecutionHash && HarnessJson.Hash(producer.Execution)==request.ExecutionHash,
            "Unbound producing executable or changed preparation-only contract.");
        Require(producer.Execution is not null && !string.IsNullOrWhiteSpace(producer.Execution.Runtime)
            && !string.IsNullOrWhiteSpace(producer.Execution.OperatingSystem) && !string.IsNullOrWhiteSpace(producer.Execution.Architecture)
            && producer.Execution.AssemblyHashes is not null
            && producer.Execution.AssemblyHashes.Keys.Order(StringComparer.Ordinal).SequenceEqual(new[] {"Application","BalanceHarness","Common","Domain","Services.LL"})
            && producer.Execution.AssemblyHashes.Values.All(TowerContractJson.Hash), "Incomplete producing execution identity.");
        var baseline = summary.GetProperty("execution").GetProperty("assemblyHashes");
        foreach (var name in new[] {"Application","Common","Domain","Services.LL"})
            Require(producer.Execution.AssemblyHashes.TryGetValue(name,out var digest) && digest==baseline.GetProperty(name).GetString(),
                "Changed baseline gameplay assembly: "+name);
    }

    private static TowerCurrentAdmissionProducer Producer(TowerCurrentAdmissionRequest request)
        => new(Version,DesignManifest,ExecutionIdentity.Current(),request.ExecutionHash,0,true,false);

    private static void CheckRuntime(TowerCurrentAdmissionRequest request, JsonElement summary, Action check)
    {
        ValidateProducer(request,Producer(request),summary);
        Require(HarnessJson.Hash(TowerBundle.ReadSettings(request.ContentRoot))==summary.GetProperty("settingsHash").GetString(),
            "Changed baseline settings.");
        foreach (var p in summary.GetProperty("contentHashes").EnumerateObject())
        {
            check(); var path=Path.Combine(request.ContentRoot,"Data",p.Name); Unlinked(path);
            Require(HarnessJson.FileHash(path)==p.Value.GetString(),"Changed baseline content: "+p.Name);
        }
    }

    public static async Task<TowerCurrentAdmissionResult> Run(string requestPath, string output,
        CancellationToken token = default, Action<string>? progress = null)
    {
        var clock=Stopwatch.StartNew(); token.ThrowIfCancellationRequested(); Unlinked(requestPath);
        var request=TowerContractJson.Read<TowerCurrentAdmissionRequest>(requestPath); ValidateRequest(request);
        output=Path.GetFullPath(output); Unlinked(output); Unlinked(request.DesignRoot); Unlinked(request.ContentRoot);
        Require(!Inside(output,request.DesignRoot) && !Inside(request.DesignRoot,output)
            && !Inside(output,request.ContentRoot) && !Inside(request.ContentRoot,output) && !Inside(requestPath,output),
            "Output overlaps admission inputs.");
        using var lease=TowerCompactBundle.AcquireWriter(output);
        if (Path.Exists(output)) throw new IOException("Use new admission output; no retry or resume.");
        Directory.CreateDirectory(output);
        using var stop=CancellationTokenSource.CreateLinkedTokenSource(token);
        stop.CancelAfter(TimeSpan.FromSeconds(request.MaximumSeconds)); var ct=stop.Token;
        using var trace=new TowerPerformanceTrace(_=>throw new InvalidOperationException("Admission cannot execute combat.")).Activate();
        void Check(bool reserve=true)
        {
            ct.ThrowIfCancellationRequested();
            var files=Directory.GetFiles(output);
            foreach(var file in files) Unlinked(file);
            Guard(clock.Elapsed.TotalSeconds,files.Sum(p=>new FileInfo(p).Length),request.MaximumSeconds,request.MaximumBytes,reserve);
        }
        void Save(string name, object value)
        {
            var bytes=JsonSerializer.SerializeToUtf8Bytes(value,HarnessJson.Options);
            Guard(clock.Elapsed.TotalSeconds,Directory.GetFiles(output).Sum(p=>new FileInfo(p).Length)+bytes.Length,
                request.MaximumSeconds,request.MaximumBytes);
            using var file=new FileStream(Path.Combine(output,name),FileMode.CreateNew,FileAccess.Write);
            file.Write(bytes); file.Flush(true);
        }
        void Copy(string source, string name)
        {
            Guard(clock.Elapsed.TotalSeconds,Directory.GetFiles(output).Sum(p=>new FileInfo(p).Length)+new FileInfo(source).Length,
                request.MaximumSeconds,request.MaximumBytes);
            File.Copy(source,Path.Combine(output,name)); Check();
        }
        try
        {
            Save("request.json",request);
            Save("started.json",new { version=Version,startedUtc=DateTimeOffset.UtcNow,fights=0,newSeeds=0 });
            CheckDesign(request.DesignRoot,"files.json",()=>Check());
            Copy(Path.Combine(request.DesignRoot,"files.json"),"design-files.json");
            foreach(var name in Inputs) Copy(Path.Combine(request.DesignRoot,name),name);
            CheckDesign(output,"design-files.json",()=>Check());
            var summary=HarnessJson.Read<JsonElement>(Path.Combine(output,"inventory-summary.json"));
            CheckRuntime(request,summary,()=>Check());
            Save("producing.json",Producer(request));
            var inventory=await Load(output,()=>Check(),ct);
            var settings=TowerBundle.ReadSettings(request.ContentRoot);
            var runner=new TowerBattleRunner(request.ContentRoot,new OfflineContent(request.ContentRoot,settings.Threat));
            TowerCurrentAdmissionResult result; var preparations=0;
            var rowLimit=request.MaximumBytes-65536-Directory.GetFiles(output).Sum(p=>new FileInfo(p).Length);
            using(var file=new FileStream(Path.Combine(output,"rows.jsonl.gz"),FileMode.CreateNew,FileAccess.Write,FileShare.Read))
            {
                using var bounded=new RowStream(file,rowLimit,()=> { ct.ThrowIfCancellationRequested();
                    Guard(clock.Elapsed.TotalSeconds,0,request.MaximumSeconds,request.MaximumBytes); });
                await using(var zip=new GZipStream(bounded,CompressionLevel.Fastest,true))
                    result=await Scan(inventory,zip,async(s, cancellation)=> {
                        // Same fixed preparation sentinel as existing seedless checks. No RNG fill, ledger or combat schedule.
                        var input=runner.CreateInput(s with { Seeds=[0] },0,settings.Threat,settings.CheckpointIntervalTicks);
                        preparations++;
                        var participants=IdleBattleRunner.DescribeParticipants(await runner.PrepareAsync(input,cancellation));
                        return (HarnessJson.Hash(input),participants);
                    },()=>Check(),ct,progress);
                file.Flush(true);
            }
            Check();
            TowerCurrentAdmissionResult audit;
            using(var file=File.OpenRead(Path.Combine(output,"rows.jsonl.gz")))
            using(var zip=new GZipStream(file,CompressionMode.Decompress))
                audit=await Audit(inventory,zip,()=>Check(),ct);
            Require(HarnessJson.Hash(result)==HarnessJson.Hash(audit),"Saved admission audit differs.");
            CheckRuntime(request,summary,()=>Check()); CheckDesign(output,"design-files.json",()=>Check());
            Save("result.json",result);
            Save("audit.json",audit);
            Save("performance.json",new { secondsBeforeSeal=clock.Elapsed.TotalSeconds,
                fights=0,newSeeds=0,preparations,projectionCells=result.Cells });
            Check();
            var files=Directory.GetFiles(output).ToDictionary(p=>Path.GetFileName(p),p=>HarnessJson.FileHash(p));
            Save("files.json",files);
            TowerBulkCampaign.VerifyFiles(output,"files.json",true,ct); Check(false);
            return result;
        }
        catch(Exception e)
        {
            // Small reserved closeout; leave partial rows and every copied input for diagnosis.
            var message=e.GetType().Name+": "+e.Message;
            HarnessJson.WriteNew(Path.Combine(output,"failure.json"),new { version=Version,status="Stopped",error=message[..Math.Min(message.Length,2048)],
                elapsedSeconds=clock.Elapsed.TotalSeconds,fights=0,newSeeds=0,noRetry=true });
            throw;
        }
    }

    internal sealed class RowStream(Stream inner, long maximumBytes, Action checkpoint) : Stream
    {
        private void Reserve(int count) { checkpoint(); if(count<0 || inner.Length>maximumBytes-count) throw new IOException("Admission row output limit."); }
        public override bool CanRead=>false;
        public override bool CanSeek=>false;
        public override bool CanWrite=>true;
        public override long Length=>inner.Length;
        public override long Position { get=>inner.Position; set=>throw new NotSupportedException(); }
        public override void Flush()=>inner.Flush();
        public override Task FlushAsync(CancellationToken ct)=>inner.FlushAsync(ct);
        public override void Write(byte[] b,int offset,int count) { Reserve(count); inner.Write(b,offset,count); }
        public override void Write(ReadOnlySpan<byte> b) { Reserve(b.Length); inner.Write(b); }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> b,CancellationToken ct=default)
        { ct.ThrowIfCancellationRequested(); Reserve(b.Length); return inner.WriteAsync(b,ct); }
        public override int Read(byte[] b,int offset,int count)=>throw new NotSupportedException();
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
    }

    public static async Task<TowerCurrentAdmissionResult> Verify(string output, CancellationToken token=default)
    {
        Unlinked(output); Require(!File.Exists(Path.Combine(output,"failure.json")),"Stopped admission cannot verify as complete.");
        using var limit=CancellationTokenSource.CreateLinkedTokenSource(token); limit.CancelAfter(TimeSpan.FromSeconds(MaximumSeconds));
        var ct=limit.Token;
        TowerBulkCampaign.VerifyFiles(output,"files.json",true,ct);
        CheckDesign(output,"design-files.json",()=>ct.ThrowIfCancellationRequested());
        var request=TowerContractJson.Read<TowerCurrentAdmissionRequest>(Path.Combine(output,"request.json"));
        ValidateProducer(request,TowerContractJson.Read<TowerCurrentAdmissionProducer>(Path.Combine(output,"producing.json")),
            HarnessJson.Read<JsonElement>(Path.Combine(output,"inventory-summary.json")));
        Guard(0,Directory.GetFiles(output).Sum(p=>new FileInfo(p).Length),request.MaximumSeconds,request.MaximumBytes,false);
        var inventory=await Load(output,()=>ct.ThrowIfCancellationRequested(),ct);
        using var file=File.OpenRead(Path.Combine(output,"rows.jsonl.gz")); using var zip=new GZipStream(file,CompressionMode.Decompress);
        var audit=await Audit(inventory,zip,()=>ct.ThrowIfCancellationRequested(),ct);
        Require(HarnessJson.Hash(audit)==HarnessJson.Hash(HarnessJson.Read<TowerCurrentAdmissionResult>(Path.Combine(output,"result.json")))
            && HarnessJson.Hash(audit)==HarnessJson.Hash(HarnessJson.Read<TowerCurrentAdmissionResult>(Path.Combine(output,"audit.json"))),
            "Changed saved admission result.");
        return audit;
    }

    public static async Task<int> Command(string[] args, CancellationToken token=default)
    {
        var result=args switch {
            ["tower-current-family-admit",var request,var output] => await Run(request,output,token,Console.WriteLine),
            ["tower-current-family-admission-verify",var output] => await Verify(output,token),
            _ => throw new InvalidDataException("Use tower-current-family-admit <request.json> <new-output> or tower-current-family-admission-verify <completed-output>. No combat, allocation or resume.") };
        Console.WriteLine(JsonSerializer.Serialize(result,HarnessJson.Options));
        return result.ReadyForFamilyFreeze ? 0 : result.Invalid>0 ? 1 : 3;
    }
}
