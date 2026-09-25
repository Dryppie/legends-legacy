using System.Text.Json;
using Domain.Models.Combat;

namespace BalanceHarness;

public static partial class TowerFixedFamilyConfirmation
{
    internal static void Seal(string root) => HarnessJson.WriteNew(Path.Combine(root, "files.json"),
        TowerBulkCampaign.Paths(root).Where(p => Path.GetFullPath(p) != Path.GetFullPath(Path.Combine(root, "files.json")))
            .ToDictionary(p => Path.GetRelativePath(root, p).Replace('\\', '/'), HarnessJson.FileHash));

    internal static async Task PrepareArchive(TowerFixedFamilyRequest q, TowerFixedFamilyFreeze freeze, Action check, CancellationToken ct)
    {
        var root = P(q, "study"); Require(!Path.Exists(root), "No study retry or resume."); Directory.CreateDirectory(root);
        foreach (var folder in new[] { "recipes", "battles", "exports" }) Directory.CreateDirectory(Path.Combine(root, folder));
        var settings = TowerBundle.ReadSettings(q.ContentRoot);
        var scope = new LoadoutScope(q.Version, settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(q.ContentRoot, Path.Combine(root, "content"), ct), "gzip-json-v1");
        ValidateScope(freeze.Definition, scope);
        Storage(q).Put("study/scope.json", scope); Storage(q).Put("study/freeze.json", freeze); check();
        Storage(q).Put("study/executable-files.json", TowerBossStudy.RetainExecutable(root, scope.Execution,
            AvailableBytes(q)-TowerBulkCampaign.StorageBytes(q.OutputRoot, ct), ct));
        // Preparation uses a local literal label only. It never executes combat or reserves a value.
        var runner = new TowerBattleRunner(Path.Combine(root, "content"), new OfflineContent(Path.Combine(root, "content"), settings.Threat));
        foreach (var team in freeze.Definition.Teams)
        {
            check(); var scenario = team.Scenario with { Seeds = new[] { 0 } };
            await runner.PrepareAsync(runner.CreateInput(scenario, 0, settings.Threat, settings.CheckpointIntervalTicks), ct);
        }
        check();
    }

    internal static void ValidateScope(TowerFixedFamilyDefinition d, LoadoutScope scope)
        => Require(scope.Algorithm == d.Version && scope.ReportStorage == "gzip-json-v1"
            && HarnessJson.Hash(scope.Execution) == d.ExecutionHash && HarnessJson.Hash(scope.Settings) == d.SettingsHash
            && HarnessJson.Hash(scope.ContentHashes) == HarnessJson.Hash(d.ContentHashes), "Changed fixed-family runtime, settings, content or encoding.");

    internal static Task<TowerFixedFamilyStudy> RunStudy(TowerFixedFamilyRequest q, TowerFixedFamilyFreeze freeze, TowerFixedFamilyPanel panel,
        Action<bool> attempt, Action check, CancellationToken ct)
    {
        var scope = TowerContractJson.Read<LoadoutScope>(P(q, "study/scope.json"));
        var archive = new TowerLoadoutArchive(P(q, "study"), scope, Policy(q.Version).Fights);
        return ExecuteArchive(q, freeze, panel, async (arm, stage, scenario, seed, token) => {
            var result = await archive.EvaluateAsync(arm, stage, scenario, seed, token);
            Require(archive.CacheHits == 0, "Unexpected cache reuse."); return result;
        }, attempt, check, ct);
    }

    internal static async Task<TowerFixedFamilyStudy> ExecuteArchive(TowerFixedFamilyRequest q, TowerFixedFamilyFreeze freeze, TowerFixedFamilyPanel panel,
        TowerBossDiscoveryRun.Battle battle, Action<bool> attempt, Action check, CancellationToken ct)
    {
        Event(q, "ConfirmationStarted", HarnessJson.Hash(panel.Panel), 0);
        try
        {
            var study = await Execute(freeze, panel.Panel, battle, attempt, ct); check();
            Storage(q).Put("study/study.json", study);
            for (var i = 0; i < freeze.Definition.Teams.Count; i++) Storage(q).Put("study/exports/"+CellId(freeze.Definition,i)+".json", freeze.Definition.Teams[i].Scenario);
            Event(q, "MeasurementCompleted", HarnessJson.Hash(study), Policy(q.Version).Fights); return study;
        }
        finally { Seal(P(q, "study")); }
    }

