using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerBulkOptions(int ChunkSize = 32, int RetryReserve = 32, int MaximumSeconds = 300,
    long MaximumBytes = 2147483648, string ExecutionMode = "prepared-v1");
public sealed record TowerBulkContract(int SchemaVersion, string Kind, JsonElement Definition, LoadoutScope Scope,
    TowerBulkOptions Options, int PlannedBattles, int MaximumAttempts);
public sealed record TowerBulkAccounting(int LogicalTrials, int ChargedAttempts, int RetryOrUncommittedAttempts, int MaximumAttempts);

/// <summary>One owning campaign, shared frozen content, immutable batch archives and deterministic prefix reconstruction.</summary>
internal sealed class TowerBulkCampaign : IDisposable
{
    public const string ContractFile = "campaign.json";
    private const string FrozenFiles = "campaign-frozen-files.json";
    private const string FinalFiles = "campaign-manifest.json";
    private readonly FileStream lease;
    private readonly CancellationTokenSource cancellation;
    private readonly Action<string>? progress;
    private readonly bool verifyOnly;
    private readonly string output;
    private readonly HashSet<string> visited = new(StringComparer.Ordinal);
    private int attempts;
    public TowerBulkContract Contract { get; }
    public CancellationToken Token => cancellation.Token;
    public string Root => Path.Combine(output, "content");
    public bool WasComplete { get; }

    private TowerBulkCampaign(string output, TowerBulkContract contract, FileStream lease,
        CancellationToken token, Action<string>? progress, bool verifyOnly)
    {
        this.output = output; Contract = contract; this.lease = lease; this.progress = progress; this.verifyOnly = verifyOnly;
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        cancellation.CancelAfter(TimeSpan.FromSeconds(contract.Options.MaximumSeconds));
        WasComplete = File.Exists(Path.Combine(output, FinalFiles));
        if (WasComplete) VerifyFiles(output, FinalFiles, exact: true, Token);
        if (verifyOnly && !WasComplete) throw new InvalidDataException("Only completed compact campaigns can be fully reconstructed.");
        var directories = Directory.GetDirectories(Path.Combine(output, "batches")).Order(StringComparer.Ordinal).ToArray();
        var stem = contract.Kind == TowerCompactDiscovery.Kind ? "discovery-" : "confirmation-";
        if (!directories.Select(Path.GetFileName).SequenceEqual(Enumerable.Range(0, directories.Length)
            .Select(i => stem + i.ToString("D6", System.Globalization.CultureInfo.InvariantCulture))))
            throw new InvalidDataException("Campaign batch prefix is missing or contains extra directories.");
        foreach (var directory in directories)
            attempts = checked(attempts + TowerCompactBundle.AttemptCount(directory));
        if (attempts > contract.MaximumAttempts) throw new InvalidDataException("Campaign attempt cap exceeded.");
    }

