using System.Buffers.Binary;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness.ProcessFixture;

public sealed record FixedFamilyFixture(string Version, string Mode, TowerFixedFamilyRequest Request, TowerSettings Settings);

// Separate test executable: literal outcomes and entropy only; no combat runner or production entropy source.
public static class FixedFamilyFixtureHost
{
    public const string Version = "synthetic-fixed-family-confirmation-fixture-v1";
    public static void Entropy(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length/4; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4, 4), -10000-i);
    }
    public static TowerBattleInput Input(LoadoutScope scope, TowerScenario scenario, int seed) => new(1, scenario, null!,
        JsonSerializer.SerializeToElement(new { synthetic = true }), [], scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks,
        new(seed, MaxTicks: 6000, StartActiveAbilitiesOnCooldown: true, CaptureEventLog: false));

    public static Task Prepare(TowerFixedFamilyRequest q, TowerFixedFamilyFreeze freeze, TowerSettings settings, Action check, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); var root = TowerFixedFamilyConfirmation.P(q, "study");
        if (Path.Exists(root)) throw new IOException("No fixture resume."); Directory.CreateDirectory(root);
        foreach (var folder in new[] { "recipes", "battles", "exports" }) Directory.CreateDirectory(Path.Combine(root, folder));
        var scope = new LoadoutScope(q.Version, settings, ExecutionIdentity.Current(), freeze.Definition.ContentHashes, "gzip-json-v1");
        TowerFixedFamilyConfirmation.ValidateScope(freeze.Definition, scope);
        HarnessJson.WriteNew(Path.Combine(root, "scope.json"), scope); HarnessJson.WriteNew(Path.Combine(root, "freeze.json"), freeze); check();
        return Task.CompletedTask;
    }

    public static Task<TowerFixedFamilyStudy> Study(TowerFixedFamilyRequest q, TowerFixedFamilyFreeze freeze, TowerFixedFamilyPanel panel,
        string mode, Action<bool> attempt, Action check, CancellationToken ct, Action<string>? boundary = null)
    {
        var root = TowerFixedFamilyConfirmation.P(q, "study"); var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(root, "scope.json")); var ordinal = 0;
        return TowerFixedFamilyConfirmation.ExecuteArchive(q, freeze, panel, (arm, stage, scenario, seed, token) => {
            token.ThrowIfCancellationRequested(); boundary?.Invoke("attempt");
            var samples = TowerFixedFamilyConfirmation.Policy(q.Version).Samples;
            var team = ordinal/samples; var index = ordinal%samples;
            var won = q.Version == TowerFixedFamilyConfirmation.NeighborhoodRecognitionVersion
                ? index < (team < 3 ? new[] { 1000, 1100, 1200 }[team] : 1100+(team%7)*80)
                : q.Version == TowerFixedFamilyConfirmation.PreservationRecognitionVersion
                ? index < 65+(team%13)*9+TowerFixedFamilyConfirmation.RecognitionRoot(q.Version,team)
                : TowerFixedFamilyConfirmation.IsRecognition(q.Version)
                ? index < new[] { 70, 80, 100, 140, 140, 90, 90, 180, 180 }[team%9]+team/9
                : index < (mode == "negative" ? 2000 : team == 0 ? 2500 : team == 1 ? 2700 : 2000);
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome, outcome, "Literal fixed-family fixture", 1, 1, [], [], [], new CompactCombatTelemetry());
            var report = new TowerBattleReport(new BattleReport(1, scenario.Id, seed, 1,
                JsonSerializer.SerializeToElement(new { fixture = true }), summary, null), won, won ? 0 : 50, 1);
            var input = Input(scope, scenario, seed);
            var trial = new LoadoutTrial($"trial-{++ordinal:D6}", stage, HarnessJson.Hash(scenario), seed, HarnessJson.Hash(input), TowerLoadoutArchive.Key(scope, arm, input));
            var recipe = Path.Combine(root, "recipes", trial.Recipe+".json"); if (!File.Exists(recipe)) HarnessJson.WriteNew(recipe, scenario);
            TowerLoadoutArchive.WriteBattle(root, trial.Id, report, scope.ReportStorage);
            File.AppendAllText(Path.Combine(root, "trials.jsonl"), JsonSerializer.Serialize(trial, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false })+"\n");
            return Task.FromResult((trial, report));
        }, attempt, check, ct);
    }

    public static Task<TowerFixedFamilyStudy> Verify(TowerFixedFamilyRequest q, CancellationToken ct)
        => TowerFixedFamilyConfirmation.VerifyStudy(q, ct, Input);

    public static async Task<int> Run(string command, string path)
    {
        var fixture = TowerContractJson.Read<FixedFamilyFixture>(path); var q = fixture.Request;
        if (fixture.Version != Version || !Path.GetFileName(q.RegistryRoot).StartsWith("tower-fixed-family-fixture-", StringComparison.Ordinal)
            || !string.Equals(Path.GetDirectoryName(q.RegistryRoot), Path.TrimEndingDirectorySeparator(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
            || Directory.EnumerateFileSystemEntries(q.ContentRoot).Any()) throw new InvalidDataException("Only isolated synthetic fixtures with empty content roots are supported.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Fixed-team fixture entered combat.")).Activate();
        var d = TowerContractJson.Read<TowerFixedFamilyDefinition>(q.DefinitionPath);
        if (command == "fixed-family-parent")
        {
            var result = await TowerFixedFamilyConfirmation.RunWithWorker(q, _ => FixtureHost.Start("fixed-family-worker", path), boundary: Boundary);
            Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return result.IntegrityStatus == "Verified" ? 0 : 2;
        }
        if (command == "fixed-family-worker") return await TowerFixedFamilyConfirmation.WorkerWithOperation(q.OutputRoot, (request, launch, ct) =>
            TowerFixedFamilyConfirmation.RunOperation(request, launch, token => new(d,
                TowerRefinementComparisonLaunch.Refresh(q.RegistryRoot, q.OutputRoot, q.RequiredHistory, d.ExcludedCombatSeeds.ToArray(), token)),
                (f, check, token) => Prepare(request, f, fixture.Settings, check, token),
                (f, panel, attempt, check, token) => Study(request, f, panel, fixture.Mode, attempt, check, token, Boundary),
                token => Verify(request, token), ct, Boundary, Entropy), default);
        throw new InvalidDataException("Unknown fixed-family fixture command.");

        void Boundary(string stage)
        {
            if (fixture.Mode == stage+"-hang" || fixture.Mode == "storage-hang" && stage == "entropy-pending")
            {
                HarnessJson.WriteNew(Path.Combine(q.OutputRoot, "fixture-ready.json"), new { stage, pid = Environment.ProcessId });
                if (fixture.Mode == "storage-hang") { using var file = new FileStream(Path.Combine(q.OutputRoot, "overflow.bin"), FileMode.CreateNew); file.SetLength(q.MaximumBytes-q.PriorBytes); }
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
