using System.Text;
using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerCompactCheckpoint(int SchemaVersion, IReadOnlyDictionary<string, string> Files);
public sealed record TowerCompactAttempt(int Index, int TrialIndex, string CaseId, int Seed);

public static partial class TowerCompactBundle
{
    private const string ResumeFile = "bulk-resume.json";
    private const string AttemptsFile = "bulk-attempts.jsonl";

    // A separate sibling lease never enters the sealed archive inventory. DeleteOnClose also handles process death.
    internal static FileStream AcquireWriter(string output)
    {
        var path = Path.GetFullPath(output) + ".writer.lock";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path) && new FileInfo(path).LinkTarget is not null) throw new InvalidDataException("Linked writer lease.");
        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
    }

    private static string ContentRoot(string output, string? shared)
    {
        if (shared is null) return Path.Combine(output, "content");
        var parent = Directory.GetParent(Path.GetFullPath(output));
        if (shared != "../../content" || parent?.Name != "batches" || parent.Parent is null
            || !File.Exists(Path.Combine(parent.Parent.FullName, "campaign.json")))
            throw new InvalidDataException("Shared compact content requires its owning campaign layout.");
        return Path.Combine(parent.Parent.FullName, "content");
    }

    internal static IReadOnlyDictionary<string, string> ContentHashes(string root, CancellationToken token)
    {
        using var timing = TowerPerformanceTrace.Measure("compact.shared-content-hashes");
        // Walk first to reject junctions/symlinks before following content files.
        Files(root);
        return TowerBundle.Files.ToDictionary(name => name, name => {
            token.ThrowIfCancellationRequested(); return HarnessJson.FileHash(Path.Combine(root, "Data", name));
        }, StringComparer.Ordinal);
    }

    internal static int AttemptCount(string output) => ValidateAttempts(output, Definition(output), 0);
    internal static TowerCompactDefinition Definition(string output)
    {
        var plan = TowerContractJson.Read<TowerCompactPlan>(Path.Combine(output, "bulk-plan.json"));
        var definition = new TowerCompactDefinition(1, plan.Id, plan.MaximumBattles, plan.ChunkSize, plan.Cases.Select(c =>
            new TowerCompactCase(c.Id, TowerContractJson.Read<TowerScenario>(SharedPath(output, "recipes", c.RecipeHash, false)))).ToArray());
        Validate(definition); return definition;
    }

    private static void WriteCheckpoint(string output)
    {
        HarnessJson.WriteNew(Path.Combine(output, ResumeFile), new TowerCompactCheckpoint(1,
            Files(output).ToDictionary(p => Relative(output, p), HarnessJson.FileHash, StringComparer.Ordinal)));
        using var attempts = new FileStream(Path.Combine(output, AttemptsFile), FileMode.CreateNew, FileAccess.Write);
        attempts.Flush(true);
    }

    private static TowerCompactCheckpoint ReadCheckpoint(string output, CancellationToken token)
    {
        var checkpoint = TowerContractJson.Read<TowerCompactCheckpoint>(Path.Combine(output, ResumeFile));
        var actual = Files(output).Select(p => Relative(output, p)).ToHashSet(StringComparer.Ordinal);
        if (checkpoint.SchemaVersion != 1 || checkpoint.Files is null || checkpoint.Files.Count > 10000
            || !checkpoint.Files.ContainsKey("bulk-plan.json") || !checkpoint.Files.ContainsKey("bulk-scope.json"))
            throw new InvalidDataException("Invalid compact resume checkpoint.");
        foreach (var pair in checkpoint.Files)
        {
            token.ThrowIfCancellationRequested();
            if (!actual.Contains(pair.Key) || !TowerContractJson.Hash(pair.Value)
                || HarnessJson.FileHash(Path.Combine(output, pair.Key)) != pair.Value)
                throw new InvalidDataException("Frozen compact resume identity changed: " + pair.Key);
        }
        return checkpoint;
    }

    private static int ValidateAttempts(string output, TowerCompactDefinition d, int committed)
    {
        using var timing = TowerPerformanceTrace.Measure("compact.validate-attempts");
        var schedule = d.Cases.SelectMany(c => c.Scenario.Seeds.Select(seed => (c.Id, Seed: seed))).ToArray();
        var seen = new HashSet<int>(); var count = 0;
        using var reader = new StreamReader(Path.Combine(output, AttemptsFile), Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 4096 || ++count > d.MaximumBattles) throw new InvalidDataException("Compact attempt reservation exceeded.");
            var row = JsonSerializer.Deserialize<TowerCompactAttempt>(line, HarnessJson.Options)
                ?? throw new InvalidDataException("Empty compact attempt.");
            if (row.Index != count - 1 || row.TrialIndex < 0 || row.TrialIndex >= schedule.Length
                || row.CaseId != schedule[row.TrialIndex].Id || row.Seed != schedule[row.TrialIndex].Seed)
                throw new InvalidDataException("Compact attempt differs from the frozen schedule.");
            seen.Add(row.TrialIndex);
        }
        if (Enumerable.Range(0, committed).Any(i => !seen.Contains(i))) throw new InvalidDataException("Committed trials lack charged attempts.");
        // A torn JSONL tail is ambiguous. Refuse it instead of silently discarding a charged attempt.
        using var stream = File.OpenRead(Path.Combine(output, AttemptsFile));
        if (stream.Length > 0) { stream.Seek(-1, SeekOrigin.End); if (stream.ReadByte() != '\n') throw new InvalidDataException("Torn compact attempt journal."); }
        return count;
    }

    private static void AppendAttempt(string output, TowerCompactDefinition d, int index, int trialIndex, string caseId, int seed)
    {
        using var timing = TowerPerformanceTrace.Measure("compact.append-attempt");
        if (index >= d.MaximumBattles) throw new InvalidDataException("Compact actual-attempt budget exhausted; uncommitted retries also consume the frozen reservation.");
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TowerCompactAttempt(index, trialIndex, caseId, seed), CompactJson) + "\n");
        using var file = new FileStream(Path.Combine(output, AttemptsFile), FileMode.Append, FileAccess.Write, FileShare.Read);
        file.Write(bytes);
        // Durable before starting combat, including crashes and cancelled attempts.
        using (TowerPerformanceTrace.Measure("compact.flush-attempt")) file.Flush(true);
    }

    private static async Task ContinueAsync(string output, TowerCompactDefinition d, TowerBattleRunner runner, TowerSettings settings,
        string? executionMode, int completed, int chunkIndex, CancellationToken token, Action<string>? progress, Action<int, int> checkpoint, Action? beforeAttempt)
    {
        var planned = Validate(d); var offset = 0;
        var attempts = ValidateAttempts(output, d, completed);
        if (attempts + planned - completed > d.MaximumBattles)
            throw new InvalidDataException("Remaining compact reservation cannot cover the uncommitted schedule; do not reset its attempt ledger.");
        var pending = new List<TowerCompactRecord>(d.ChunkSize);
        foreach (var item in d.Cases)
        {
            var skip = Math.Min(completed - offset, item.Scenario.Seeds.Count);
            offset += item.Scenario.Seeds.Count;
            if (skip == item.Scenario.Seeds.Count) continue;
            // Keep preparation only for this case; no cross-case or cross-archive mutable cache.
            var reusable = executionMode == TowerPreparedBattle.Mode
                ? await runner.PrepareReusableAsync(runner.CreateInput(item.Scenario, item.Scenario.Seeds[0], settings.Threat, settings.CheckpointIntervalTicks), token)
                : null;
            foreach (var seed in item.Scenario.Seeds.Skip(skip))
            {
                token.ThrowIfCancellationRequested();
                beforeAttempt?.Invoke();
                AppendAttempt(output, d, attempts++, completed, item.Id, seed);
                var report = reusable is not null ? await reusable.RunAsync(seed, token: token)
                    : await runner.RunAsync(runner.CreateInput(item.Scenario, seed, settings.Threat, settings.CheckpointIntervalTicks), token: token);
                var prepared = reusable?.ParticipantsHash ?? HarnessJson.Hash(report.Battle.PreparedParticipants);
                WriteShared(output, "prepared", prepared, report.Battle.PreparedParticipants, true);
                pending.Add(new(completed, item.Id, seed, prepared, report.Battle.Summary, report.Succeeded,
                    report.GuardianHealthRemainingPercent, report.DisplayDurationSeconds, ReportHash(report)));
                completed++;
                checkpoint(completed, chunkIndex);
                if (pending.Count == d.ChunkSize || completed == planned)
                {
                    token.ThrowIfCancellationRequested();
                    CommitChunk(output, chunkIndex, pending);
                    chunkIndex++;
                    pending.Clear();
                    checkpoint(completed, chunkIndex);
                    progress?.Invoke($"Compact Tower: {completed}/{planned} trials committed.");
                }
            }
        }

    }

    private static void Complete(string output, int planned, int completed, int chunkIndex, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        HarnessJson.WriteNew(Path.Combine(output, "bulk-status.json"), new { Status = "Complete", Planned = planned, Completed = completed });
        var files = Files(output).ToDictionary(p => Relative(output, p), HarnessJson.FileHash, StringComparer.Ordinal);
        // The final manifest is the completion marker; readers reject missing or pending state.
        var temporary = Path.Combine(output, ManifestFile + ".pending");
        HarnessJson.WriteNew(temporary, new TowerCompactManifest(1, Format, completed, chunkIndex, files));
        File.Move(temporary, Path.Combine(output, ManifestFile));
    }

    private static async Task ResumeCoreAsync(string apiRoot, TowerCompactDefinition d, string output, CancellationToken token,
        Action<string>? progress, TowerSettings? settingsOverride, bool retainExecutable, string? mode, bool sharedContent, Action? beforeAttempt)
    {
        if (!Directory.Exists(output)) throw new IOException("Cannot resume a missing compact archive.");
        var checkpoint = ReadCheckpoint(output, token);
        var settings = settingsOverride ?? TowerBundle.ReadSettings(apiRoot);
        var scope = TowerContractJson.Read<LoadoutScope>(Path.Combine(output, "bulk-scope.json"));
        var plan = TowerContractJson.Read<TowerCompactPlan>(Path.Combine(output, "bulk-plan.json"));
        if (HarnessJson.Hash(scope.Settings) != HarnessJson.Hash(settings)
            || HarnessJson.Hash(scope.Execution) != HarnessJson.Hash(ExecutionIdentity.Current())
            || scope.ContentHashes.Any(p => HarnessJson.FileHash(Path.Combine(apiRoot, "Data", p.Key)) != p.Value)
            || plan.SharedContentPath != (sharedContent ? "../../content" : null) || plan.ExecutionMode != mode || plan.Id != d.Id || plan.MaximumBattles != d.MaximumBattles || plan.ChunkSize != d.ChunkSize
            || !plan.Cases.Select(c => (c.Id, c.RecipeHash)).SequenceEqual(d.Cases.Select(c => (c.Id, HarnessJson.Hash(c.Scenario))))
            || retainExecutable != checkpoint.Files.ContainsKey("executable-files.json"))
            throw new InvalidDataException("Resume requires identical content, settings, producing execution, mode, recipes and ordered seeds.");
        if (File.Exists(Path.Combine(output, ManifestFile))) { Verify(output, token); return; }
        var chunkDirs = Directory.GetDirectories(Path.Combine(output, "chunks")).Order(StringComparer.Ordinal).ToArray();
        if (!chunkDirs.Select(Path.GetFileName).SequenceEqual(Enumerable.Range(0, chunkDirs.Length).Select(ChunkName)))
            throw new InvalidDataException("Missing, non-contiguous or pending compact chunks require explicit recovery; resume will not discard them.");
        var completed = chunkDirs.Sum(p => TowerContractJson.Read<TowerCompactChunk>(Path.Combine(p, "receipt.json")).Count);
        var files = Files(output).ToDictionary(p => Relative(output, p), HarnessJson.FileHash, StringComparer.Ordinal);
        VerifyCore(output, token, null, null, new(1, Format, completed, chunkDirs.Length, files));
        var attempts = ValidateAttempts(output, d, completed);
        if (attempts + Validate(d) - completed > d.MaximumBattles)
            throw new InvalidDataException("Remaining compact reservation cannot cover retries of uncommitted trials.");
        // Only validated artifacts owned by this unfinished run are replaced. Committed chunks stay immutable.
        File.Delete(Path.Combine(output, "bulk-failure.json"));
        File.Delete(Path.Combine(output, "bulk-status.json"));
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var folder in chunkDirs)
            foreach (var row in ReadGzip<TowerCompactRecord[]>(Path.Combine(folder, "records.json.gz"))) referenced.Add(row.PreparedHash);
        foreach (var file in Directory.GetFiles(Path.Combine(output, "prepared")))
            if (!referenced.Contains(Path.GetFileName(file)[..^8])) File.Delete(file);
        var committedChunks = chunkDirs.Length;
        try
        {
            var root = ContentRoot(output, plan.SharedContentPath);
            await ContinueAsync(output, d, new(root, new OfflineContent(root, settings.Threat)), settings, mode,
                completed, committedChunks, token, progress, (count, chunks) => { completed = count; committedChunks = chunks; }, beforeAttempt);
            Complete(output, Validate(d), completed, committedChunks, token);
        }
        catch (Exception error)
        {
            HarnessJson.WriteNew(Path.Combine(output, "bulk-failure.json"), new {
                Status = error is OperationCanceledException ? "Cancelled" : "Invalid", Planned = Validate(d),
                CompletedCombats = completed, CommittedChunks = committedChunks, error.Message });
            throw;
        }
    }
}
