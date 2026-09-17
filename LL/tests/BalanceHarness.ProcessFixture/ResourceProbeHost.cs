using System.Diagnostics;
using System.Text.Json;

namespace BalanceHarness.ProcessFixture;

public sealed record ResourceProbeSpec(string Version, string Repository, string Output, string Mode, int Seconds, long Bytes);
public sealed record ResourceProbeLaunch(string RequestHash, DateTimeOffset StartedAt, DateTimeOffset Deadline, int ParentId, long ParentStartTicks);
public sealed record ResourceProbePhase(string Name, DateTimeOffset StartedAt, long BaseBytes);
public sealed record ResourceProbePhaseReceipt(string Name, double Seconds, long StartBytes, long EndBytes, long SampledHighWaterBytes);
public sealed record ResourceProbeOutcome(bool Completed, double SecondsAfterPublication, long FinalBytes, long SampledHighWaterBytes, string? Error);

// Test-host-only observation, never an allocation, study, or diagnostic publication.
public static class ResourceProbeHost
{
    public const string Version = "tower-selection-resource-probe-v1";
    // A separately authorized scope. The first attempt's directory remains closed.
    public const string OutputName = "selection-diagnostic-resource-probe-20260917-02";
    public const int MaximumSeconds = 180;
    public const long MaximumBytes = 256L*1048576;
    public const long CloseoutBytes = 4L*1048576;
    public static void Require(bool ok, string message) { if (!ok) throw new InvalidDataException(message); }
    public static ResourceProbeSpec NativeSpec(string repository) => new(Version, Path.GetFullPath(repository),
        Path.Combine(Path.GetFullPath(repository), "TestResults", OutputName), "native", MaximumSeconds, MaximumBytes);

