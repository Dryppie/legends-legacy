using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerReferenceExplorationComparison
{
    private static async Task<ExplorationStudy> RunStudy(ExplorationRequest q, TowerBossDiscoveryDefinition template,
        ExplorationReservation allocation, Action check, CancellationToken ct)
    {
        var output = P(q, "study"); Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles" }) Directory.CreateDirectory(Path.Combine(output, folder));
        void Save(string name, object value) { check(); Storage(q).Put("study/" + name, value); }
        var settings = TowerBundle.ReadSettings(q.ContentRoot);
        var scope = new LoadoutScope(q.Version, settings, ExecutionIdentity.Current(),
            TowerBundle.CopyContent(q.ContentRoot, Path.Combine(output, "content"), ct), "gzip-json-v1");
        Require(HarnessJson.Hash(settings) == template.SettingsHash && HarnessJson.Hash(scope.Execution) == template.ExecutionHash
            && HarnessJson.Hash(scope.ContentHashes) == HarnessJson.Hash(template.ContentHashes), "Runtime or content changed after admission.");
        Save("scope.json", scope);
        var inventory = TowerBossInventory.Create(Path.Combine(output, "content"), settings.Threat);
        var inputs = TowerBossImprovement.Inputs(Bind(template, allocation.Selected, 0, false, q.Version));
        var mechanics = TowerBossPartyGenerator.FromInventory(inputs, inventory);
        Save("boss-profiles.json", inventory); Save("generation-mechanics.json", mechanics);
        Save("executable-files.json", TowerBossStudy.RetainExecutable(output, scope.Execution,
            NativeBytesFor(q.Version) - 4 * 1048576 - TowerBulkCampaign.StorageBytes(q.OutputRoot, ct), ct));
        var archive = new TowerLoadoutArchive(output, scope, FightLimit(q.Version));
        using (var attempts = new TowerPracticalSearch.Attempts(P(q, "attempts.jsonl"), FightLimit(q.Version), check))
        {
            var study = await Execute(template, allocation, mechanics, async (arm, stage, scenario, seed, token) => {
                var value = await archive.EvaluateAsync(arm, stage, scenario, seed, token);
                Require(archive.CacheHits == 0, "Unexpected cross-search or confirmation cache reuse."); return value;
            }, Save, attempts.Event, () => TowerSelectionDiagnostic.LiveAttemptHash(P(q, "attempts.jsonl")), ct);
            Require(attempts.Started == attempts.Completed && attempts.Completed == Assess(template, study, allocation).Fights,
                "Changed fight accounting.");
        }
        TowerSelectionDiagnostic.Seal(output);
        return TowerContractJson.Read<ExplorationStudy>(Path.Combine(output, "study.json"));
    }

    internal static string AttemptPrefix(string path)
    {
        var lines = File.ReadLines(path).Take(SearchFights * 2).ToArray();
        Require(lines.Length == SearchFights * 2, "Incomplete search attempt journal.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines) + "\n")));
    }

    internal static void VerifyAttempts(string path, ExplorationStudy study, int fights)
    {
        var lines = File.ReadAllLines(path);
        Require(lines.Length == 2 * fights && File.ReadAllText(path).EndsWith('\n')
            && AttemptPrefix(path) == study.Freeze.AttemptsHash, "Incomplete attempts or changed global freeze boundary.");
        for (var i = 0; i < fights; i++)
        for (var offset = 0; offset < 2; offset++)
            Require(HarnessJson.Hash(JsonSerializer.Deserialize<JsonElement>(lines[2 * i + offset]))
                == HarnessJson.Hash(new { kind = offset == 0 ? "Started" : "Completed", ordinal = i + 1 }), "Reordered or unmatched attempt.");
    }

    // Fixtures replace native input preparation only. They still traverse the real generator, both selectors,
    // global freeze, saved battle ledger, panels, attempts and arithmetic. Public verification has no fixture route.
    internal static async Task<ExplorationResult> VerifyStudy(ExplorationRequest q, CancellationToken ct,
        Func<LoadoutScope, IReadOnlyList<LoadoutTrial>, (BossGenerationMechanics Mechanics, TowerBossDiscoveryRun.Battle Battle)>? fixture = null)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Comparison verification cannot fight.")).Activate();
        var template = TowerBossDiscovery.Read(P(q, "template.json")); ValidateTemplate(template, q.Version);
        Require(HarnessJson.FileHash(P(q, "template.json")) == q.TemplateHash, "Changed saved template.");
        var allocation = VerifyReservation(q, template); var output = P(q, "study");
        TowerBulkCampaign.VerifyFiles(output, "files.json", true, ct);
        var trials = TowerLoadoutArchive.Verify(output, ct);
        Require(File.ReadAllText(Path.Combine(output, "trials.jsonl")).EndsWith('\n'), "Torn trial ledger.");
        var saved = TowerContractJson.Read<ExplorationStudy>(Path.Combine(output, "study.json"));
        var result = Assess(template, saved, allocation);
        Require(trials.Count == result.Fights, "Missing or extra fights.");
        VerifyAttempts(P(q, "attempts.jsonl"), saved, result.Fights);
        var scope = TowerContractJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        Require(scope.Algorithm == q.Version && scope.ReportStorage == "gzip-json-v1"
            && HarnessJson.Hash(scope.Execution) == template.ExecutionHash && HarnessJson.Hash(scope.Settings) == template.SettingsHash
            && HarnessJson.Hash(scope.ContentHashes) == HarnessJson.Hash(template.ContentHashes), "Changed saved scope.");
        BossGenerationMechanics mechanics; TowerBossDiscoveryRun.Battle battle;
        if (fixture is not null) (mechanics, battle) = fixture(scope, trials);
        else
        {
            ValidateCapture(P(q, "capture"), P(q, "capture/closeout"), template, scope.Execution, q.Version);
            Require(template.ExecutionHash == HarnessJson.Hash(ExecutionIdentity.Current()), "Use the retained producing runtime for native verification.");
            var content = Path.Combine(output, "content");
            foreach (var p in template.ContentHashes) Require(HarnessJson.FileHash(Path.Combine(content, "Data", p.Key)) == p.Value, "Changed retained content.");
            // CopyContent retains Data only; the authenticated scope above is the effective settings snapshot.
            var executable = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "executable-files.json"));
            var actual = TowerBulkCampaign.Paths(Path.Combine(output, "executable")).ToDictionary(p =>
                Path.GetRelativePath(Path.Combine(output, "executable"), p).Replace('\\', '/'), HarnessJson.FileHash);
            Require(HarnessJson.Hash(executable) == HarnessJson.Hash(actual)
                && scope.Execution.AssemblyHashes.All(p => executable.GetValueOrDefault(p.Key + ".dll") == p.Value), "Changed retained executable.");
            var captureFiles = HarnessJson.Read<Dictionary<string, string>>(P(q, "capture/files.json"));
            Require(captureFiles.Where(p => p.Key.StartsWith("runtime/", StringComparison.Ordinal)
                && !Path.GetFileName(p.Key).StartsWith("BalanceHarness", StringComparison.Ordinal))
                .All(p => actual.GetValueOrDefault(p.Key[8..]) == p.Value), "Changed captured runtime dependency.");
            var inventory = TowerBossInventory.Create(content, scope.Settings.Threat); Match(output, "boss-profiles.json", inventory);
            mechanics = TowerBossPartyGenerator.FromInventory(TowerBossImprovement.Inputs(Bind(template, allocation.Selected, 0, false, q.Version)), inventory);
            var runner = new TowerBattleRunner(content, new OfflineContent(content, scope.Settings.Threat)); var index = 0;
            TowerScenario? lastScenario = null; TowerBattleInput? prepared = null;
            battle = (arm, stage, scenario, seed, token) => {
                token.ThrowIfCancellationRequested(); Require(index < trials.Count, "Missing saved battle."); var trial = trials[index++];
                if (!ReferenceEquals(lastScenario, scenario))
                {
                    prepared = runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                    lastScenario = scenario;
                }
                var input = prepared! with { Rules = prepared!.Rules with { RandomSeed = seed } };
                Require(trial.InputHash == HarnessJson.Hash(input) && trial.CacheKey == TowerLoadoutArchive.Key(scope, arm, input), "Changed prepared input or arm.");
                Match(output, "recipes/" + trial.Recipe + ".json", scenario);
                var report = TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage);
                Require(report.Battle.Summary.DurationTicks <= input.Rules.MaxTicks, "Battle exceeded its prepared tick limit.");
                return Task.FromResult((trial, report));
            };
        }
        Match(output, "generation-mechanics.json", mechanics);
        var rebuilt = await Execute(template, allocation, mechanics, battle, (name, value) => Match(output, name, value),
            _ => { }, () => AttemptPrefix(P(q, "attempts.jsonl")), ct);
        Require(HarnessJson.Hash(saved) == HarnessJson.Hash(rebuilt), "Native saved reconstruction differs.");
        Require(Directory.EnumerateFiles(Path.Combine(output, "recipes")).Select(Path.GetFileName).Order()
            .SequenceEqual(trials.Select(t => t.Recipe + ".json").Distinct().Order())
            && Directory.EnumerateFiles(Path.Combine(output, "battles")).Select(Path.GetFileName).Order()
            .SequenceEqual(trials.Select(t => t.Id + ".json.gz").Order()), "Changed recipe or battle inventory.");
        return result;
    }
}
