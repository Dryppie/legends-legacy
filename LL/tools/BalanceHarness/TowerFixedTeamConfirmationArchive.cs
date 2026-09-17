using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public static partial class TowerFixedTeamConfirmation
{
    internal static void Seal(string root) => HarnessJson.WriteNew(Path.Combine(root, "files.json"),
        TowerBulkCampaign.Paths(root).Where(p => Path.GetFullPath(p) != Path.GetFullPath(Path.Combine(root, "files.json")))
            .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash));

    internal static async Task PrepareArchive(TowerFixedTeamRequest q, TowerFixedTeamFreeze freeze, Action check, CancellationToken ct)
    {
        var root = P(q, "study"); Require(!Path.Exists(root), "No study retry or resume."); Directory.CreateDirectory(root);
        foreach (var folder in new[] { "recipes", "battles", "exports" }) Directory.CreateDirectory(Path.Combine(root, folder));
        var settings = TowerBundle.ReadSettings(q.ContentRoot);
        var scope = new LoadoutScope(Version, settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(q.ContentRoot, Path.Combine(root, "content"), ct), "gzip-json-v1");
        ValidateScope(freeze.Definition, scope);
        Storage(q).Put("study/scope.json", scope); Storage(q).Put("study/freeze.json", freeze); check();
        Storage(q).Put("study/executable-files.json", TowerBossStudy.RetainExecutable(root, scope.Execution,
            q.MaximumBytes-q.PriorBytes-TowerPracticalSearch.CloseoutBytes-TowerBulkCampaign.StorageBytes(q.OutputRoot, ct), ct));
        // Preparation uses a local literal label only. It never executes combat or reserves a value.
        var runner = new TowerBattleRunner(Path.Combine(root, "content"), new OfflineContent(Path.Combine(root, "content"), settings.Threat));
        foreach (var team in freeze.Definition.Teams)
        {
            check(); var scenario = team.Scenario with { Seeds = new[] { 0 } };
            await runner.PrepareAsync(runner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks), ct);
        }
        check();
    }

    internal static void ValidateScope(TowerFixedTeamDefinition d, LoadoutScope scope)
        => Require(scope.Algorithm == Version && scope.ReportStorage == "gzip-json-v1"
            && HarnessJson.Hash(scope.Execution) == d.ExecutionHash && HarnessJson.Hash(scope.Settings) == d.SettingsHash
            && HarnessJson.Hash(scope.ContentHashes) == HarnessJson.Hash(d.ContentHashes), "Changed fixed-team runtime, settings, content or encoding.");

    private static Task<TowerFixedTeamStudy> RunStudy(TowerFixedTeamRequest q, TowerFixedTeamFreeze freeze, TowerFixedTeamPanel panel,
        Action<bool> attempt, Action check, CancellationToken ct)
    {
        var scope = TowerContractJson.Read<LoadoutScope>(P(q, "study/scope.json"));
        var archive = new TowerLoadoutArchive(P(q, "study"), scope, TotalFights);
        return ExecuteArchive(q, freeze, panel, async (arm, stage, scenario, seed, token) => {
            var result = await archive.EvaluateAsync(arm, stage, scenario, seed, token);
            Require(archive.CacheHits == 0, "Unexpected cache reuse."); return result;
        }, attempt, check, ct);
    }

    internal static async Task<TowerFixedTeamStudy> ExecuteArchive(TowerFixedTeamRequest q, TowerFixedTeamFreeze freeze, TowerFixedTeamPanel panel,
        TowerBossDiscoveryRun.Battle battle, Action<bool> attempt, Action check, CancellationToken ct)
    {
        Event(q, "ConfirmationStarted", HarnessJson.Hash(panel.Panel), 0);
        try
        {
            var study = await Execute(freeze, panel.Panel, battle, attempt, ct); check();
            Storage(q).Put("study/study.json", study);
            foreach (var team in freeze.Definition.Teams) Storage(q).Put("study/exports/"+team.PartyId+".json", team.Scenario);
            Event(q, "MeasurementCompleted", HarnessJson.Hash(study), TotalFights); return study;
        }
        finally { Seal(P(q, "study")); }
    }

    internal static async Task<TowerFixedTeamStudy> VerifyStudy(TowerFixedTeamRequest q, CancellationToken ct,
        Func<LoadoutScope, TowerScenario, int, TowerBattleInput>? fixtureInput = null)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Confirmation verification cannot fight.")).Activate();
        var freeze = VerifyFreeze(q); var panel = VerifyPanel(q, freeze); var root = P(q, "study");
        var trials = TowerLoadoutArchive.Verify(root, ct); Require(trials.Count == TotalFights, "Incomplete archive.");
        var saved = TowerContractJson.Read<TowerFixedTeamStudy>(Path.Combine(root, "study.json")); VerifyJournals(q, saved, panel);
        Match(q, "study/freeze.json", freeze);
        var scope = TowerContractJson.Read<LoadoutScope>(Path.Combine(root, "scope.json")); ValidateScope(freeze.Definition, scope);
        Func<TowerScenario, int, TowerBattleInput> prepare;
        if (fixtureInput is not null) prepare = (scenario, seed) => fixtureInput(scope, scenario, seed);
        else
        {
            Require(freeze.Definition.ExecutionHash == HarnessJson.Hash(ExecutionIdentity.Current()), "Use the retained producing runtime.");
            var content = Path.Combine(root, "content");
            foreach (var p in freeze.Definition.ContentHashes) Require(HarnessJson.FileHash(Path.Combine(content, "Data", p.Key)) == p.Value, "Changed retained content.");
            var executable = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(root, "executable-files.json"));
            var actual = TowerBulkCampaign.Paths(Path.Combine(root, "executable")).ToDictionary(p => Path.GetRelativePath(Path.Combine(root, "executable"), p).Replace('\\', '/'), HarnessJson.FileHash);
            Require(HarnessJson.Hash(executable) == HarnessJson.Hash(actual)
                && scope.Execution.AssemblyHashes.All(p => executable.GetValueOrDefault(p.Key+".dll") == p.Value), "Changed producing executable.");
            var runner = new TowerBattleRunner(content, new OfflineContent(content, scope.Settings.Threat));
            prepare = (scenario, seed) => runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
        }
        Require(File.ReadAllText(Path.Combine(root, "trials.jsonl")).EndsWith('\n'), "Torn trial journal.");
        foreach (var chunk in Chunks(freeze.Definition, panel.Panel))
            Match(q, "study/recipes/"+HarnessJson.Hash(chunk.Scenario)+".json", chunk.Scenario);
        var ordinal = 0;
        foreach (var line in File.ReadLines(Path.Combine(root, "trials.jsonl")))
            Require(HarnessJson.Hash(JsonSerializer.Deserialize<JsonElement>(line)) == HarnessJson.Hash(trials[ordinal++]), "Unknown trial fields.");
        ordinal = 0;
        var rebuilt = await Execute(freeze, panel.Panel, (arm, stage, scenario, seed, token) => {
            token.ThrowIfCancellationRequested(); Require(ordinal < trials.Count, "Missing recorded trial."); var trial = trials[ordinal++];
            var input = prepare(scenario, seed);
            Require(trial.InputHash == HarnessJson.Hash(input) && trial.CacheKey == TowerLoadoutArchive.Key(scope, arm, input), "Changed prepared input or cache binding.");
            return Task.FromResult((trial, TowerLoadoutArchive.ReadBattle(root, trial.Id, scope.ReportStorage)));
        }, _ => { }, ct);
        Require(ordinal == TotalFights && HarnessJson.Hash(saved) == HarnessJson.Hash(rebuilt), "Recorded study differs from native reconstruction.");
        foreach (var team in freeze.Definition.Teams) Match(q, "study/exports/"+team.PartyId+".json", team.Scenario);
        Require(Directory.EnumerateFiles(Path.Combine(root, "exports")).Select(Path.GetFileName).Order()
            .SequenceEqual(freeze.Definition.Teams.Select(t => t.PartyId+".json").Order()), "Changed export inventory.");
        Require(Directory.EnumerateFiles(Path.Combine(root, "recipes")).Select(Path.GetFileName).Order()
            .SequenceEqual(Chunks(freeze.Definition, panel.Panel).Select(c => HarnessJson.Hash(c.Scenario)+".json").Order()), "Changed 18-recipe inventory.");
        Require(Directory.EnumerateFiles(Path.Combine(root, "battles")).Select(Path.GetFileName).Order()
            .SequenceEqual(trials.Select(t => t.Id+".json.gz").Order()), "Changed battle inventory.");
        return rebuilt;
    }

    // Separate direct-outcome count implementation: does not call Execute or Assess.
    internal static TowerFixedTeamResult IndependentAudit(TowerFixedTeamRequest q, TowerFixedTeamStudy study, CancellationToken ct)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Independent confirmation audit cannot fight.")).Activate();
        var freeze = VerifyFreeze(q); var panel = VerifyPanel(q, freeze); var teams = freeze.Definition.Teams;
        Require(study.Version == Version && HarnessJson.Hash(study.Freeze) == HarnessJson.Hash(freeze)
            && study.Evidence.Select(e => e.PartyId).SequenceEqual(teams.Select(t => t.PartyId)), "Changed direct-audit family.");
        var root = P(q, "study"); var trials = TowerLoadoutArchive.Verify(root, ct); Require(trials.Count == TotalFights, "Incomplete direct audit.");
        var wins = new[] { new bool[Samples], new bool[Samples], new bool[Samples] }; var ordinal = 0;
        foreach (var chunk in Chunks(freeze.Definition, panel.Panel))
        {
            var recipeHash = HarnessJson.Hash(chunk.Scenario);
            foreach (var seed in chunk.Scenario.Seeds)
            {
                ct.ThrowIfCancellationRequested(); var trial = trials[ordinal];
                Require(trial.Id == $"trial-{ordinal+1:D6}" && trial.Stage == "confirmation" && trial.Seed == seed && trial.Recipe == recipeHash, "Changed direct-audit trial order.");
                var report = TowerLoadoutArchive.ReadBattle(root, trial.Id, "gzip-json-v1"); ValidateReport(report, chunk.Scenario, seed);
                var cell = study.Evidence[chunk.TeamOrdinal].Trials;
                Require(cell.Count == Samples && cell[ordinal%Samples] == new TowerBalanceTrial(seed, report.Battle.Summary.ContentOutcome), "Summary differs from direct outcomes.");
                wins[chunk.TeamOrdinal][ordinal%Samples] = report.Battle.Summary.ContentOutcome == BattleOutcome.Victory; ordinal++;
            }
        }
        var rates = new List<TowerDiagnosticRate>(); var contrasts = new List<TowerDiagnosticContrast>();
        for (var i = 0; i < 3; i++)
        {
            var count = wins[i].Count(w => w); rates.Add(new(teams[i].PartyId, count, TowerBalanceEvaluator.Wilson(count, Samples, Family)!));
        }
        for (var anchor = 1; anchor <= 2; anchor++)
        {
            var gains = 0; var losses = 0;
            for (var i = 0; i < Samples; i++) { if (wins[0][i] && !wins[anchor][i]) gains++; if (!wins[0][i] && wins[anchor][i]) losses++; }
            var g = TowerBalanceEvaluator.Wilson(gains, Samples, Family)!; var l = TowerBalanceEvaluator.Wilson(losses, Samples, Family)!;
            contrasts.Add(new(teams[anchor].PartyId, gains, losses, (gains-losses)/(double)Samples,
                g.Lower-l.Upper, g.Upper-l.Lower, gains-losses >= 275 && g.Lower > l.Upper && rates[0].Estimate.Lower >= .10));
        }
        var pass = contrasts[0].Qualifies && contrasts[1].Qualifies; var controls = teams.Skip(1).Select(t => t.PartyId).ToArray();
        return new(Version, "Complete", "Verified", pass ? "StrongerFixedTeamConfirmed" : "StrengthNotDemonstrated", pass ? "AdoptFixedTeam" : "Hold",
            teams[0].PartyId, rates, contrasts, pass ? [teams[0].PartyId] : controls, controls, "NotAssessed", SamplingAssumption,
            HarnessJson.Hash(study), HarnessJson.FileHash(Path.Combine(root, "files.json")), "One complete fixed panel; no retry, extension, method-reliability or global-optimality claim.");
    }
}
