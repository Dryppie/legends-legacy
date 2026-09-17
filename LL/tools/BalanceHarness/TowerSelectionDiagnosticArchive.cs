using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public static partial class TowerSelectionDiagnostic
{
    internal static async Task<TowerDiagnosticStudy> RunStudy(TowerSelectionDiagnosticRequest q, TowerDiagnosticSearchBinding binding,
        IReadOnlyDictionary<string, string> history, Action<bool> attempt, Func<string> attemptsHash,
        Action<string> phase, Action check, CancellationToken ct)
    {
        var d = binding.Definition; TowerBossDiscovery.ValidateDiagnosticContent(q.Operation.ContentRoot, d, false);
        var output = P(q, "study"); Require(!Path.Exists(output), "No study retry or resume.");
        Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles", "exports" }) Directory.CreateDirectory(Path.Combine(output, folder));
        try
        {
            var settings = TowerBundle.ReadSettings(q.Operation.ContentRoot);
            var scope = new LoadoutScope(Version, settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(q.Operation.ContentRoot, Path.Combine(output, "content"), ct), "gzip-json-v1");
            Require(HarnessJson.Hash(scope.ContentHashes) == HarnessJson.Hash(d.ContentHashes) && HarnessJson.Hash(scope.Execution) == d.ExecutionHash
                && HarnessJson.Hash(settings) == d.SettingsHash, "Changed diagnostic runtime or content.");
            void Save(string name, object value) { check(); Storage(q).Put("study/"+name, value); }
            Save("definition.json", d); Save("scope.json", scope); Save("search-binding.json", binding);
            var inputs = TowerBossDiscovery.CopyGenerationInputs(d);
            Save("generation-inputs.json", inputs);
            var inventory = TowerBossInventory.Create(Path.Combine(output, "content"), settings.Threat);
            var mechanics = TowerBossPartyGenerator.FromInventory(inputs, inventory);
            Save("boss-profiles.json", inventory); Save("generation-mechanics.json", mechanics);
            Save("executable-files.json", TowerBossStudy.RetainExecutable(output, scope.Execution,
                q.Operation.MaximumBytes-q.Operation.PriorBytes-TowerPracticalSearch.CloseoutBytes-TowerBulkCampaign.StorageBytes(q.Operation.OutputRoot, ct), ct));
            var archive = new TowerLoadoutArchive(output, scope, TotalFights);
            var report = await Execute(binding, mechanics, async (arm, stage, scenario, seed, token) => {
                var result = await archive.EvaluateAsync(arm, stage, scenario, seed, token);
                Require(archive.CacheHits == 0, "Unexpected diagnostic cache reuse."); return result;
            }, Save, frozen => ReserveConfirmation(q, binding, frozen, history, check, ct), attemptsHash, attempt, phase, ct);
            foreach (var n in report.Freeze.Nominees) Save("exports/"+n.PartyId+".json", n.Scenario);
            Event(q, "MeasurementCompleted", HarnessJson.Hash(report), TotalFights);
            return report;
        }
        finally { Seal(output); }
    }

    internal static void Seal(string output) => HarnessJson.WriteNew(Path.Combine(output, "files.json"),
        TowerBulkCampaign.Paths(output).Where(p => Path.GetFullPath(p) != Path.GetFullPath(Path.Combine(output, "files.json")))
            .ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash));

    internal static string SearchAttemptHash(string path)
    {
        var lines = File.ReadLines(path).Take(2*SearchFights).ToArray();
        Require(lines.Length == 2*SearchFights, "Missing search attempt prefix.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines)+"\n")));
    }

    internal static string LiveAttemptHash(string path)
    {
        // The same worker has flushed and paused its writer at the freeze boundary.
        // Windows readers must share the still-open writer's access explicitly.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    internal static void VerifyAttemptsAndEvents(TowerSelectionDiagnosticRequest q, TowerDiagnosticSearchBinding binding, TowerDiagnosticStudy study, TowerDiagnosticPanel panel)
    {
        var lines = File.ReadAllLines(P(q, "attempts.jsonl")); Require(lines.Length == 2*TotalFights, "Incomplete attempt journal.");
        for (var i = 0; i < TotalFights; i++)
        foreach (var offset in new[] { 0, 1 })
        {
            var row = JsonSerializer.Deserialize<JsonElement>(lines[2*i+offset]);
            Require(HarnessJson.Hash(row) == HarnessJson.Hash(new { kind = offset == 0 ? "Started" : "Completed", ordinal = i+1 }), "Changed attempt order or fields.");
        }
        Require(File.ReadAllText(P(q, "attempts.jsonl")).EndsWith('\n') && study.Freeze.AttemptsHash == SearchAttemptHash(P(q, "attempts.jsonl")), "Changed attempt prefix.");
        var intent = HarnessJson.Read<JsonElement>(P(q, "entropy-intent.json"));
        var expected = new[] {
            new TowerDiagnosticEvent(1, "SearchReserved", HarnessJson.Hash(binding), 0),
            new TowerDiagnosticEvent(2, "NomineesFrozen", HarnessJson.Hash(study.Freeze), SearchFights),
            new TowerDiagnosticEvent(3, "EntropyStarted", HarnessJson.Hash(intent), SearchFights),
            new TowerDiagnosticEvent(4, "EntropyCompleted", panel.EntropyHash, SearchFights),
            new TowerDiagnosticEvent(5, "ConfirmationReserved", HarnessJson.Hash(panel), SearchFights),
            new TowerDiagnosticEvent(6, "ConfirmationStarted", HarnessJson.Hash(panel.Panel), SearchFights),
            new TowerDiagnosticEvent(7, "MeasurementCompleted", HarnessJson.Hash(study), TotalFights) };
        var events = File.ReadAllLines(P(q, "events.jsonl"));
        Require(File.ReadAllText(P(q, "events.jsonl")).EndsWith('\n') && events.Length == expected.Length, "Missing, repeated or torn phase events.");
        for (var i = 0; i < events.Length; i++) Require(HarnessJson.Hash(JsonSerializer.Deserialize<JsonElement>(events[i])) == HarnessJson.Hash(expected[i]), "Reordered phase event.");
    }

    internal static async Task<TowerDiagnosticStudy> VerifyStudy(TowerSelectionDiagnosticRequest q, CancellationToken ct,
        Func<TowerDiagnosticSearchBinding, LoadoutScope, IReadOnlyList<LoadoutTrial>, (BossGenerationMechanics Mechanics, TowerBossDiscoveryRun.Battle Battle)>? fixture = null,
        Func<string, int, int>? candidate = null)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Diagnostic verification cannot fight.")).Activate();
        var binding = VerifySearch(q, candidate); var d = binding.Definition; var output = P(q, "study");
        var trials = TowerLoadoutArchive.Verify(output, ct);
        Require(trials.Count == TotalFights, "Incomplete diagnostic archive.");
        var saved = TowerContractJson.Read<TowerDiagnosticStudy>(Path.Combine(output, "study.json"));
        var panel = VerifyPanel(q, binding, saved.Freeze); VerifyAttemptsAndEvents(q, binding, saved, panel);
        Match(q, "study/definition.json", d); Match(q, "study/search-binding.json", binding);
        var scope = TowerContractJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        var input = TowerBossDiscovery.CopyGenerationInputs(d); Match(q, "study/generation-inputs.json", input);
        Require(scope.Algorithm == Version && HarnessJson.Hash(scope.Execution) == d.ExecutionHash
            && HarnessJson.Hash(scope.Settings) == d.SettingsHash && HarnessJson.Hash(scope.ContentHashes) == HarnessJson.Hash(d.ContentHashes), "Changed diagnostic scope.");
        BossGenerationMechanics mechanics; TowerBossDiscoveryRun.Battle battle;
        if (fixture is not null) (mechanics, battle) = fixture(binding, scope, trials);
        else
        {
            Require(d.ExecutionHash == HarnessJson.Hash(ExecutionIdentity.Current()), "Use the retained producing runtime.");
            var root = Path.Combine(output, "content");
            foreach (var p in d.ContentHashes) Require(HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)) == p.Value, "Changed retained content.");
            var executable = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "executable-files.json"));
            var actual = TowerBulkCampaign.Paths(Path.Combine(output, "executable")).ToDictionary(p => Path.GetRelativePath(Path.Combine(output, "executable"), p).Replace('\\', '/'), HarnessJson.FileHash);
            Require(HarnessJson.Hash(executable) == HarnessJson.Hash(actual) && scope.Execution.AssemblyHashes.All(p => executable.GetValueOrDefault(p.Key+".dll") == p.Value), "Changed producing executable.");
            var inventory = TowerBossInventory.Create(root, scope.Settings.Threat); Match(q, "study/boss-profiles.json", inventory);
            mechanics = TowerBossPartyGenerator.FromInventory(input, inventory);
            var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat)); var index = 0;
            battle = (arm, stage, scenario, seed, token) => {
                token.ThrowIfCancellationRequested(); Require(index < trials.Count, "Missing trial."); var trial = trials[index++];
                var prepared = runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                Require(trial.InputHash == HarnessJson.Hash(prepared) && trial.CacheKey == TowerLoadoutArchive.Key(scope, arm, prepared), "Changed native prepared input.");
                Match(q, "study/recipes/"+trial.Recipe+".json", scenario);
                return Task.FromResult((trial, TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage)));
            };
        }
        Match(q, "study/generation-mechanics.json", mechanics);
        var rebuilt = await Execute(binding, mechanics, battle, (name, value) => Match(q, "study/"+name, value),
            frozen => { Require(HarnessJson.Hash(frozen) == HarnessJson.Hash(saved.Freeze), "Changed nominee freeze."); return panel.Panel.ToArray(); },
            () => saved.Freeze.AttemptsHash, _ => { }, _ => { }, ct);
        Require(HarnessJson.Hash(rebuilt) == HarnessJson.Hash(saved), "Study differs from saved reconstruction.");
        foreach (var n in rebuilt.Freeze.Nominees) Match(q, "study/exports/"+n.PartyId+".json", n.Scenario);
        Require(Directory.EnumerateFiles(Path.Combine(output, "exports")).Select(Path.GetFileName).Order()
            .SequenceEqual(rebuilt.Freeze.Nominees.Select(n => n.PartyId+".json").Order()), "Changed export inventory.");
        Require(Directory.EnumerateFiles(Path.Combine(output, "recipes")).Select(Path.GetFileName).Order()
            .SequenceEqual(trials.Select(t => t.Recipe+".json").Distinct().Order()), "Changed recipe inventory.");
        return rebuilt;
    }

    // Independent count audit: reads battle outcomes directly, never Execute or Assess.
    internal static TowerDiagnosticResult IndependentAudit(TowerSelectionDiagnosticRequest q, TowerDiagnosticStudy study, CancellationToken ct)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Independent audit cannot fight.")).Activate();
        var output = P(q, "study"); var trials = TowerLoadoutArchive.Verify(output, ct); var f = study.Freeze;
        Require(trials.Count == TotalFights && trials.Take(512).All(t => t.Stage == "discovery")
            && trials.Skip(512).Take(128).All(t => t.Stage == "selection") && trials.Skip(SearchFights).All(t => t.Stage == "confirmation"), "Changed stage counts.");
        var binding = TowerContractJson.Read<TowerDiagnosticSearchBinding>(P(q, "search-binding.json"));
        var panel = VerifyPanel(q, binding, f);
        var scope = TowerContractJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        var wins = new Dictionary<string, bool[]>(); var rates = new List<TowerDiagnosticRate>();
        foreach (var n in f.Nominees)
        {
            var block = trials.Skip(SearchFights+n.Ordinal*Samples).Take(Samples).ToArray();
            var scenario = n.Scenario with { Seeds = panel.Panel };
            Require(block.Select(t => t.Seed).SequenceEqual(panel.Panel) && block.All(t => t.Recipe == HarnessJson.Hash(scenario)), "Changed paired cell.");
            var outcomes = block.Select(t => { ct.ThrowIfCancellationRequested(); var report = TowerLoadoutArchive.ReadBattle(output, t.Id, scope.ReportStorage);
                Require(report.Battle.Seed == t.Seed && report.Battle.ScenarioId == scenario.Id && Enum.IsDefined(report.Battle.Summary.ContentOutcome)
                    && report.Succeeded == (report.Battle.Summary.ContentOutcome == BattleOutcome.Victory), "Changed saved outcome."); return report.Battle.Summary.ContentOutcome; }).ToArray();
            Require(outcomes.SequenceEqual(study.Evidence.Single(e => e.PartyId == n.PartyId).Trials.Select(t => t.Outcome)), "Study summary differs from battle outcomes.");
            wins.Add(n.PartyId, outcomes.Select(o => o == BattleOutcome.Victory).ToArray());
            var count = outcomes.Count(o => o == BattleOutcome.Victory); rates.Add(new(n.PartyId, count, TowerBalanceEvaluator.Wilson(count, Samples, Family)!));
        }
        var contrasts = new List<TowerDiagnosticContrast>();
        foreach (var n in f.Nominees.Where(n => n.PartyId != f.PrimaryId))
        {
            var gains = 0; var losses = 0;
            for (var i = 0; i < Samples; i++) { if (wins[n.PartyId][i] && !wins[f.PrimaryId][i]) gains++; if (!wins[n.PartyId][i] && wins[f.PrimaryId][i]) losses++; }
            var a = TowerBalanceEvaluator.Wilson(gains, Samples, Family)!; var b = TowerBalanceEvaluator.Wilson(losses, Samples, Family)!;
            var lower = a.Lower-b.Upper;
            contrasts.Add(new(n.PartyId, gains, losses, (gains-losses)/(double)Samples, lower, a.Upper-b.Lower,
                gains-losses >= 50 && lower > 0 && rates.Single(r => r.PartyId == n.PartyId).Estimate.Lower >= .10));
        }
        return new(Version, "Complete", "Verified", contrasts.Any(c => c.Qualifies) ? "SelectionMissDemonstrated" : "NoSelectionMissDemonstrated",
            f.PrimaryId, rates, contrasts, f.Nominees.Where(n => n.ReferenceIds.Count > 0).Select(n => n.PartyId).ToArray(), "NotAssessed", SamplingAssumption,
            HarnessJson.Hash(study), HarnessJson.FileHash(Path.Combine(output, "files.json")), "Frozen diagnostic completed; no promotion, retry or extension.");
    }
}
