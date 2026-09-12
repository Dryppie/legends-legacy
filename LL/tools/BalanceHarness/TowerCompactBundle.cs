using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Domain.Models.Combat;
using Domain.Models.WorldTower;
using Services.LL.Combat.Engine;
using Services.LL.Interfaces.Combat.Resolution;

namespace BalanceHarness;

public sealed record TowerCompactCase(string Id, TowerScenario Scenario);
public sealed record TowerCompactDefinition(int SchemaVersion, string Id, int MaximumBattles, int ChunkSize,
    IReadOnlyList<TowerCompactCase> Cases);
public sealed record TowerCompactCaseReference(string Id, string RecipeHash, string InputHash);
public sealed record TowerCompactPlan(int SchemaVersion, string Id, int MaximumBattles, int ChunkSize,
    int PlannedBattles, IReadOnlyList<TowerCompactCaseReference> Cases);
public sealed record TowerCompactInput(int SchemaVersion, string RecipeHash, TowerFloorDefinition Floor,
    JsonElement Guardian, IReadOnlyList<TowerPartyMember> Party, CombatRuleset Rules);
public sealed record TowerCompactRecord(int Index, string CaseId, int Seed, string PreparedHash,
    BattleSummary Summary, bool Succeeded, decimal GuardianHealthRemainingPercent, int DisplayDurationSeconds,
    string ReportHash);
public sealed record TowerCompactChunk(int SchemaVersion, int Index, int FirstTrial, int Count, string DataHash);
public sealed record TowerCompactManifest(int SchemaVersion, string Format, int CompletedBattles, int Chunks,
    IReadOnlyDictionary<string, string> Files);
public sealed record SavedTowerCompact(TowerCompactPlan Plan, LoadoutScope Scope,
    IReadOnlyDictionary<string, TowerScenario> Scenarios, IReadOnlyDictionary<string, IReadOnlyList<TowerTrial>> Cases,
    IReadOnlyDictionary<string, string> ResultDigests, string ManifestHash);