    public static TowerBulkCampaign Open<T>(string root, string output, string kind, T definition,
        IReadOnlyDictionary<string, string> content, string settingsHash, string executionHash, int planned, int maximum,
        TowerBulkOptions options, bool resume, bool verifyOnly, CancellationToken token, Action<string>? progress)
    {
        if (options.ChunkSize is < 1 or > 32 || options.RetryReserve is < 0 or > 1000
            || options.MaximumSeconds is < 1 or > 86400 || options.MaximumBytes is < 1048576 or > 107374182400
            || planned + options.RetryReserve > maximum || planned < 1 || options.ExecutionMode != TowerPreparedBattle.Mode)
            throw new InvalidDataException("Invalid compact campaign limits; planned trials plus retry reserve must fit the definition's combat cap.");
        output = Path.GetFullPath(output);
        var lease = TowerCompactBundle.AcquireWriter(output);
        try
        {
            var settings = verifyOnly ? TowerContractJson.Read<TowerBulkContract>(Path.Combine(output, ContractFile)).Scope.Settings : TowerBundle.ReadSettings(root);
            var execution = ExecutionIdentity.Current();
            if (HarnessJson.Hash(settings) != settingsHash || HarnessJson.Hash(execution) != executionHash
                || HarnessJson.Hash(TowerCompactBundle.ContentHashes(root, token)) != HarnessJson.Hash(content))
                throw new InvalidDataException("Campaign content, settings or producing execution differs from its frozen definition.");
            var contract = new TowerBulkContract(1, kind, JsonSerializer.SerializeToElement(definition, HarnessJson.Options),
                new(kind, settings, execution, content, TowerCompactBundle.Format), options, planned, planned + options.RetryReserve);
            if (resume || verifyOnly)
            {
                VerifyFiles(output, FrozenFiles, exact: false, token);
                var saved = TowerContractJson.Read<TowerBulkContract>(Path.Combine(output, ContractFile));
                if (HarnessJson.Hash(contract) != HarnessJson.Hash(saved))
                    throw new InvalidDataException("Resume requires the identical campaign definition, mode and limits.");
            }
            else
            {
                if (Path.Exists(output)) throw new IOException("Choose a new compact campaign output or explicitly resume.");
                token.ThrowIfCancellationRequested();
                Directory.CreateDirectory(output);
                var copied = TowerBundle.CopyContent(root, Path.Combine(output, "content"), token);
                if (HarnessJson.Hash(copied) != HarnessJson.Hash(content)) throw new InvalidDataException("Content changed while freezing campaign.");
                HarnessJson.WriteNew(Path.Combine(output, ContractFile), contract);
                HarnessJson.WriteNew(Path.Combine(output, "definition.json"), definition);
                HarnessJson.WriteNew(Path.Combine(output, "executable-files.json"), TowerBossStudy.RetainExecutable(output, execution));
                HarnessJson.WriteNew(Path.Combine(output, FrozenFiles), Inventory(output));
                Directory.CreateDirectory(Path.Combine(output, "batches"));
            }
            return new(output, contract, lease, token, progress, verifyOnly);
        }
        catch { lease.Dispose(); throw; }
    }

    public async Task<VerifiedTowerCompact> BatchAsync(string id, IReadOnlyList<TowerCompactCase> cases,
        Action<string, TowerTrial>? visit = null)
    {
        Token.ThrowIfCancellationRequested();
        if (!TowerBenchmark.SafeId(id) || !visited.Add(id)) throw new InvalidDataException("Duplicate or invalid campaign batch.");
        var path = Path.Combine(output, "batches", id);
        var planned = cases.Sum(c => c.Scenario.Seeds.Count);
        var definition = new TowerCompactDefinition(1, id, planned + Contract.Options.RetryReserve, Contract.Options.ChunkSize, cases);
        TowerCompactBundle.Validate(definition);
        if (verifyOnly || WasComplete || File.Exists(Path.Combine(path, TowerCompactBundle.ManifestFile)))
        {
            if (HarnessJson.Hash(TowerCompactBundle.Definition(path)) != HarnessJson.Hash(definition))
                throw new InvalidDataException("Campaign batch differs from deterministic recipe/seed reconstruction.");
        }
        else
        {
            CheckStorage();
            await TowerCompactBundle.CreateAsync(Root, definition, path, Token, message => {
                CheckStorage(); progress?.Invoke(id + ": " + message);
            }, Contract.Scope.Settings, executionMode: Contract.Options.ExecutionMode, resume: Directory.Exists(path),
                shareCampaignContent: true, beforeAttempt: () => {
                    Token.ThrowIfCancellationRequested();
                    if (attempts >= Contract.MaximumAttempts) throw new InvalidDataException("Campaign charged-attempt cap exhausted.");
                    attempts++;
                });
        }
        var saved = TowerCompactBundle.Verify(path, Token, visit: visit);
        if (saved.Plan.SharedContentPath != "../../content" || saved.Plan.ExecutionMode != Contract.Options.ExecutionMode
            || HarnessJson.Hash(saved.Scope.ContentHashes) != HarnessJson.Hash(Contract.Scope.ContentHashes)
            || HarnessJson.Hash(saved.Scope.Settings) != HarnessJson.Hash(Contract.Scope.Settings)
            || HarnessJson.Hash(saved.Scope.Execution) != HarnessJson.Hash(Contract.Scope.Execution))
            throw new InvalidDataException("Campaign batch content, execution or settings changed.");
        return saved;
    }

    public void Result<T>(string name, T value)
    {
        if (verifyOnly || WasComplete)
        {
            if (HarnessJson.Hash(HarnessJson.Read<T>(Path.Combine(output, name))) != HarnessJson.Hash(value))
                throw new InvalidDataException("Campaign result differs from reconstruction: " + name);
        }
        else Replace(Path.Combine(output, name), value);
    }