    public static void Validate(ResourceProbeSpec q)
    {
        Require(q.Version == Version && Path.IsPathFullyQualified(q.Output) && q.Seconds is >= 3 and <= MaximumSeconds
            && q.Bytes is >= 8L*1048576 and <= MaximumBytes, "Invalid resource probe limits.");
        if (q.Mode == "native")
        {
            Require(Path.IsPathFullyQualified(q.Repository) && q == NativeSpec(q.Repository)
                && File.Exists(Path.Combine(q.Repository, "LL/tools/BalanceHarness/BalanceHarness.csproj")), "Unrecognized native probe location or caps.");
            var registry = Path.GetFullPath(Path.Combine(q.Repository, "TestResults/balance"));
            Require(!Path.GetFullPath(q.Output).StartsWith(registry+Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Probe cannot enter the allocation registry.");
        }
        else Require(q.Repository == "" && (q.Mode is "literal" or "hang" or "storage" or "combat")
            && Path.GetFileName(Path.GetDirectoryName(q.Output)!).StartsWith("tower-resource-probe-fixture-", StringComparison.Ordinal)
            && Path.GetDirectoryName(Path.GetDirectoryName(q.Output)!) == Path.TrimEndingDirectorySeparator(Path.GetTempPath()), "Only isolated synthetic probe fixtures are supported.");
        for (var p = Path.GetDirectoryName(q.Output); p is not null; p = Path.GetDirectoryName(p))
            Require((File.GetAttributes(p)&FileAttributes.ReparsePoint) == 0, "Linked probe ancestor.");
    }

    public static void SafeArtifact(string name)
    {
        Require(!Path.IsPathRooted(name) && !name.Split('/', '\\').Any(p => p is ".." or "")
            && Path.GetFileName(name) is not ("seed-ledger.json" or "prior-seed-ledger.json" or "history-input.json"), "Authoritative or unsafe probe artifact name.");
    }

    public sealed class Control(ResourceProbeSpec q, ResourceProbeLaunch launch, CancellationToken token)
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double lastScan = double.NegativeInfinity;
        private long bytes, phaseHigh;
        private ResourceProbePhase? phase;
        private double phaseStart;
        public List<ResourceProbePhaseReceipt> Phases { get; } = [];
        public long HighWaterBytes { get; private set; }
        public void Check(bool force = false)
        {
            token.ThrowIfCancellationRequested();
            Require(DateTimeOffset.UtcNow < launch.Deadline.AddSeconds(-2), "Probe deadline reached.");
            if (force || clock.Elapsed.TotalSeconds-lastScan >= .25)
            {
                bytes = TowerBulkCampaign.StorageBytes(q.Output, token); lastScan = clock.Elapsed.TotalSeconds;
                HighWaterBytes = Math.Max(HighWaterBytes, bytes); phaseHigh = Math.Max(phaseHigh, bytes);
                Require(bytes <= q.Bytes-CloseoutBytes, "Probe storage ceiling reached.");
            }
        }
        public void Put(string name, object value, bool replace = false)
        {
            SafeArtifact(name); Check();
            var nested = TowerBulkCampaign.StorageBytes(q.Output, token)-Directory.EnumerateFiles(q.Output).Sum(p => new FileInfo(p).Length);
            new TowerCompleteReservation.Storage(q.Output, q.Bytes-CloseoutBytes-nested).Put(name, value, replace);
            Check();
        }
        public void Enter(string name)
        {
            ClosePhase(); Check(true); phase = new(name, DateTimeOffset.UtcNow, bytes); phaseHigh = bytes; phaseStart = clock.Elapsed.TotalSeconds;
            var path = Path.Combine(q.Output, "current-phase.json");
            new TowerCompleteReservation.Storage(q.Output, q.Bytes-CloseoutBytes-(bytes-Directory.EnumerateFiles(q.Output).Sum(p => new FileInfo(p).Length)))
                .Put("current-phase.json", phase, File.Exists(path));
        }
        public void ClosePhase()
        {
            if (phase is null) return;
            Check(true); var receipt = new ResourceProbePhaseReceipt(phase.Name, clock.Elapsed.TotalSeconds-phaseStart, phase.BaseBytes, bytes, phaseHigh);
            Phases.Add(receipt); phase = null; Put("phase-"+receipt.Name+".json", receipt);
        }
        public long RemainingBytes() { Check(true); return q.Bytes-CloseoutBytes-bytes; }
    }

    public static ProcessStartInfo Start(string command, string argument) => FixtureHost.Start(command, argument);

    public static async Task<ResourceProbeOutcome> RunOwned(ResourceProbeSpec q)
    {
        Validate(q); using var lease = TowerCompactBundle.AcquireWriter(q.Output);
        Require(!Path.Exists(q.Output), "Resource probe output exists; one attempt only, no retry or resume.");
        var clock = Stopwatch.StartNew(); Directory.CreateDirectory(q.Output);
        using var parent = Process.GetCurrentProcess(); var now = DateTimeOffset.UtcNow;
        var launch = new ResourceProbeLaunch(HarnessJson.Hash(q), now, now.AddSeconds(q.Seconds), parent.Id, parent.StartTime.ToUniversalTime().Ticks);
        Process? child = null; IDisposable? registryLease = null;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(q.Seconds-2));
        long peak = 0; double lastScan = double.NegativeInfinity; var sampled = new Dictionary<string, long>(StringComparer.Ordinal);
        void Sample(bool force = false)
        {
            stop.Token.ThrowIfCancellationRequested(); Require(clock.Elapsed.TotalSeconds < q.Seconds-2, "Parent probe deadline reached.");
            if (!force && clock.Elapsed.TotalSeconds-lastScan < .2) return;
            var bytes = TowerBulkCampaign.StorageBytes(q.Output, stop.Token); peak = Math.Max(peak, bytes);
            lastScan = clock.Elapsed.TotalSeconds;
            Require(bytes <= q.Bytes-CloseoutBytes, "Parent probe storage ceiling reached.");
            var p = Path.Combine(q.Output, "current-phase.json");
            if (File.Exists(p)) { var phase = HarnessJson.Read<ResourceProbePhase>(p); sampled[phase.Name] = Math.Max(sampled.GetValueOrDefault(phase.Name), bytes); }
        }
        try
        {
            new TowerCompleteReservation.Storage(q.Output, q.Bytes-CloseoutBytes).Put("probe-request.json", q);
            new TowerCompleteReservation.Storage(q.Output, q.Bytes-CloseoutBytes).Put("probe-launch.json", launch);
            if (q.Mode == "native") registryLease = TowerCompactBundle.AcquireWriter(Path.Combine(q.Repository, "TestResults/balance/complete-family-allocation"));
            child = Process.Start(Start("resource-probe-worker", q.Output)) ?? throw new IOException("Probe worker failed to start.");
            while (!child.HasExited) { Sample(); await Task.Delay(200, stop.Token); }
            Sample(true); Require(child.ExitCode == 0 && !File.Exists(Path.Combine(q.Output, "worker-failure.json")), "Probe worker failed; attempt closed without retry.");
            var worker = HarnessJson.Read<JsonElement>(Path.Combine(q.Output, "worker-observation.json"));
            Require(worker.GetProperty("resourceOnly").GetBoolean() && worker.GetProperty("newFights").GetInt32() == 0
                && worker.GetProperty("newReservations").GetInt32() == 0, "Invalid probe observation.");
            var inventory = new Dictionary<string, string>();
            foreach (var file in TowerBulkCampaign.Paths(q.Output)) { Sample(); inventory.Add(Path.GetRelativePath(q.Output, file).Replace('\\','/'), HarnessJson.FileHash(file)); }
            HarnessJson.WriteNew(Path.Combine(q.Output, "probe-files.json"), inventory); Sample(true);
            HarnessJson.WriteNew(Path.Combine(q.Output, "resource-receipt.json"), new { version = Version, resourceOnly = true,
                status = "ResourceWorkloadMeasured", requestHash = launch.RequestHash, payloadManifestHash = HarnessJson.FileHash(Path.Combine(q.Output,"probe-files.json")),
                measuredSecondsAfterSealing = clock.Elapsed.TotalSeconds, chargedSeconds = q.Seconds, chargedBytes = q.Bytes,
                sampledHighWaterBytesBeforeReceipt = peak, phaseSampledHighWaterBytes = sampled, newFights = 0, newReservations = 0,
                entropyCalls = 0, retries = 0, diagnosticDecision = "NotApplicable", worker });
            Sample(true); var finalBytes = TowerBulkCampaign.StorageBytes(q.Output, default);
            return new(true, clock.Elapsed.TotalSeconds, finalBytes, Math.Max(peak, finalBytes), null);
        }
        catch (Exception error)
        {
            if (child is not null && !child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); }
            var failure = new { version = Version, status = "ResourceProbeFailed", error = error.Message,
                chargedSeconds = q.Seconds, chargedBytes = q.Bytes, measuredSecondsBeforeFailure = clock.Elapsed.TotalSeconds,
                sampledHighWaterBytes = peak, phaseSampledHighWaterBytes = sampled, retries = 0, newFights = 0, newReservations = 0 };
            HarnessJson.WriteNew(Path.Combine(q.Output, "probe-failure.json"), failure);
            return new(false, clock.Elapsed.TotalSeconds, TowerBulkCampaign.StorageBytes(q.Output, default), peak, error.Message);
        }
        finally { child?.Dispose(); registryLease?.Dispose(); }
    }

    public static async Task<int> Worker(string output)
    {
        var q = HarnessJson.Read<ResourceProbeSpec>(Path.Combine(output,"probe-request.json")); Validate(q);
        var launch = HarnessJson.Read<ResourceProbeLaunch>(Path.Combine(output,"probe-launch.json"));
        Require(q.Output == output && launch.RequestHash == HarnessJson.Hash(q) && launch.Deadline == launch.StartedAt.AddSeconds(q.Seconds), "Changed probe launch.");
        using var owner = Process.GetProcessById(launch.ParentId);
        Require(!owner.HasExited && owner.StartTime.ToUniversalTime().Ticks == launch.ParentStartTicks && owner.Id != Environment.ProcessId, "Changed probe owner.");
        var remaining = launch.Deadline.AddSeconds(-2)-DateTimeOffset.UtcNow;
        Require(remaining.TotalSeconds > 0, "Expired probe launch.");
        using var stop = new CancellationTokenSource(remaining);
        await using var watchdog = new Timer(_ => {
            try { if (owner.HasExited || DateTimeOffset.UtcNow >= launch.Deadline.AddSeconds(-2)) Environment.Exit(130); }
            catch (InvalidOperationException) { Environment.Exit(130); }
        }, null, 100, 100);
        var trace = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Resource probe forbids combat."));
        using var guard = trace.Activate(); var control = new Control(q, launch, stop.Token);
        try
        {
            object details;
            if (q.Mode == "native") details = ResourceProbeWork.Run(q, control, stop.Token);
            else
            {
                control.Enter("synthetic");
                control.Put("fixture-ready.json", new { pid = Environment.ProcessId });
                if (q.Mode == "hang") Thread.Sleep(Timeout.Infinite);
                if (q.Mode == "storage") { using var file = File.Create(Path.Combine(output,"overflow.bin")); file.SetLength(q.Bytes+1); Thread.Sleep(Timeout.Infinite); }
                if (q.Mode == "combat") TowerPerformanceTrace.BattleStarted();
                control.Put("literal.json", new { source = "synthetic fixture" }); details = new { synthetic = true };
            }
            control.ClosePhase(); control.Put("worker-observation.json", new { version = Version, resourceOnly = true,
                newFights = 0, newReservations = 0, entropyCalls = 0, details, phases = control.Phases,
                sampledHighWaterBytes = control.HighWaterBytes, timings = trace.Snapshot() });
            return 0;
        }
        catch (Exception error)
        {
            HarnessJson.WriteNew(Path.Combine(output,"worker-failure.json"), new { error = error.ToString(), retries = 0 }); return 2;
        }
    }

    public static async Task<int> Command(string command, string argument)
    {
        if (command == "resource-probe-worker") return await Worker(argument);
        Require(command is "resource-probe-run" or "resource-probe-fixture", "Unknown resource probe command.");
        var q = command == "resource-probe-run" ? NativeSpec(argument) : HarnessJson.Read<ResourceProbeSpec>(argument);
        Require(command != "resource-probe-fixture" || q.Mode != "native", "Fixture command cannot run native work.");
        var result = await RunOwned(q);
        Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return result.Completed ? 0 : 2;
    }
}