/// <summary>Lossless non-event Tower reports: shared recipes/inputs/preparation and compressed summary chunks.</summary>
public static class TowerCompactBundle
{
    public const string Format = "tower-compact-v1";
    public const string ManifestFile = "bulk-manifest.json";
    private const int MaximumJsonBytes = 64 * 1024 * 1024;
    private static readonly Regex SafeId = new("^[a-z0-9][a-z0-9-]{0,79}$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions CompactJson = new(HarnessJson.Options) { WriteIndented = false };

    public static int Validate(TowerCompactDefinition d)
    {
        if (d is null || d.SchemaVersion != 1 || d.Id is null || !SafeId.IsMatch(d.Id)
            || d.MaximumBattles is < 1 or > 100_000 || d.ChunkSize is < 1 or > 32
            || d.Cases is not { Count: >= 1 and <= 256 } || d.Cases.Any(c => c is null || c.Id is null || !SafeId.IsMatch(c.Id)
                || c.Scenario is null || c.Scenario.SchemaVersion != 1 || string.IsNullOrWhiteSpace(c.Scenario.Id)
                || c.Scenario.Assumptions is not { Count: > 0 } || c.Scenario.Assumptions.Any(string.IsNullOrWhiteSpace)
                || c.Scenario.Seeds is not { Count: >= 1 and <= 1000 }
                || c.Scenario.Seeds.Distinct().Count() != c.Scenario.Seeds.Count || c.Scenario.Party is not { Count: > 0 })
            || d.Cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != d.Cases.Count)
            throw new InvalidDataException("Invalid compact Tower definition, case identity, schedule or bounded chunk size.");
        var count = checked(d.Cases.Sum(c => c.Scenario.Seeds.Count));
        if (count > d.MaximumBattles) throw new InvalidDataException("Compact Tower schedule exceeds its declared combat reservation.");
        return count;
    }

    public static async Task CreateAsync(string apiRoot, TowerCompactDefinition definition, string output,
        CancellationToken token = default, Action<string>? progress = null, TowerSettings? settingsOverride = null,
        bool retainExecutable = false)
    {
        // Own the caller's mutable lists before freezing the experiment.
        var d = JsonSerializer.Deserialize<TowerCompactDefinition>(JsonSerializer.Serialize(definition, HarnessJson.Options), HarnessJson.Options)!;
        var planned = Validate(d);
        output = Path.GetFullPath(output);
        if (Path.Exists(output)) throw new IOException("Choose a new compact Tower output directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        using var timing = TowerPerformanceTrace.Measure("compact.create");
        var completed = 0; var chunkIndex = 0;
        try
        {
            var root = Path.Combine(output, "content");
            var settings = settingsOverride ?? TowerBundle.ReadSettings(apiRoot);
            var scope = new LoadoutScope(Format, settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(apiRoot, root, token), Format);
            HarnessJson.WriteNew(Path.Combine(output, "bulk-scope.json"), scope);
            if (retainExecutable) HarnessJson.WriteNew(Path.Combine(output, "executable-files.json"), TowerBossStudy.RetainExecutable(output, scope.Execution));
            foreach (var name in new[] { "recipes", "inputs", "prepared", "chunks" }) Directory.CreateDirectory(Path.Combine(output, name));
            var runner = new TowerBattleRunner(root, new OfflineContent(root, settings.Threat));
            var references = new List<TowerCompactCaseReference>();
            // No fight starts until every case has been legally materialized and its schedule frozen.
            foreach (var item in d.Cases)
            {
                token.ThrowIfCancellationRequested();
                var input = runner.CreateInput(item.Scenario, item.Scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks);
                var recipe = HarnessJson.Hash(item.Scenario);
                var compact = ProjectInput(input, recipe);
                var inputHash = HarnessJson.Hash(compact);
                WriteShared(output, "recipes", recipe, item.Scenario, false);
                WriteShared(output, "inputs", inputHash, compact, false);
                references.Add(new(item.Id, recipe, inputHash));
            }
            var plan = new TowerCompactPlan(1, d.Id, d.MaximumBattles, d.ChunkSize, planned, references);
            HarnessJson.WriteNew(Path.Combine(output, "bulk-plan.json"), plan);
            var pending = new List<TowerCompactRecord>(d.ChunkSize);
            foreach (var item in d.Cases)
            foreach (var seed in item.Scenario.Seeds)
            {
                token.ThrowIfCancellationRequested();
                var input = runner.CreateInput(item.Scenario, seed, settings.Threat, settings.CheckpointIntervalTicks);
                var report = await runner.RunAsync(input, token: token);
                var prepared = HarnessJson.Hash(report.Battle.PreparedParticipants);
                WriteShared(output, "prepared", prepared, report.Battle.PreparedParticipants, true);
                pending.Add(new(completed, item.Id, seed, prepared, report.Battle.Summary, report.Succeeded,
                    report.GuardianHealthRemainingPercent, report.DisplayDurationSeconds, ReportHash(report)));
                completed++;
                if (pending.Count == d.ChunkSize || completed == planned)
                {
                    token.ThrowIfCancellationRequested();
                    CommitChunk(output, chunkIndex, pending);
                    chunkIndex++;
                    pending.Clear();
                    progress?.Invoke($"Compact Tower: {completed}/{planned} trials committed.");
                }
            }
            token.ThrowIfCancellationRequested();
            HarnessJson.WriteNew(Path.Combine(output, "bulk-status.json"), new { Status = "Complete", Planned = planned, Completed = completed });
            var files = Files(output).ToDictionary(p => Relative(output, p), HarnessJson.FileHash, StringComparer.Ordinal);
            // The final manifest is the completion marker; readers reject missing or pending state.
            var temporary = Path.Combine(output, ManifestFile + ".pending");
            HarnessJson.WriteNew(temporary, new TowerCompactManifest(1, Format, completed, chunkIndex, files));
            File.Move(temporary, Path.Combine(output, ManifestFile));
        }
        catch (Exception error)
        {
            HarnessJson.WriteNew(Path.Combine(output, "bulk-failure.json"), new {
                Status = error is OperationCanceledException ? "Cancelled" : "Invalid", Planned = planned,
                CompletedCombats = completed, CommittedChunks = chunkIndex, error.Message });
            throw;
        }
    }

    public static SavedTowerCompact ReadSaved(string output, CancellationToken token = default, string? expectedManifestHash = null)
    {
        using var timing = TowerPerformanceTrace.Measure("compact.read-verify");
        output = Path.GetFullPath(output);
        token.ThrowIfCancellationRequested();
        var manifestHash = HarnessJson.FileHash(Path.Combine(output, ManifestFile));
        if (expectedManifestHash is not null && manifestHash != expectedManifestHash)
            throw new InvalidDataException("Compact Tower manifest differs from the trusted receipt.");
        var manifest = TowerContractJson.Read<TowerCompactManifest>(Path.Combine(output, ManifestFile));
        if (manifest.SchemaVersion != 1 || manifest.Format != Format || manifest.CompletedBattles is < 1 or > 100_000
            || manifest.Chunks is < 1 or > 100_000 || manifest.Files is null || manifest.Files.Count > 400_000)
            throw new InvalidDataException("Unsupported compact Tower manifest.");
        var files = Files(output).Select(p => Relative(output, p)).Where(p => p != ManifestFile).Order(StringComparer.Ordinal).ToArray();
        if (!files.SequenceEqual(manifest.Files.Keys.Order(StringComparer.Ordinal))) throw new InvalidDataException("Compact Tower inventory is missing files or contains uncommitted/extra files.");
        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            if (!TowerContractJson.Hash(manifest.Files[file]) || HarnessJson.FileHash(Path.Combine(output, file)) != manifest.Files[file])
                throw new InvalidDataException("Modified compact Tower file: " + file);
        }
        var scope = TowerContractJson.Read<LoadoutScope>(Path.Combine(output, "bulk-scope.json"));
        var plan = TowerContractJson.Read<TowerCompactPlan>(Path.Combine(output, "bulk-plan.json"));
        if (scope.Algorithm != Format || scope.ReportStorage != Format || scope.ContentHashes is null
            || !scope.ContentHashes.Keys.Order(StringComparer.Ordinal).SequenceEqual(TowerBundle.Files.Order(StringComparer.Ordinal))
            || scope.ContentHashes.Any(p => !manifest.Files.TryGetValue("content/Data/" + p.Key, out var hash) || hash != p.Value)
            || plan.SchemaVersion != 1 || plan.Cases is not { Count: >= 1 and <= 256 }
            || plan.Cases.Any(c => c is null || !TowerContractJson.Hash(c.RecipeHash) || !TowerContractJson.Hash(c.InputHash)))
            throw new InvalidDataException("Compact Tower scope, recipe or input index is invalid.");
        var scenarios = new Dictionary<string, TowerScenario>(StringComparer.Ordinal);
        var inputs = new Dictionary<string, TowerCompactInput>(StringComparer.Ordinal);
        var runner = new TowerBattleRunner(Path.Combine(output, "content"), new OfflineContent(Path.Combine(output, "content"), scope.Settings.Threat));
        foreach (var item in plan.Cases)
        {
            token.ThrowIfCancellationRequested();
            var recipe = TowerContractJson.Read<TowerScenario>(SharedPath(output, "recipes", item.RecipeHash, false));
            var input = TowerContractJson.Read<TowerCompactInput>(SharedPath(output, "inputs", item.InputHash, false));
            if (HarnessJson.Hash(recipe) != item.RecipeHash || HarnessJson.Hash(input) != item.InputHash || input.RecipeHash != item.RecipeHash)
                throw new InvalidDataException("Compact Tower recipe/input digest mismatch.");
            scenarios.Add(item.Id, recipe); inputs.Add(item.Id, input);
        }
        var definition = new TowerCompactDefinition(1, plan.Id, plan.MaximumBattles, plan.ChunkSize,
            plan.Cases.Select(c => new TowerCompactCase(c.Id, scenarios[c.Id])).ToArray());
        var planned = Validate(definition);
        if (plan.PlannedBattles != planned || manifest.CompletedBattles != planned || manifest.Chunks != (planned + plan.ChunkSize - 1) / plan.ChunkSize)
            throw new InvalidDataException("Compact Tower schedule is incomplete or has been extended.");
        foreach (var item in plan.Cases)
        {
            var recipe = scenarios[item.Id];
            var expected = runner.CreateInput(recipe, recipe.Seeds[0], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
            if (HarnessJson.Hash(ProjectInput(expected, item.RecipeHash)) != item.InputHash)
                throw new InvalidDataException("Compact Tower materialized input does not match frozen content/recipe.");
        }
        var schedule = definition.Cases.SelectMany(c => c.Scenario.Seeds.Select(seed => (Case: c.Id, Seed: seed))).ToArray();
        var cases = definition.Cases.ToDictionary(c => c.Id, _ => new List<TowerTrial>(), StringComparer.Ordinal);
        var reportHashes = definition.Cases.ToDictionary(c => c.Id, _ => new Dictionary<string, string>(), StringComparer.Ordinal);
        var prepared = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var usedFiles = new HashSet<string>(StringComparer.Ordinal) { "bulk-plan.json", "bulk-scope.json", "bulk-status.json" };
        foreach (var file in TowerBundle.Files) usedFiles.Add("content/Data/" + file);
        foreach (var item in plan.Cases) { usedFiles.Add("recipes/" + item.RecipeHash + ".json"); usedFiles.Add("inputs/" + item.InputHash + ".json"); }
        var cursor = 0;
        for (var index = 0; index < manifest.Chunks; index++)
        {
            token.ThrowIfCancellationRequested();
            var chunk = ChunkName(index);
            var dataName = "chunks/" + chunk + "/records.json.gz";
            var receiptName = "chunks/" + chunk + "/receipt.json";
            var receipt = TowerContractJson.Read<TowerCompactChunk>(Path.Combine(output, receiptName));
            var expectedCount = Math.Min(plan.ChunkSize, planned - cursor);
            if (receipt.SchemaVersion != 1 || receipt.Index != index || receipt.FirstTrial != cursor || receipt.Count != expectedCount
                || !manifest.Files.TryGetValue(dataName, out var dataHash) || receipt.DataHash != dataHash)
                throw new InvalidDataException("Compact Tower chunk receipt, count or digest changed.");
            var rows = ReadGzip<TowerCompactRecord[]>(Path.Combine(output, dataName));
            if (rows.Length != expectedCount) throw new InvalidDataException("Compact Tower chunk is truncated or contains extra trials.");
            usedFiles.Add(dataName); usedFiles.Add(receiptName);
            foreach (var row in rows)
            {
                token.ThrowIfCancellationRequested();
                if (row is null || row.Index != cursor || row.CaseId != schedule[cursor].Case || row.Seed != schedule[cursor].Seed
                    || !TowerContractJson.Hash(row.PreparedHash) || !TowerContractJson.Hash(row.ReportHash))
                    throw new InvalidDataException("Compact Tower trial identity/order differs from the frozen schedule.");
                if (!prepared.TryGetValue(row.PreparedHash, out var participants))
                {
                    participants = ReadGzip<JsonElement>(SharedPath(output, "prepared", row.PreparedHash, true));
                    if (HarnessJson.Hash(participants) != row.PreparedHash) throw new InvalidDataException("Modified shared prepared participants.");
                    prepared.Add(row.PreparedHash, participants); usedFiles.Add("prepared/" + row.PreparedHash + ".json.gz");
                }
                var report = Restore(scenarios[row.CaseId], row, participants);
                ValidateReport(report, inputs[row.CaseId]);
                if (ReportHash(report) != row.ReportHash) throw new InvalidDataException("Compact summary does not reconstruct its original full report.");
                var trials = cases[row.CaseId];
                var trialId = TowerId(trials.Count);
                trials.Add(new(trialId, row.Seed, report));
                reportHashes[row.CaseId].Add(trialId, row.ReportHash);
                cursor++;
            }
        }
        var status = HarnessJson.Read<JsonElement>(Path.Combine(output, "bulk-status.json"));
        if (status.GetProperty("status").GetString() != "Complete" || status.GetProperty("planned").GetInt32() != planned
            || status.GetProperty("completed").GetInt32() != planned)
            throw new InvalidDataException("Compact Tower status is incomplete.");
        if (manifest.Files.ContainsKey("executable-files.json"))
        {
            usedFiles.Add("executable-files.json");
            var executable = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "executable-files.json"));
            foreach (var item in executable)
            {
                var name = "executable/" + item.Key;
                if (!manifest.Files.TryGetValue(name, out var hash) || hash != item.Value) throw new InvalidDataException("Modified retained compact executable.");
                usedFiles.Add(name);
            }
            if (scope.Execution.AssemblyHashes.Any(p => !executable.TryGetValue(p.Key + ".dll", out var hash) || hash != p.Value))
                throw new InvalidDataException("Retained compact executable differs from producing identity.");
        }
        if (!usedFiles.Order(StringComparer.Ordinal).SequenceEqual(files)) throw new InvalidDataException("Compact Tower contains unreferenced data or pending chunks.");
        return new(plan, scope, scenarios, cases.ToDictionary(p => p.Key, p => (IReadOnlyList<TowerTrial>)p.Value, StringComparer.Ordinal),
            reportHashes.ToDictionary(p => p.Key, p => HarnessJson.Hash(p.Value), StringComparer.Ordinal), manifestHash);
    }