    internal static async Task<TowerFixedFamilyStudy> VerifyStudy(TowerFixedFamilyRequest q, CancellationToken ct,
        Func<LoadoutScope, TowerScenario, int, TowerBattleInput>? fixtureInput = null)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Confirmation verification cannot fight.")).Activate();
        var freeze = VerifyFreeze(q); var panel = VerifyPanel(q, freeze); var root = P(q, "study");
        var trials = TowerLoadoutArchive.Verify(root, ct); Require(trials.Count == Policy(q.Version).Fights, "Incomplete archive.");
        var saved = TowerContractJson.Read<TowerFixedFamilyStudy>(Path.Combine(root, "study.json")); VerifyJournals(q, saved, panel);
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
        Require(ordinal == Policy(q.Version).Fights && HarnessJson.Hash(saved) == HarnessJson.Hash(rebuilt), "Recorded study differs from native reconstruction.");
        for (var i = 0; i < freeze.Definition.Teams.Count; i++) Match(q, "study/exports/"+CellId(freeze.Definition,i)+".json", freeze.Definition.Teams[i].Scenario);
        Require(Directory.EnumerateFiles(Path.Combine(root, "exports")).Select(Path.GetFileName).Order()
            .SequenceEqual(freeze.Definition.Teams.Select((t,i) => CellId(freeze.Definition,i)+".json").Order()), "Changed export inventory.");
        Require(Directory.EnumerateFiles(Path.Combine(root, "recipes")).Select(Path.GetFileName).Order()
            .SequenceEqual(Chunks(freeze.Definition, panel.Panel).Select(c => HarnessJson.Hash(c.Scenario)+".json").Order()), "Changed transport-recipe inventory.");
        Require(Directory.EnumerateFiles(Path.Combine(root, "battles")).Select(Path.GetFileName).Order()
            .SequenceEqual(trials.Select(t => t.Id+".json.gz").Order()), "Changed battle inventory.");
        return rebuilt;
    }

    // Separate direct-outcome count implementation: does not call Execute or Assess.
    internal static TowerFixedFamilyResult IndependentAudit(TowerFixedFamilyRequest q, TowerFixedFamilyStudy study, CancellationToken ct)
    {
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Independent confirmation audit cannot fight.")).Activate();
        var policy = Policy(q.Version); var freeze = VerifyFreeze(q); var panel = VerifyPanel(q, freeze); var teams = freeze.Definition.Teams;
        Require(study.Version == q.Version && HarnessJson.Hash(study.Freeze) == HarnessJson.Hash(freeze)
            && study.Evidence.Select(e => e.PartyId).SequenceEqual(teams.Select(t => t.PartyId)), "Changed direct-audit family.");
        var root = P(q, "study"); var trials = TowerLoadoutArchive.Verify(root, ct); Require(trials.Count == Policy(q.Version).Fights, "Incomplete direct audit.");
        var wins = Enumerable.Range(0, TeamCount).Select(_ => new bool[policy.Samples]).ToArray(); var ordinal = 0;
        foreach (var chunk in Chunks(freeze.Definition, panel.Panel))
        {
            var recipeHash = HarnessJson.Hash(chunk.Scenario);
            foreach (var seed in chunk.Scenario.Seeds)
            {
                ct.ThrowIfCancellationRequested(); var trial = trials[ordinal];
                Require(trial.Id == $"trial-{ordinal+1:D6}" && trial.Stage == "confirmation" && trial.Seed == seed && trial.Recipe == recipeHash, "Changed direct-audit trial order.");
                var report = TowerLoadoutArchive.ReadBattle(root, trial.Id, "gzip-json-v1"); ValidateReport(report, chunk.Scenario, seed);
                var cell = study.Evidence[chunk.TeamOrdinal].Trials;
                Require(cell.Count == policy.Samples && cell[ordinal%policy.Samples] == new TowerBalanceTrial(seed, report.Battle.Summary.ContentOutcome), "Summary differs from direct outcomes.");
                wins[chunk.TeamOrdinal][ordinal%policy.Samples] = report.Battle.Summary.ContentOutcome == BattleOutcome.Victory; ordinal++;
            }
        }
        var rates = new List<TowerDiagnosticRate>(); var contrasts = new List<TowerFixedFamilyContrast>();
        for (var i = 0; i < TeamCount; i++)
        {
            var count = wins[i].Count(w => w); rates.Add(new(teams[i].PartyId, count, TowerBalanceEvaluator.Wilson(count, policy.Samples, policy.Family)!));
        }
        var qualifiers = new List<string>();
        for (var candidate = 0; candidate < policy.Candidates; candidate++)
        {
            var qualifies = true;
            for (var reference = policy.Candidates; reference < TeamCount; reference++)
            {
                var gains = 0; var losses = 0;
                for (var i = 0; i < policy.Samples; i++)
                {
                    if (wins[candidate][i] && !wins[reference][i]) gains++;
                    if (!wins[candidate][i] && wins[reference][i]) losses++;
                }
                var g = TowerBalanceEvaluator.Wilson(gains, policy.Samples, policy.Family)!; var l = TowerBalanceEvaluator.Wilson(losses, policy.Samples, policy.Family)!;
                var pass = gains-losses >= policy.MinimumNet && g.Lower > l.Upper && rates[candidate].Estimate.Lower >= .10;
                contrasts.Add(new(teams[candidate].PartyId, teams[reference].PartyId, gains, losses, (gains-losses)/(double)policy.Samples,
                    g.Lower-l.Upper, g.Upper-l.Lower, pass));
                qualifies &= pass;
            }
            if (qualifies) qualifiers.Add(teams[candidate].PartyId);
        }
        var controls = teams.Skip(policy.Candidates).Select(t => t.PartyId).ToArray(); var any = qualifiers.Count != 0;
        return new(q.Version, "Complete", "Verified", any ? "StrongerFixedCandidatesConfirmed" : "StrengthNotDemonstrated", any ? "RecommendFixedCandidates" : "Hold",
            teams.Take(policy.Candidates).Select(t => t.PartyId).ToArray(), qualifiers, rates, contrasts, Recommendations(q.Version, qualifiers, controls), controls, "NotAssessed", Assumption(q.Version),
            HarnessJson.Hash(study), HarnessJson.FileHash(Path.Combine(root, "files.json")), StopReason);
    }
}
