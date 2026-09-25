using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness.ProcessFixture;

public sealed record ProposalAuditProbeRequest(string Version, string Mode, string Repository, string Output,
    DateTimeOffset StartedAt, DateTimeOffset Deadline, int ParentId, long ParentStartTicks, string HostHash);
public sealed record ProposalAuditProbePhase(string Name, double Seconds, long Items, string EvidenceHash);

/// <summary>Read-only resource work in a separate executable. Never an admission,
/// allocation, combat runner, campaign, or interpretation of synthetic outcomes.</summary>
public static class ProposalAuditCostProbe
{
    public const string Version = "proposal-native-audit-cost-probe-v1";
    public const int Seconds = 600, WorkerSeconds = 480;
    public const long Bytes = 256L * 1048576;
    public const string ReviewPin = "b2cd5249373b58069af6000579aa2d73c971999adc74b5d57e19a0369b8c28d2";
    public const string HistoricalPin = "5fd7e9eb38eb4319f41be7a0e874297a897c7a32a73d09030261579b2473b377";
    public const string HarnessPin = "015b8e06535543414d497ccdfb35f647b534f24fe655cc4732a845e082aa4a9b";
    public const string ExecutionHash = "c0657716db5c6303edf6fbe14f5b8853095cb57f12be1b9428580c71dec4a513";
    public const string OutputName = "proposal-native-audit-cost-probe-20260923";
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsProcessInJob(IntPtr process, IntPtr job, out bool result);
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidDataException(reason); }
    private static string P(string root, string name) => Path.Combine(root, name);

    public static void Validate(ProposalAuditProbeRequest q)
    {
        Require(q.Version == Version && Path.IsPathFullyQualified(q.Output) && Path.IsPathFullyQualified(q.Repository)
            && q.Deadline == q.StartedAt.AddSeconds(WorkerSeconds) && q.ParentId > 0 && q.ParentStartTicks > 0
            && TowerContractJson.Hash(q.HostHash), "Invalid fixed probe contract.");
        if (q.Mode == "native")
            Require(string.Equals(Path.GetFullPath(q.Output), Path.GetFullPath(P(q.Repository, "TestResults/" + OutputName)),
                StringComparison.OrdinalIgnoreCase), "Wrong native probe output.");
        else Require(q.Mode is "literal" or "hang" or "storage" or "combat"
            && Path.GetFileName(Path.GetDirectoryName(q.Output)!).StartsWith("proposal-audit-cost-test-", StringComparison.Ordinal)
            && Path.GetDirectoryName(Path.GetDirectoryName(q.Output)!) == Path.TrimEndingDirectorySeparator(Path.GetTempPath()), "Use isolated literal fixtures.");
        TowerProposalStudy.Unlinked(q.Output); TowerProposalStudy.Unlinked(q.Repository);
    }

    // Every value comes from the historical union. These deliberately reused
    // panels are resource fixtures and cannot satisfy scientific allocation Bind.
    public static TowerProposalRacingPlan[] Workloads(TowerProposalContext context, TowerProposalComparisonPlan design, int[] history)
    {
        Require(history.SequenceEqual(history.Distinct().Order()), "Unordered resource history.");
        var legacy = TowerProposalStudy.LegacySeeds(context).ToHashSet();
        var forbidden = legacy.Concat(context.Scope.References.SelectMany(r => r.Scenario.Seeds)).ToHashSet();
        var values = history.Where(v => !forbidden.Contains(v)).Take(876).ToArray();
        Require(values.Length == 876, "Missing historical resource values.");
        var result = new List<TowerProposalRacingPlan>();
        string[] roles = ["wave-1-screen", "wave-1-continuation", "wave-2-screen", "wave-2-continuation", "selection"];
        for (var root = 0; root < 12; root++)
        {
            var block = values.Skip(root*73).Take(73).ToArray();
            var scope = context.Scope with { Id = "resource-only-historical-reuse", Generation = context.Scope.Generation with { Seeds = [block[0]] },
                ExcludedCombatSeeds = history.Except(legacy).Except(block).ToArray() };
            var racing = new TowerAdaptiveRacingPlan(TowerAdaptiveRacing.Version, scope, context.Mechanics, context.BenchmarkReferenceId,
                block[0], roles.Select((role, i) => new TowerRacingPanel(role, block.Skip(1+i*8).Take(i == 4 ? 40 : 8).ToArray())).ToArray(), 528);
            foreach (var policy in new[] { design.Control, design.Candidate })
                result.Add(new(TowerProposalPolicies.DamageRacingVersion, racing, policy, context.DamageAffinityInventory));
        }
        return result.ToArray();
    }

    public static TowerPanelOutcome Literal(TowerPanelTrial request) => new(HarnessJson.Hash(request), $"trial-{request.Ordinal:D6}",
        request.Seed, BattleOutcome.Defeat, 100, 0, 1);

    public static void ValidateSourceCompatibility(ExecutionIdentity current, LoadoutScope archived, TowerProposalContext context)
    {
        var before = archived.Execution with { AssemblyHashes = archived.Execution.AssemblyHashes.Where(p => p.Key != "BalanceHarness").ToDictionary() };
        var after = current with { AssemblyHashes = current.AssemblyHashes.Where(p => p.Key != "BalanceHarness").ToDictionary() };
        Require(HarnessJson.Hash(before) == HarnessJson.Hash(after) && archived.Execution.AssemblyHashes.ContainsKey("BalanceHarness")
            && current.AssemblyHashes.ContainsKey("BalanceHarness") && HarnessJson.Hash(archived.Settings) == context.Scope.SettingsHash
            && HarnessJson.Hash(archived.ContentHashes) == HarnessJson.Hash(context.Scope.ContentHashes), "Unqualified historical input compatibility.");
    }

    private static object SourceProof(string output, string repository)
    {
        var assembly = typeof(ProposalAuditCostProbe).Assembly.Location;
        using var peStream = File.OpenRead(assembly); using var pdbStream = File.OpenRead(Path.ChangeExtension(assembly, ".pdb"));
        using var pe = new PEReader(peStream); using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var reader = provider.GetMetadataReader(); var guid = new Guid(reader.DebugMetadataHeader!.Id.Take(16).ToArray());
        Require(pe.ReadCodeViewDebugDirectoryData(pe.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.CodeView)).Guid == guid,
            "Probe symbols do not match executable.");
        var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var handle in reader.Documents)
        {
            var document = reader.GetDocument(handle); var source = Path.GetFullPath(reader.GetString(document.Name));
            Require(source.StartsWith(Path.GetFullPath(repository).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && reader.GetGuid(document.HashAlgorithm) == Guid.Parse("8829d00f-11b8-4213-878b-770e8597ac16"), "Unexpected probe source binding.");
            var hash = Convert.ToHexStringLower(reader.GetBlobBytes(document.Hash)); Require(HarnessJson.FileHash(source) == hash, "Probe source drift.");
            var relative = Path.GetRelativePath(repository, source).Replace('\\','/'); sources.Add(relative, hash);
            var target = P(output, "source/" + relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(source, target, false);
        }
        Require(sources.ContainsKey("LL/tests/BalanceHarness.ProcessFixture/ProposalAuditCostProbe.cs")
            && sources.ContainsKey("LL/tests/BalanceHarness.ProcessFixture/FixtureHost.cs"), "Missing probe source proof.");
        return new { pdbGuid = guid, sources };
    }

    public static async Task<int> Run(string requestPath)
    {
        var q = TowerContractJson.Read<ProposalAuditProbeRequest>(requestPath); Validate(q);
        Require(Path.GetFullPath(requestPath) == P(Path.GetFullPath(q.Output), "request.json"), "Unbound probe request.");
        using var process = Process.GetCurrentProcess(); using var parent = Process.GetProcessById(q.ParentId);
        Require(OperatingSystem.IsWindows() && IsProcessInJob(process.Handle, IntPtr.Zero, out var inJob) && inJob
            && !parent.HasExited && parent.StartTime.ToUniversalTime().Ticks == q.ParentStartTicks
            && HarnessJson.FileHash(typeof(ProposalAuditCostProbe).Assembly.Location) == q.HostHash, "Missing owned probe process or changed host.");
        var remaining = q.Deadline-DateTimeOffset.UtcNow; Require(remaining > TimeSpan.FromSeconds(2), "Expired probe.");
        using var stop = new CancellationTokenSource(remaining-TimeSpan.FromSeconds(1)); var ct = stop.Token;
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Audit-cost probe forbids combat.")).Activate();
        var phases = new List<ProposalAuditProbePhase>();
        void Check() { ct.ThrowIfCancellationRequested(); Require(!parent.HasExited && DateTimeOffset.UtcNow < q.Deadline, "Lost probe owner."); }
        void Put(string name, object value) { Check(); HarnessJson.WriteNew(P(q.Output, name), value); }
        async Task<T> Phase<T>(string name, Func<Task<T>> action, Func<T,long> items)
        {
            Put(name+"-started.json",new { name, startedAt = DateTimeOffset.UtcNow }); var clock = Stopwatch.StartNew();
            var value = await action(); var hash = HarnessJson.Hash(value);
            var receipt = new ProposalAuditProbePhase(name, clock.Elapsed.TotalSeconds, items(value), hash);
            Put(name+".json", receipt); phases.Add(receipt); return value;
        }
        try
        {
            Put("worker-start.json",new { version = Version, q.Mode, resourceOnly = true, fights = 0, newValues = 0 });
            if (q.Mode != "native")
            {
                if (q.Mode == "hang") await Task.Delay(Timeout.Infinite, ct);
                if (q.Mode == "storage") { using var stream = File.Create(P(q.Output,"overflow.bin")); stream.SetLength(Bytes+1); await Task.Delay(Timeout.Infinite, ct); }
                if (q.Mode == "combat") TowerPerformanceTrace.BattleStarted();
                Put("worker-observation.json",new { status = "LiteralProbeComplete", resourceOnly = true, fights = 0, newValues = 0 }); return 0;
            }
            var package = P(q.Repository,"TestResults/proposal-affinity-admission-repaired-20260923");
            var review = P(q.Repository,"TestResults/proposal-affinity-admission-review-20260923");
            var historical = P(q.Repository,"TestResults/balance/tower-frozen-pool-recognition-repaired-20260923");
            await Phase("qualification", () => {
                Require(HarnessJson.FileHash(P(review,"files.json")) == ReviewPin, "Changed qualification review pin.");
                TowerBulkCampaign.VerifyFiles(review,"files.json",true,ct);
                var files = HarnessJson.Read<Dictionary<string,string>>(P(review,"attempt-2-files.json"));
                Require(TowerBulkCampaign.Paths(package).Select(p => Path.GetRelativePath(package,p).Replace('\\','/')).Order(StringComparer.Ordinal)
                    .SequenceEqual(files.Keys.Order(StringComparer.Ordinal)), "Changed failed admission membership.");
                foreach (var file in files) { Check(); Require(HarnessJson.FileHash(P(package,file.Key)) == file.Value, "Changed qualification evidence."); }
                TowerProposalStudy.ValidateRuntime(P(package,"runtime.json"),AppContext.BaseDirectory,false);
                Require(HarnessJson.Hash(ExecutionIdentity.Current()) == ExecutionHash
                    && HarnessJson.FileHash(typeof(TowerProposalStudy).Assembly.Location) == HarnessPin, "Use the qualified unchanged runtime.");
                Put("source-proof.json",SourceProof(q.Output,q.Repository));
                return Task.FromResult(new { files = files.Count, executionHash = ExecutionHash });
            }, _ => 1);
            var history = HarnessJson.Read<int[]>(P(package,"history.json"));
            var context = HarnessJson.Read<TowerProposalContext>(P(package,"context.json"));
            var design = HarnessJson.Read<TowerProposalComparisonPlan>(P(package,"plan.json"));
            Require(history.Length == 672220, "Changed historical resource workload.");
            await Phase("historical-inventory", () => {
                Require(HarnessJson.FileHash(P(historical,"files.json")) == HistoricalPin, "Changed historical archive pin.");
                TowerBulkCampaign.VerifyFiles(historical,"files.json",false,ct);
                var files = HarnessJson.Read<Dictionary<string,string>>(P(historical,"files.json"));
                Require(TowerBulkCampaign.Paths(historical).Select(p => Path.GetRelativePath(historical,p).Replace('\\','/'))
                    .Where(n => n is not ("files.json" or "closeout.json")).Order(StringComparer.Ordinal).SequenceEqual(files.Keys.Order(StringComparer.Ordinal)), "Changed historical membership.");
                return Task.FromResult(new { files = files.Count, bytes = TowerBulkCampaign.StorageBytes(historical,ct) });
            }, value => value.files);
            var archived = P(historical,"study"); var oldScope = HarnessJson.Read<LoadoutScope>(P(archived,"scope.json"));
            ValidateSourceCompatibility(ExecutionIdentity.Current(),oldScope,context);
            long materializeTicks = 0; var inputCount = 0;
            var runner = new TowerBattleRunner(P(archived,"content"),new OfflineContent(P(archived,"content"),oldScope.Settings.Threat));
            await Phase("historical-native-reconstruction", async () => {
                var request = HarnessJson.Read<TowerFixedFamilyRequest>(P(historical,"request.json"));
                // Preserve the old archive's execution/cache identity. Only the
                // already-qualified input factory bridges to this unchanged runner.
                // VerifyStudy still authenticates every input hash, cache key,
                // literal battle, trial, freeze, journal and reconstructed result.
                var rebuilt = await TowerFixedFamilyConfirmation.VerifyStudy(request,ct,(scope,scenario,seed) => {
                    Check(); var start = Stopwatch.GetTimestamp();
                    var input = runner.CreateInput(scenario,seed,scope.Settings.Threat,scope.Settings.CheckpointIntervalTicks);
                    materializeTicks += Stopwatch.GetTimestamp()-start; inputCount++; return input;
                });
                Require(inputCount == 27648, "Incomplete historical native workload.");
                return new { inputCount, materializationSeconds = materializeTicks/(double)Stopwatch.Frequency, reconstructedHash = HarnessJson.Hash(rebuilt) };
            }, value => value.inputCount);
            Put("historical-materialization.json",new { inputCount, seconds = materializeTicks/(double)Stopwatch.Frequency,
                note = "C# loop timing; full input/cache hashing, report I/O and authentication are included in the enclosing reconstruction phase." });
            var plans = await Phase("full-history-workloads", () => Task.FromResult(Workloads(context,design,history)), p => p.Length);
            var measurements = new List<object>();
            await Phase("full-history-proposal-replay", async () => {
                for (var i = 0; i < plans.Length; i++)
                {
                    Check(); var clock = Stopwatch.StartNew();
                    var report = await TowerProposalPolicies.RunAsync(plans[i],(request,_) => { Check(); return Task.FromResult(Literal(request)); },ct);
                    Require(report.Evaluation.Status == "Complete" && report.Evaluation.ChargedEvaluations == 528, "Incomplete literal resource trajectory.");
                    var generationSeconds = clock.Elapsed.TotalSeconds; clock.Restart();
                    var rebuilt = await TowerProposalPolicies.ReconstructAsync(plans[i],report,ct);
                    var replaySeconds = clock.Elapsed.TotalSeconds;
                    var hash = HarnessJson.Hash(report); Require(HarnessJson.Hash(rebuilt) == hash, "Resource replay drift.");
                    var row = new { ordinal = i+1, root = i/2+1, arm = i%2 == 0 ? "control" : "candidate", historicalRoot = plans[i].Racing.RootSeed,
                        planHash = HarnessJson.Hash(plans[i]), reportHash = hash, generationSeconds, replaySeconds,
                        excludedValues = plans[i].Racing.Scope.ExcludedCombatSeeds.Count, literalRows = 528,
                        serializedPlanBytes = JsonSerializer.SerializeToUtf8Bytes(plans[i],HarnessJson.Options).Length,
                        resourceOnly = true, fights = 0, newValues = 0 };
                    Put($"proposal-{i+1:D2}.json",row); measurements.Add(row);
                }
                return measurements;
            }, _ => 24*528);
            // Catch changed source bytes after reads without mutating any archive.
            Require(HarnessJson.FileHash(P(historical,"files.json")) == HistoricalPin
                && HarnessJson.FileHash(P(review,"files.json")) == ReviewPin, "Source pin changed during probe.");
            Put("worker-observation.json",new { version = Version, status = "NativeAuditCostWorkloadComplete", resourceOnly = true,
                phases, inputReconstructions = inputCount, proposalArms = 24, literalRows = 12672,
                historyValues = history.Length, executionHash = ExecutionHash, fights = 0, newValues = 0, admission = "NotPerformed" });
            return 0;
        }
        catch (Exception error)
        {
            HarnessJson.WriteNew(P(q.Output,"worker-failure.json"),new { version = Version, error = error.ToString(), phases,
                fights = 0, newValues = 0, admission = "NotPerformed" }); throw;
        }
    }
}
