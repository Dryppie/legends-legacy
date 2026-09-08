using System.Runtime.InteropServices;
using System.Text.Json;
using Domain.Models.Combat;
using Services.LL.Combat.Engine;

namespace BalanceHarness;

public sealed record ExecutionIdentity(
    string Runtime, string OperatingSystem, string Architecture,
    IReadOnlyDictionary<string, string> AssemblyHashes)
{
    public static ExecutionIdentity Current() => new(
        RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription,
        RuntimeInformation.ProcessArchitecture.ToString(),
        new[] { typeof(RunBundle).Assembly, typeof(CombatEngineExecutor).Assembly,
                typeof(CombatResult).Assembly, typeof(Common.Randomness.StableRandom).Assembly,
                typeof(Application.Interfaces.Services.LL.Essences.IEssenceCombatLoadoutResolver).Assembly }
            .OrderBy(x => x.GetName().Name, StringComparer.Ordinal)
            .ToDictionary(x => x.GetName().Name!, x => HarnessJson.FileHash(x.Location)));
}

public sealed record RunManifest(int SchemaVersion, string InputHash,
    IReadOnlyDictionary<string, string> ContentHashes, ExecutionIdentity Execution);

public static class RunBundle
{
    public static async Task<BattleReport> CreateAsync(string apiContentRoot, string scenarioPath,
        string outputDirectory, int seed, bool detailed, CancellationToken cancellationToken)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output))
            throw new IOException($"Output already exists; choose a new run directory: {output}");
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        try
        {
            var snapshotRoot = Path.Combine(output, "content");
            var hashes = CopyContent(apiContentRoot, snapshotRoot, cancellationToken);
            var (threat, cadence) = ReadCombatSettings(apiContentRoot);
            var scenario = HarnessJson.Read<IdleScenario>(scenarioPath);
            var content = new OfflineContent(snapshotRoot, threat);
            var input = content.CreateInput(scenario, seed, threat, cadence);
            HarnessJson.WriteNew(Path.Combine(output, "input.json"), input);
            HarnessJson.WriteNew(Path.Combine(output, "manifest.json"),
                new RunManifest(1, HarnessJson.Hash(input), hashes, ExecutionIdentity.Current()));
            var report = await new IdleBattleRunner(content).RunAsync(input, detailed, cancellationToken);
            HarnessJson.WriteNew(Path.Combine(output, "result.json"), report);
            return report;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            {
                Status = exception is OperationCanceledException ? "Cancelled" : "Invalid",
                Seed = seed, ErrorType = exception.GetType().Name, exception.Message
            });
            throw;
        }
    }

    public static async Task<BattleReport> ReplayAsync(string runDirectory, bool detailed,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = HarnessJson.Read<RunManifest>(Path.Combine(runDirectory, "manifest.json"));
        var input = HarnessJson.Read<IdleBattleInput>(Path.Combine(runDirectory, "input.json"));
        Verify(runDirectory, manifest, HarnessJson.Hash(input), cancellationToken);
        var content = new OfflineContent(Path.Combine(runDirectory, "content"), input.ThreatAndTanking);
        var report = await new IdleBattleRunner(content).RunAsync(input, detailed, cancellationToken);
        var originalPath = Path.Combine(runDirectory, "result.json");
        if (File.Exists(originalPath))
            VerifyResult(HarnessJson.Read<BattleReport>(originalPath), report);
        return report;
    }

    internal static IReadOnlyDictionary<string, string> CopyContent(
        string apiContentRoot, string snapshotRoot, CancellationToken cancellationToken)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var relative in OfflineContent.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destination = Path.Combine(snapshotRoot, "Data", relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(apiContentRoot, "Data", relative), destination, overwrite: false);
            hashes.Add(relative, HarnessJson.FileHash(destination));
        }
        return hashes;
    }

    internal static (ThreatAndTankingOptions Threat, double Cadence) ReadCombatSettings(string apiContentRoot)
    {
        // Select only non-secret combat settings; never copy appsettings into artifacts.
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(apiContentRoot, "appsettings.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var settings = document.RootElement.GetProperty("Combat");
        var threat = settings.GetProperty("ThreatAndTanking").Deserialize<ThreatAndTankingOptions>(HarnessJson.Options)
            ?? throw new InvalidDataException("Missing threat/tanking settings.");
        return (threat, settings.GetProperty("IdleProgression").GetProperty("EncounterCadenceSeconds").GetDouble());
    }

    internal static void Verify(string runDirectory, RunManifest manifest, string inputHash,
        CancellationToken cancellationToken)
    {
        VerifySnapshot(runDirectory, manifest, inputHash, cancellationToken);
        if (HarnessJson.Hash(ExecutionIdentity.Current()) != HarnessJson.Hash(manifest.Execution))
            throw new InvalidDataException("Replay requires the original assemblies, runtime and platform. Rebuild the original revision/configuration or start a new run.");
    }

    // Reading historical measurements must not require executing their old assemblies.
    internal static void VerifySnapshot(string runDirectory, RunManifest manifest, string inputHash,
        CancellationToken cancellationToken)
    {
        if (manifest.SchemaVersion != 1 || inputHash != manifest.InputHash)
            throw new InvalidDataException("Unsupported or modified run inputs.");
        if (!manifest.ContentHashes.Keys.Order(StringComparer.Ordinal)
            .SequenceEqual(OfflineContent.Files.Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Unexpected content snapshot files.");
        // Only allowlisted paths are read, even when the manifest came from elsewhere.
        foreach (var relative in OfflineContent.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (HarnessJson.FileHash(Path.Combine(runDirectory, "content", "Data", relative))
                != manifest.ContentHashes[relative])
                throw new InvalidDataException($"Modified content snapshot: {relative}");
        }
    }

    internal static void VerifyResult(BattleReport original, BattleReport report)
    {
        if (original.ScenarioId != report.ScenarioId || original.Seed != report.Seed
            || HarnessJson.Hash(report.PreparedParticipants) != HarnessJson.Hash(original.PreparedParticipants)
            || HarnessJson.Hash(report.Summary) != HarnessJson.Hash(original.Summary))
            throw new InvalidDataException("Replay diverged from the saved preparation or combat result.");
    }
}
