using System.Buffers.Binary;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness.ProcessFixture;

public sealed record DiagnosticFixture(string Version, string Mode, TowerSelectionDiagnosticRequest Request, BossGenerationMechanics Mechanics);

// Literal evidence only. This executable never constructs a TowerBattleRunner or draws entropy.
public static class DiagnosticFixtureHost
{
    public const string Version = "synthetic-selection-diagnostic-fixture-v1";
    public static readonly TowerSettings Settings = new(new(), 30);
    public static int Candidate(string stage, int ordinal) => FixtureHost.AllocationCandidate(stage, ordinal);
    public static void Entropy(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length/4; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i*4, 4), -10000-i);
    }

    public static async Task<TowerDiagnosticStudy> Study(TowerSelectionDiagnosticRequest q, TowerDiagnosticSearchBinding binding,
        BossGenerationMechanics mechanics, string mode, IReadOnlyDictionary<string, string> history, Action<bool> attempt,
        Func<string> hash, Action<string> phase, Action check, CancellationToken ct, Action<string>? boundary = null)
    {
        var root = Path.Combine(q.Operation.OutputRoot, "study"); Directory.CreateDirectory(root);
        foreach (var folder in new[] { "recipes", "battles", "exports" }) Directory.CreateDirectory(Path.Combine(root, folder));
        void Save(string name, object value) { check(); TowerSelectionDiagnostic.Storage(q).Put("study/"+name, value); }
        var d = binding.Definition; var scope = new LoadoutScope(TowerSelectionDiagnostic.Version, Settings, ExecutionIdentity.Current(), d.ContentHashes, "gzip-json-v1");
        Save("definition.json", d); Save("scope.json", scope); Save("search-binding.json", binding);
        Save("generation-inputs.json", TowerBossDiscovery.CopyGenerationInputs(d)); Save("generation-mechanics.json", mechanics);
        var ordinal = 0; TowerDiagnosticFreeze? frozen = null;
        var report = await TowerSelectionDiagnostic.Execute(binding, mechanics, (arm, stage, scenario, seed, token) => {
            token.ThrowIfCancellationRequested(); var party = TowerPartySelection.Choice("fixture", scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            var anchor = d.Starts.Any(s => s.Party.Id == party.Id); var index = scenario.Seeds.ToList().IndexOf(seed);
            var selected = frozen?.PrimaryId == party.Id;
            var won = stage switch {
                "discovery" => index < (anchor ? 3 : 5),
                "selection" => index < ((mode == "incumbent" ? anchor : !anchor) ? 26 : 23),
                _ => index < (mode == "negative" ? 500 : selected ? 400 : 650) };
            var outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat;
            var summary = new BattleSummary(outcome, outcome, "Literal diagnostic fixture", 1, 1,
                [new SimpleCombatEntity("f", "f", "", 10, 0)], [], [], new CompactCombatTelemetry());
            var battle = new TowerBattleReport(new BattleReport(1, scenario.Id, seed, 1,
                JsonSerializer.SerializeToElement(new { fixture = true }), summary, null), won,
                Convert.ToInt32(party.Id[..4], 16)/655.35m, 1);
            var trial = new LoadoutTrial($"trial-{++ordinal:D6}", stage, HarnessJson.Hash(scenario), seed, new string('a', 64), new string('b', 64));
            var recipe = Path.Combine(root, "recipes", trial.Recipe+".json");
            if (!File.Exists(recipe)) HarnessJson.WriteNew(recipe, scenario);
            TowerLoadoutArchive.WriteBattle(root, trial.Id, battle, scope.ReportStorage);
            File.AppendAllText(Path.Combine(root, "trials.jsonl"), JsonSerializer.Serialize(trial, new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false })+"\n");
            return Task.FromResult((trial, battle));
        }, Save, f => {
            frozen = f;
            return TowerSelectionDiagnostic.ReserveConfirmation(q, binding, f, history, check, ct, boundary, Entropy);
        }, hash, attempt, phase, ct);
        foreach (var n in report.Freeze.Nominees) Save("exports/"+n.PartyId+".json", n.Scenario);
        TowerSelectionDiagnostic.Event(q, "MeasurementCompleted", HarnessJson.Hash(report), 4640);
        TowerSelectionDiagnostic.Seal(root); return report;
    }

    public static Task<TowerDiagnosticStudy> Verify(TowerSelectionDiagnosticRequest q, BossGenerationMechanics mechanics, CancellationToken ct)
        => TowerSelectionDiagnostic.VerifyStudy(q, ct, (_, scope, trials) => {
            var ordinal = 0;
            return (mechanics, (_, _, scenario, _, token) => {
                token.ThrowIfCancellationRequested(); var trial = trials[ordinal++];
                TowerSelectionDiagnostic.Match(q, "study/recipes/"+trial.Recipe+".json", scenario);
                return Task.FromResult((trial, TowerLoadoutArchive.ReadBattle(TowerSelectionDiagnostic.P(q, "study"), trial.Id, scope.ReportStorage)));
            });
        }, Candidate);

    public static async Task<int> Run(string command, string path)
    {
        var fixture = HarnessJson.Read<DiagnosticFixture>(path); var q = fixture.Request;
        if (fixture.Version != Version || !Path.GetFileName(q.Operation.RegistryRoot).StartsWith("tower-diagnostic-fixture-", StringComparison.Ordinal)
            || Path.GetDirectoryName(q.Operation.RegistryRoot) != Path.TrimEndingDirectorySeparator(Path.GetTempPath()))
            throw new InvalidDataException("Only isolated literal diagnostic fixtures are supported.");
        var template = TowerBossDiscovery.Read(q.Operation.DefinitionPath);
        if (template.ContentHashes.Values.Any(h => h != new string('a', 64)) || template.Contexts.Single().Id != "fixture")
            throw new InvalidDataException("Real content is forbidden in the fixture host.");
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Diagnostic fixture entered combat.")).Activate();
        if (command == "diagnostic-parent")
        {
            var result = await TowerSelectionDiagnostic.RunWithWorker(q, _ => FixtureHost.Start("diagnostic-worker", path));
            Console.WriteLine(JsonSerializer.Serialize(result, HarnessJson.Options)); return result.IntegrityStatus == "Verified" ? 0 : 2;
        }
        if (command == "diagnostic-worker") return await TowerSelectionDiagnostic.WorkerWithOperation(q.Operation.OutputRoot, (request, launch, ct) =>
            TowerSelectionDiagnostic.RunOperation(request, launch, token => new(template,
                TowerRefinementComparisonLaunch.Refresh(q.Operation.RegistryRoot, q.Operation.OutputRoot, q.Operation.RequiredHistory, template.ExcludedCombatSeeds.ToArray(), token)),
                (binding, history, attempt, hash, phase, check, token) => Study(request, binding, fixture.Mechanics, fixture.Mode, history, attempt, hash, phase, check, token, Boundary),
                token => Verify(request, fixture.Mechanics, token), ct, Boundary, Candidate), default);
        throw new InvalidDataException("Unknown diagnostic fixture command.");

        void Boundary(string stage)
        {
            if (fixture.Mode == stage+"-hang" || fixture.Mode == "storage-hang" && stage == "search-pending")
            {
                HarnessJson.WriteNew(Path.Combine(q.Operation.OutputRoot, "fixture-ready.json"), new { stage, pid = Environment.ProcessId });
                if (fixture.Mode == "storage-hang") { using var file = new FileStream(Path.Combine(q.Operation.OutputRoot, "overflow.bin"), FileMode.CreateNew); file.SetLength(q.Operation.MaximumBytes); }
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