    public static TowerBalanceEvidence Evidence(string cellId, SavedTowerCompact saved, string? caseId = null)
    {
        caseId ??= saved.Plan.Cases.Count == 1 ? saved.Plan.Cases[0].Id : throw new InvalidDataException("A multi-case compact archive requires an explicit case ID.");
        if (!saved.Cases.TryGetValue(caseId, out var trials)) throw new InvalidDataException("Unknown compact case ID.");
        var scenario = saved.Scenarios[caseId];
        return new(cellId, "Complete", HarnessJson.Hash(scenario), HarnessJson.Hash(saved.Scope.ContentHashes),
            HarnessJson.Hash(saved.Scope.Settings), HarnessJson.Hash(saved.Scope.Execution), scenario.Party.Count,
            trials.Select(t => new TowerBalanceTrial(t.Seed, t.Report.Battle.Summary.ContentOutcome)).ToArray(), saved.ManifestHash);
    }

    public static async Task<TowerBattleReport> ReplayAsync(string output, string caseId, string battleId, bool detailed,
        CancellationToken token = default)
    {
        var saved = ReadSaved(output, token);
        if (HarnessJson.Hash(saved.Scope.Execution) != HarnessJson.Hash(ExecutionIdentity.Current()))
            throw new InvalidDataException("Compact replay requires original assemblies, runtime and platform.");
        if (!saved.Cases.TryGetValue(caseId, out var trials)) throw new InvalidDataException("Unknown compact replay case.");
        var original = trials.SingleOrDefault(t => t.Id == battleId) ?? throw new InvalidDataException("Unknown compact replay trial.");
        var root = Path.Combine(output, "content");
        var runner = new TowerBattleRunner(root, new OfflineContent(root, saved.Scope.Settings.Threat));
        var input = runner.CreateInput(saved.Scenarios[caseId], original.Seed, saved.Scope.Settings.Threat, saved.Scope.Settings.CheckpointIntervalTicks);
        var report = await runner.RunAsync(input, detailed, token);
        RunBundle.VerifyResult(original.Report.Battle, report.Battle);
        if (original.Report.Succeeded != report.Succeeded || original.Report.GuardianHealthRemainingPercent != report.GuardianHealthRemainingPercent
            || original.Report.DisplayDurationSeconds != report.DisplayDurationSeconds)
            throw new InvalidDataException("Compact replay Tower result changed.");
        return report;
    }