    public void TextResult(string name, string value)
    {
        var path = Path.Combine(output, name);
        if (verifyOnly || WasComplete)
        {
            if (File.ReadAllText(path) != value) throw new InvalidDataException("Campaign text differs from reconstruction: " + name);
        }
        else
        {
            using (var writer = new StreamWriter(new FileStream(path + ".pending", FileMode.CreateNew, FileAccess.Write))) writer.Write(value);
            File.Move(path + ".pending", path, true);
        }
    }

    public void Finish(int logicalTrials)
    {
        if (!Directory.GetDirectories(Path.Combine(output, "batches")).Select(Path.GetFileName).Order(StringComparer.Ordinal)
            .SequenceEqual(visited.Order(StringComparer.Ordinal))) throw new InvalidDataException("Campaign contains unvisited or extra batches.");
        // Re-read durable journals: conservatively charged setup failures are never invented as completed fights.
        attempts = visited.Sum(id => TowerCompactBundle.AttemptCount(Path.Combine(output, "batches", id)));
        Result("campaign-accounting.json", new TowerBulkAccounting(logicalTrials, attempts, attempts - logicalTrials, Contract.MaximumAttempts));
        if (verifyOnly || WasComplete) return;
        File.Delete(Path.Combine(output, "campaign-failure.json"));
        CheckStorage();
        HarnessJson.WriteNew(Path.Combine(output, FinalFiles), Inventory(output));
    }

    public void Failure(string status, string? error)
    {
        if (!verifyOnly && !WasComplete) Replace(Path.Combine(output, "campaign-failure.json"), new { Status = status, Error = error });
    }

    private void CheckStorage()
    {
        using var timing = TowerPerformanceTrace.Measure("campaign.check-storage");
        Token.ThrowIfCancellationRequested();
        if (StorageBytes(output, Token) > Contract.Options.MaximumBytes)
            throw new InvalidDataException("Compact campaign storage limit reached at a batch/chunk boundary.");
    }

    internal static long StorageBytes(string output, CancellationToken token = default)
    {
        using var timing = TowerPerformanceTrace.Measure("campaign.storage-bytes");
        long bytes = 0;
        foreach (var file in EnumerateFiles(output, token)) bytes = checked(bytes + file.Length);
        return bytes;
    }

    private static void Replace<T>(string path, T value)
    {
        var pending = path + ".pending";
        HarnessJson.WriteNew(pending, value);
        File.Move(pending, path, true);
    }

    internal static IReadOnlyList<string> Paths(string output) => EnumerateFiles(output, CancellationToken.None)
        .Select(file => file.FullName).ToArray();

    private static IEnumerable<FileInfo> EnumerateFiles(string output, CancellationToken token)
    {
        using var timing = TowerPerformanceTrace.Measure("campaign.enumerate-files");
        var stack = new Stack<string>(); stack.Push(output);
        while (stack.TryPop(out var directory))
        {
            token.ThrowIfCancellationRequested();
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked campaign directory.");
            // Enumeration supplies attributes and length together. Reuse those values for this scan only;
            // every boundary still scans the complete tree, including hidden files and pending writes.
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                token.ThrowIfCancellationRequested();
                var attributes = entry.Attributes;
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked campaign file.");
                if ((attributes & FileAttributes.Directory) != 0) stack.Push(entry.FullName); else yield return (FileInfo)entry;
            }
        }
    }

    private static Dictionary<string, string> Inventory(string output) => Paths(output)
        .ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash, StringComparer.Ordinal);

    private static void VerifyFiles(string output, string name, bool exact, CancellationToken token)
    {
        var actual = Paths(output).Select(p => Path.GetRelativePath(output, p).Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        var files = TowerContractJson.Read<Dictionary<string, string>>(Path.Combine(output, name));
        if (exact && !actual.Where(p => p != name).Order(StringComparer.Ordinal).SequenceEqual(files.Keys.Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Campaign final inventory differs.");
        foreach (var pair in files)
        {
            token.ThrowIfCancellationRequested();
            if (!actual.Contains(pair.Key) || !TowerContractJson.Hash(pair.Value) || HarnessJson.FileHash(Path.Combine(output, pair.Key)) != pair.Value)
                throw new InvalidDataException("Changed campaign artifact: " + pair.Key);
        }
    }

    public void Dispose() { cancellation.Dispose(); lease.Dispose(); }
}
