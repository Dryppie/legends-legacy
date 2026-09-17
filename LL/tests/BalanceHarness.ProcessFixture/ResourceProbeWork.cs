using System.Diagnostics;
using System.Text.Json;
using static BalanceHarness.ProcessFixture.ResourceProbeHost;

namespace BalanceHarness.ProcessFixture;

// This intentionally does not call a study runner, allocation, candidate generator,
// combat preparation, or either decision verifier. Repetition measures I/O only.
public static class ResourceProbeWork
{
    public const string PackageHash = "2c8415a62bdf6d72451a6468be5ba0c7ea78a10744e0688516a796690b921e15";
    public const string NativeHash = "54a3a877b209207bc66edd027396e7116e79acbae379639c513990d1a37f3db3";
    public sealed record Occurrence(string Id, string Stage, string SourceId, string Recipe, int HistoricalLabel, string InputHash, string CacheKey);

    public static string SourcePath(string repository, string relative) => Path.GetFullPath(Path.Combine(repository,relative));

    public static void ValidateSourceLocations(string repository, string registry, string content)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        bool Same(string actual, string relative) => string.Equals(Path.GetFullPath(actual),
            SourcePath(repository,relative),comparison);
        Require(Same(registry,"TestResults/balance") && Same(content,"LL/src/API/API.LL"), "Changed native source location.");
    }

    public static LoadoutTrial[] Plan(IReadOnlyList<LoadoutTrial> trials)
    {
        var discovery = trials.Where(t => t.Stage == "discovery").ToArray();
        var selection = trials.Where(t => t.Stage == "selection").ToArray();
        var confirmation = trials.Where(t => t.Stage == "confirmation").ToArray();
        Require(trials.Count == 1408 && discovery.Length == 512 && selection.Length == 128 && confirmation.Length == 768,
            "Unexpected retained trial shape.");
        return discovery.Concat(selection).Concat(Enumerable.Range(0, 4000).Select(i => confirmation[i%confirmation.Length])).ToArray();
    }

    public static int[] HistoricalLabels(IEnumerable<int> reportLabels, IReadOnlyList<int> history)
    {
        var used = reportLabels.Distinct().ToArray(); var known = history.ToHashSet();
        Require(used.Length == 256 && used.All(known.Contains), "Source labels are not historical.");
        var result = used.Concat(history.Order()).Distinct().Take(1000).ToArray();
        Require(result.Length == 1000 && result.All(known.Contains), "Insufficient existing labels for serialization workload.");
        return result;
    }

    public static object Run(ResourceProbeSpec q, Control control, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew(); var steps = new Dictionary<string, double>();
        T Timed<T>(string name, Func<T> action) { control.Check(); var start = clock.Elapsed.TotalSeconds; var value = action();
            steps.Add(name, clock.Elapsed.TotalSeconds-start); control.Put("step-"+name+".json",new { resourceOnly = true, seconds = steps[name] }); return value; }
        object Wrap(object value) => new { version = ResourceProbeHost.Version, resourceOnly = true, payload = value };
        string At(string path) => SourcePath(q.Repository,path);
        var package = At("TestResults/practical-native-verification-20260917");
        var native = At("TestResults/balance/tower-practical-native-verification-20260917");
        var source = Path.Combine(native, "study"); var payload = Path.Combine(q.Output, "payload");
        control.Enter("admission");
        var pins = new Dictionary<string, string> { [Path.Combine(package,"files.json")] = PackageHash, [Path.Combine(native,"files.json")] = NativeHash };
        var sourcePins = Directory.EnumerateFiles(At("LL/tools/BalanceHarness"), "*.cs")
            .Concat(Directory.EnumerateFiles(At("LL/tests/BalanceHarness.ProcessFixture"), "*.cs"))
            .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(q.Repository,p).Replace('\\','/'), HarnessJson.FileHash);
        control.Put("probe-source-pins.json", new { pins, sourcePins, execution = ExecutionIdentity.Current(),
            probeAssemblyHash = HarnessJson.FileHash(typeof(ResourceProbeWork).Assembly.Location),
            resourceOnly = true, q.Seconds, q.Bytes, reportWrites = 4640, auditWorkloadReads = 8640 });
        Timed("authenticate-retained-inventories", () => {
            foreach (var pin in pins) { Require(HarnessJson.FileHash(pin.Key) == pin.Value, "Changed retained manifest.");
                TowerBulkCampaign.VerifyFiles(Path.GetDirectoryName(pin.Key)!, "files.json", true, ct); }
            return true;
        });
        var oldRequest = HarnessJson.Read<TowerPracticalRequest>(Path.Combine(native,"request.json"));
        ValidateSourceLocations(q.Repository,oldRequest.RegistryRoot,oldRequest.ContentRoot);
        foreach (var p in oldRequest.RecoveryReceiptHashes!) Require(HarnessJson.FileHash(p.Key) == p.Value, "Changed existing recovery receipt.");
        var expected = TowerSearchBenchmark.History(HarnessJson.Read<JsonElement>(Path.Combine(native,"seed-ledger.json"))).Order().ToArray();
        Require(expected.Length == 484285, "Changed authenticated history size.");
        var required = oldRequest.RequiredHistory.ToDictionary(p => p.Key, p => p.Value);
        required.Add(Path.Combine(native,"seed-ledger.json"), HarnessJson.FileHash(Path.Combine(native,"seed-ledger.json")));
        var history = Timed("history-admission", () => TowerRefinementComparisonLaunch.Refresh(oldRequest.RegistryRoot, q.Output,
            required, expected, ct, oldRequest.PendingHistoryRecoveries));
        var execution = ExecutionIdentity.Current();
        var original = TowerBossDiscovery.Read(Path.Combine(source,"definition.json"));
        var template = original with { ExecutionHash = HarnessJson.Hash(execution), ExcludedCombatSeeds = history.Values,
            MaximumBattles = 4640, Generation = original.Generation with { Seeds = [] },
            Stages = original.Stages with { Schedules = original.Stages.Schedules.ToDictionary(p => p.Key,
                p => p.Value with { Discovery = [], Selection = [], Confirmation = [], Diagnostics = [] }) } };
        Timed("content-validation", () => { TowerSelectionDiagnostic.ValidateDefinition(template, true);
            TowerBossDiscovery.ValidateDiagnosticContent(oldRequest.ContentRoot, template, true); return true; });
        control.Put("protocol.json", new { version = ResourceProbeHost.Version, resourceOnly = true, q.Seconds, q.Bytes, pins, sourcePins, execution,
            sourceExecution = HarnessJson.Read<LoadoutScope>(Path.Combine(source,"scope.json")).Execution,
            historicalValues = history.Values.Length, historyFiles = history.Files, sourceConfirmationReports = 768,
            reportWrites = 4640, nativeAuditWorkloadReads = 4640, independentAuditWorkloadReads = 4000,
            confirmationScenarioLabels = 1000, entropyCalls = 0, newFights = 0, newReservations = 0,
            limitations = new[] { "Repeated historical reports are not new evidence or a diagnostic study.",
                "No candidate generation, combat runtime preparation, simulation, entropy sampler, statistical assessment or decision verifier is executed.",
                "Only three retained confirmation recipes exist; a fourth nominee's report distribution is unmeasured.",
                "Report source reads, pin authentication and probe bookkeeping add work absent from a new combat run.",
                "Disk byte counts and sampled file-size high water are not RAM or allocated filesystem blocks." } });
        Directory.CreateDirectory(payload);
        Timed("history-payload-serialization", () => {
            // Four full definition/binding payloads, plus three history arrays. Wrapped
            // payloads have no runnable top-level schema and no registry sentinel names.
            var searchPayload = template with { Generation = original.Generation, Stages = original.Stages with {
                Schedules = original.Stages.Schedules.ToDictionary(p => p.Key, p => p.Value with { Confirmation = [] }) } };
            control.Put("resource-template-payload.json", Wrap(template));
            control.Put("resource-search-binding-payload.json", Wrap(searchPayload));
            control.Put("payload/resource-definition-payload.json", Wrap(searchPayload));
            control.Put("payload/resource-search-binding-payload.json", Wrap(searchPayload));
            control.Put("resource-history-payload.json", Wrap(new { historical = history.Values, excluded = history.Values, ledgerCopy = history.Values }));
            return true;
        });
        var settings = TowerBundle.ReadSettings(oldRequest.ContentRoot);
        var hashes = Timed("copy-content", () => TowerBundle.CopyContent(oldRequest.ContentRoot, Path.Combine(payload,"content"), ct));
        Require(HarnessJson.Hash(hashes) == HarnessJson.Hash(original.ContentHashes), "Changed copied content.");
        var assets = Timed("copy-runtime", () => TowerBossStudy.RetainExecutable(payload, execution, control.RemainingBytes(), ct));
        control.Put("runtime-assets.json", assets);
        Timed("inventory-and-mechanics", () => { var inventory = TowerBossInventory.Create(Path.Combine(payload,"content"), settings.Threat);
            var mechanics = TowerBossPartyGenerator.FromInventory(TowerBossImprovement.Inputs(original), inventory);
            control.Put("resource-inventory.json", Wrap(inventory)); return mechanics.Essences.Count; });
        var trials = Timed("authenticate-study", () => TowerLoadoutArchive.Verify(source, ct));
        var plan = Plan(trials); var labels = HistoricalLabels(trials.Where(t => t.Stage == "confirmation").Select(t => t.Seed), history.Values);
        var originals = trials.Select(t => t.Recipe).Distinct().ToDictionary(r => r,
            r => HarnessJson.Read<TowerScenario>(Path.Combine(source,"recipes",r+".json")));
        Require(originals.All(p => HarnessJson.Hash(p.Value) == p.Key), "Changed retained recipe.");
        var recipes = originals.ToDictionary(p => p.Key, p => trials.Any(t => t.Recipe == p.Key && t.Stage == "confirmation")
            ? p.Value with { Seeds = labels } : p.Value);
        control.Put("payload/resource-recipes.json", Wrap(recipes));
        var scope = new LoadoutScope(ResourceProbeHost.Version, settings, execution, hashes, "gzip-json-v1");
        var runner = new TowerBattleRunner(Path.Combine(payload,"content"), new OfflineContent(Path.Combine(payload,"content"), settings.Threat));
        var occurrences = new List<Occurrence>(); Directory.CreateDirectory(Path.Combine(payload,"battles"));
        void Recheck(string name) => Timed(name, () => { TowerRefinementComparisonLaunch.Recheck(oldRequest.RegistryRoot,q.Output,history.Files,ct); return true; });
        void Identity(TowerBattleReport report, TowerScenario scenario, int seed) => Require(report.Battle.Seed == seed
            && report.Battle.ScenarioId == scenario.Id, "Retained report identity changed.");
        void Progress(string pass, int completed) => control.Put("progress.json", new { resourceOnly = true, pass, completed }, File.Exists(Path.Combine(q.Output,"progress.json")));
        control.Enter("search"); Recheck("history-before-search");
        for (var i = 0; i < plan.Length; i++)
        {
            if (i == 640) { control.Enter("confirmation"); Recheck("history-before-confirmation");
                control.Put("resource-existing-labels.json", Wrap(new { labels, description = "Existing exclusions for scenario size only; no evaluation panel" })); }
            control.Check(); var t = plan[i]; var scenario = recipes[t.Recipe];
            var input = runner.CreateInput(scenario,t.Seed,settings.Threat,settings.CheckpointIntervalTicks);
            Require(HarnessJson.Hash(input with { Scenario = originals[t.Recipe] }) == t.InputHash, "Native input changed under current executable.");
            var id = $"trial-{i+1:D6}"; var inputHash = HarnessJson.Hash(input); var key = TowerLoadoutArchive.Key(scope, "resource/"+id,input);
            var report = TowerLoadoutArchive.ReadBattle(source,t.Id,"gzip-json-v1"); Identity(report,scenario,t.Seed);
            TowerLoadoutArchive.WriteBattle(payload,id,report,"gzip-json-v1");
            occurrences.Add(new(id,t.Stage,t.Id,t.Recipe,t.Seed,inputHash,key));
            if ((i+1)%256 == 0 || i+1 == plan.Length) Progress("write",i+1);
        }
        control.Put("payload/resource-occurrences.json", Wrap(occurrences));
        control.Enter("audit");
        var files = new Dictionary<string,string>();
        foreach (var file in TowerBulkCampaign.Paths(payload)) { control.Check(); files.Add(Path.GetRelativePath(payload,file).Replace('\\','/'),HarnessJson.FileHash(file)); }
        control.Put("payload/resource-files.json",files);
        var audit = Timed("native-audit-read-workload", () => {
            TowerBulkCampaign.VerifyFiles(payload,"resource-files.json",true,ct);
            foreach (var row in occurrences) { control.Check(); var scenario = recipes[row.Recipe];
                var input = runner.CreateInput(scenario,row.HistoricalLabel,settings.Threat,settings.CheckpointIntervalTicks);
                Require(HarnessJson.Hash(input) == row.InputHash && TowerLoadoutArchive.Key(scope,"resource/"+row.Id,input) == row.CacheKey, "Changed workload input.");
                Identity(TowerLoadoutArchive.ReadBattle(payload,row.Id,"gzip-json-v1"),scenario,row.HistoricalLabel); }
            return occurrences.Count;
        });
        Progress("native-audit-read",audit);
        var independent = Timed("independent-audit-read-workload", () => {
            TowerBulkCampaign.VerifyFiles(payload,"resource-files.json",true,ct); var count = 0;
            foreach (var row in occurrences.Where(r => r.Stage == "confirmation")) { control.Check();
                Identity(TowerLoadoutArchive.ReadBattle(payload,row.Id,"gzip-json-v1"),recipes[row.Recipe],row.HistoricalLabel); count++; }
            return count;
        });
        Progress("independent-audit-read",independent); Recheck("history-after-audits");
        foreach (var pin in pins) Require(HarnessJson.FileHash(pin.Key) == pin.Value,"Changed source manifest.");
        foreach (var pin in sourcePins) { control.Check(); Require(HarnessJson.FileHash(At(pin.Key)) == pin.Value,"Probe source changed during observation."); }
        foreach (var p in original.ContentHashes) Require(HarnessJson.FileHash(Path.Combine(oldRequest.ContentRoot,"Data",p.Key)) == p.Value,"Live content changed.");
        return new { steps, historicalValues = history.Values.Length, historicalFiles = history.Files.Count, reportWrites = occurrences.Count,
            discoveryWrites = 512, selectionWrites = 128, confirmationWrites = 4000, sourceReads = 4640,
            nativeAuditWorkloadReads = audit, independentAuditWorkloadReads = independent, inputMaterializations = 9280,
            retainedConfirmationRecipes = trials.Where(t => t.Stage == "confirmation").Select(t => t.Recipe).Distinct().Count(),
            newEvidence = false, diagnosticDecision = "NotApplicable" };
    }
}