    public static string ReportHash(TowerBattleReport report)
    {
        using var timing = TowerPerformanceTrace.Measure("hash.compatible-report");
        using var algorithm = SHA256.Create();
        using (var sink = new CryptoStream(Stream.Null, algorithm, CryptoStreamMode.Write))
            JsonSerializer.Serialize(sink, report, HarnessJson.Options);
        return Convert.ToHexStringLower(algorithm.Hash!);
    }
    public static string ResultDigest(IReadOnlyList<TowerTrial> trials) => HarnessJson.Hash(trials.ToDictionary(t => t.Id, t => ReportHash(t.Report)));
    private static string TowerId(int index) => "tower." + (index + 1).ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
    private static TowerCompactInput ProjectInput(TowerBattleInput input, string recipe) => new(1, recipe, input.Floor, input.Guardian, input.Party, input.Rules);
    private static TowerBattleReport Restore(TowerScenario scenario, TowerCompactRecord row, JsonElement participants) =>
        new(new(1, scenario.Id, row.Seed, FastCombatEngine.TicksPerSecond, participants, row.Summary, null), row.Succeeded, row.GuardianHealthRemainingPercent, row.DisplayDurationSeconds);
    private static void ValidateReport(TowerBattleReport report, TowerCompactInput input)
    {
        var summary = report.Battle.Summary;
        if (summary is null || !Enum.IsDefined(summary.EngineOutcome) || !Enum.IsDefined(summary.ContentOutcome)
            || summary.DurationTicks < 0 || summary.DurationTicks > input.Rules.MaxTicks
            || summary.DurationSeconds != summary.DurationTicks / (double)FastCombatEngine.TicksPerSecond
            || report.DisplayDurationSeconds != (int)Math.Ceiling(summary.DurationSeconds)
            || report.Succeeded != (summary.ContentOutcome == BattleOutcome.Victory)
            || summary.TerminationReason != (summary.EngineOutcome == BattleOutcome.Draw && summary.DurationTicks >= input.Rules.MaxTicks ? "TickLimit" : summary.EngineOutcome.ToString())
            || summary.Friendly is null || summary.Hostile is null || summary.Statistics is null || summary.Telemetry is null
            || report.GuardianHealthRemainingPercent is < 0 or > 100)
            throw new InvalidDataException("Compact Tower result has inconsistent outcome, duration or required statistics.");
    }
    private static string ChunkName(int index) => index.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    private static void CommitChunk(string output, int index, IReadOnlyList<TowerCompactRecord> rows)
    {
        using var timing = TowerPerformanceTrace.Measure("compact.commit-chunk");
        var folder = Path.Combine(output, "chunks", ".pending-" + ChunkName(index));
        Directory.CreateDirectory(folder);
        var data = Path.Combine(folder, "records.json.gz");
        WriteGzip(data, rows);
        HarnessJson.WriteNew(Path.Combine(folder, "receipt.json"), new TowerCompactChunk(1, index, rows[0].Index, rows.Count, HarnessJson.FileHash(data)));
        Directory.Move(folder, Path.Combine(output, "chunks", ChunkName(index)));
    }
    private static string SharedPath(string output, string folder, string hash, bool gzip)
    {
        if (!TowerContractJson.Hash(hash)) throw new InvalidDataException("Invalid shared artifact hash.");
        return Path.Combine(output, folder, hash + (gzip ? ".json.gz" : ".json"));
    }
    private static void WriteShared<T>(string output, string folder, string hash, T value, bool gzip)
    {
        var path = SharedPath(output, folder, hash, gzip);
        if (File.Exists(path)) return;
        if (gzip) WriteGzip(path, value); else HarnessJson.WriteNew(path, value);
    }
    private static void WriteGzip<T>(string path, T value)
    {
        using var timing = TowerPerformanceTrace.Measure("compact.serialize-compress-write");
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var gzip = new GZipStream(file, CompressionLevel.Fastest);
        using var limited = new LimitedStream(gzip, MaximumJsonBytes);
        JsonSerializer.Serialize(limited, value, CompactJson);
    }
    private static T ReadGzip<T>(string path)
    {
        using var timing = TowerPerformanceTrace.Measure("compact.read-decompress-deserialize");
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var limited = new LimitedStream(gzip, MaximumJsonBytes);
        return JsonSerializer.Deserialize<T>(limited, CompactJson) ?? throw new InvalidDataException("Empty compact compressed JSON.");
    }
    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
    private static IReadOnlyList<string> Files(string output)
    {
        var files = new List<string>(); var pending = new Stack<string>(); pending.Push(output);
        while (pending.TryPop(out var directory))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked compact archive directory.");
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                var info = new FileInfo(path);
                if ((info.Attributes & FileAttributes.Directory) != 0) pending.Push(path);
                else { if (info.LinkTarget is not null) throw new InvalidDataException("Linked compact archive file."); files.Add(path); }
            }
        }
        return files;
    }
    private sealed class LimitedStream(Stream inner, long limit) : Stream
    {
        private long transferred;
        private void Charge(int count) { transferred += count; if (transferred > limit) throw new InvalidDataException("Compact JSON exceeds the 64 MiB per-file uncompressed limit."); }
        public override bool CanRead => inner.CanRead;
        public override bool CanWrite => inner.CanWrite;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => transferred; set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) { var read = inner.Read(buffer, offset, count); Charge(read); return read; }
        public override int Read(Span<byte> buffer) { var read = inner.Read(buffer); Charge(read); return read; }
        public override void Write(byte[] buffer, int offset, int count) { Charge(count); inner.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Charge(buffer.Length); inner.Write(buffer); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
