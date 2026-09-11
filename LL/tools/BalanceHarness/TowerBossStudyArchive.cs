using System.Text.Json;

namespace BalanceHarness;

public static partial class TowerBossStudy
{
    public static async Task<BossStudyReport> RunAsync(string root, string output, TowerBossDiscoveryDefinition definition,
        CancellationToken token = default, Action<string>? progress = null)
    {
        // Own the arrays and dictionaries too: callers cannot change a running experiment through a record's children.
        var d = JsonSerializer.Deserialize<TowerBossDiscoveryDefinition>(JsonSerializer.Serialize(definition, HarnessJson.Options), HarnessJson.Options)!;
        var cost = TowerBossDiscovery.Validate(root, d); var inputs = TowerBossDiscovery.GenerationInputs(d);
        if (Path.Exists(output)) throw new IOException("Choose a new Tower study output directory.");
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        foreach (var folder in new[] { "recipes", "battles", "exports", "replays" }) Directory.CreateDirectory(Path.Combine(output, folder));
        BossStudyReport? report = null;
        try
        {
            var settings = TowerBundle.ReadSettings(root); var frozen = Path.Combine(output, "content");
            var scope = new LoadoutScope(Algorithm, settings, ExecutionIdentity.Current(), TowerBundle.CopyContent(root, frozen, token), "gzip-json-v1");
            if (HarnessJson.Hash(scope.ContentHashes) != HarnessJson.Hash(d.ContentHashes)
                || HarnessJson.Hash(settings) != d.SettingsHash || HarnessJson.Hash(scope.Execution) != d.ExecutionHash)
                throw new InvalidDataException("Content, settings or execution changed while freezing the study.");
            void Freeze(string name, object value) => HarnessJson.WriteNew(Path.Combine(output, name), value);
            Freeze("definition.json", d); Freeze("scope.json", scope); Freeze("cost.json", cost);
            Freeze("generation-inputs.json", inputs);
            Freeze("seed-ledger.json", new { d.ExcludedCombatSeeds, Generation = d.Generation.Seeds, d.Stages.Schedules });
            var inventory = TowerBossInventory.Create(frozen, settings.Threat);
            var mechanics = TowerBossPartyGenerator.FromInventory(inputs, inventory);
            Freeze("boss-profiles.json", inventory); Freeze("generation-mechanics.json", mechanics);
            Freeze("executable-files.json", RetainExecutable(output, scope.Execution));
            var archive = new TowerLoadoutArchive(output, scope, cost.Total - cost.ReplayReserve);
            var runner = new TowerBattleRunner(frozen, new OfflineContent(frozen, settings.Threat));
            report = await ExecuteAsync(d, mechanics, async (arm, stage, scenario, seed, ct) => {
                var result = await archive.EvaluateAsync(arm, stage, scenario, seed, ct);
                if (archive.CacheHits != 0) throw new InvalidDataException("Unexpected study cache reuse; combat counts must remain exact.");
                return result;
            }, async (trial, scenario, ct) => {
                var input = runner.CreateInput(scenario, trial.Seed, settings.Threat, settings.CheckpointIntervalTicks);
                if (trial.InputHash != HarnessJson.Hash(input)) throw new InvalidDataException("Replay preparation changed.");
                var replay = await runner.RunAsync(input, detailed: true, token: ct);
                Freeze("replays/" + trial.Id + ".json", replay);
                return replay;
            }, Freeze, token, progress);
            Freeze("study.json", report);
            if (report.Balance is not null) Freeze("assessment.json", report.Balance);
            File.WriteAllText(Path.Combine(output, "study.md"), Markdown(report));
            return report;
        }
        catch (Exception exception)
        {
            // Preparation failures precede the state machine and never produce an acceptance report.
            if (report is null) HarnessJson.WriteNew(Path.Combine(output, "preparation-error.json"), new {
                Status = token.IsCancellationRequested ? "Cancelled" : "Invalid", Error = exception.GetType().Name + ": " + exception.Message });
            throw;
        }
        finally
        {
            HarnessJson.WriteNew(Path.Combine(output, "files.json"), Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal).ToDictionary(p => Path.GetRelativePath(output, p).Replace('\\', '/'), HarnessJson.FileHash));
        }
    }

    public static async Task<BossStudyReport> VerifyAsync(string output, CancellationToken token = default)
    {
        var trials = TowerLoadoutArchive.Verify(output, token);
        var saved = HarnessJson.Read<BossStudyReport>(Path.Combine(output, "study.json"));
        if (saved.Status is not ("Complete" or "Incomplete"))
            throw new InvalidDataException("Interrupted or invalid studies retain partial evidence; they cannot pass full study reconstruction.");
        var d = TowerBossDiscovery.Read(Path.Combine(output, "definition.json"));
        var cost = TowerBossDiscovery.Validate(d); var inputs = TowerBossDiscovery.GenerationInputs(d);
        var scope = HarnessJson.Read<LoadoutScope>(Path.Combine(output, "scope.json"));
        if (scope.Algorithm != Algorithm || HarnessJson.Hash(scope.Execution) != d.ExecutionHash
            || d.ExecutionHash != HarnessJson.Hash(ExecutionIdentity.Current()) || HarnessJson.Hash(scope.Settings) != d.SettingsHash
            || HarnessJson.Hash(scope.ContentHashes) != HarnessJson.Hash(d.ContentHashes))
            throw new InvalidDataException("Study scope changed; use the retained producing executable and platform.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Match(string name, object value)
        {
            seen.Add(name);
            if (HarnessJson.Hash(HarnessJson.Read<JsonElement>(Path.Combine(output, name))) != HarnessJson.Hash(value))
                throw new InvalidDataException("Study artifact differs from deterministic reconstruction: " + name);
        }
        var root = Path.Combine(output, "content");
        if (d.ContentHashes.Any(p => HarnessJson.FileHash(Path.Combine(root, "Data", p.Key)) != p.Value))
            throw new InvalidDataException("Frozen study content changed.");
        Match("cost.json", cost); Match("generation-inputs.json", inputs);
        Match("seed-ledger.json", new { d.ExcludedCombatSeeds, Generation = d.Generation.Seeds, d.Stages.Schedules });
        var executable = HarnessJson.Read<Dictionary<string, string>>(Path.Combine(output, "executable-files.json"));
        var actualExecutable = Directory.EnumerateFiles(Path.Combine(output, "executable"), "*", SearchOption.AllDirectories)
            .ToDictionary(p => Path.GetRelativePath(Path.Combine(output, "executable"), p).Replace('\\', '/'), HarnessJson.FileHash);
        if (HarnessJson.Hash(executable) != HarnessJson.Hash(actualExecutable)
            || scope.Execution.AssemblyHashes.Any(p => !executable.TryGetValue(p.Key + ".dll", out var hash) || hash != p.Value))
            throw new InvalidDataException("Retained producing executable changed.");
        var inventory = TowerBossInventory.Create(root, scope.Settings.Threat);
        var mechanics = TowerBossPartyGenerator.FromInventory(inputs, inventory);
        Match("boss-profiles.json", inventory); Match("generation-mechanics.json", mechanics);
        var runner = new TowerBattleRunner(root, new OfflineContent(root, scope.Settings.Threat));
        var index = 0; var replayIds = new List<string>();
        TowerScenario? lastScenario = null; TowerBattleInput? template = null; string? recipeHash = null;
        var rebuilt = await ExecuteAsync(d, mechanics, (arm, stage, scenario, seed, ct) => {
            ct.ThrowIfCancellationRequested();
            if (index >= trials.Count) throw new InvalidDataException("Missing study trial.");
            if (!ReferenceEquals(lastScenario, scenario))
            {
                template = runner.CreateInput(scenario, seed, scope.Settings.Threat, scope.Settings.CheckpointIntervalTicks);
                recipeHash = HarnessJson.Hash(scenario); lastScenario = scenario;
                Match("recipes/" + recipeHash + ".json", scenario);
            }
            var input = template! with { Rules = template!.Rules with { RandomSeed = seed } };
            var trial = trials[index++];
            if (trial.Stage != stage || trial.Seed != seed || trial.Recipe != recipeHash || trial.InputHash != HarnessJson.Hash(input)
                || trial.CacheKey != TowerLoadoutArchive.Key(scope, arm, input))
                throw new InvalidDataException("Study trial differs in recipe, identity, stage, arm or seed.");
            return Task.FromResult((trial, TowerLoadoutArchive.ReadBattle(output, trial.Id, scope.ReportStorage)));
        }, (trial, scenario, ct) => {
            ct.ThrowIfCancellationRequested(); replayIds.Add(trial.Id);
            return Task.FromResult(HarnessJson.Read<TowerBattleReport>(Path.Combine(output, "replays", trial.Id + ".json")));
        }, Match, token);
        token.ThrowIfCancellationRequested();
        if (index != trials.Count || HarnessJson.Hash(saved) != HarnessJson.Hash(rebuilt))
            throw new InvalidDataException("Study stages, family, evidence, acceptance, replay or accounting differs from reconstruction.");
        if (rebuilt.Balance is not null) Match("assessment.json", rebuilt.Balance);
        foreach (var folder in new[] { "exports", "recipes" })
            if (!Directory.EnumerateFiles(Path.Combine(output, folder)).Select(p => Path.GetRelativePath(output, p).Replace('\\', '/')).Order(StringComparer.Ordinal)
                .SequenceEqual(seen.Where(p => p.StartsWith(folder + "/", StringComparison.Ordinal)).Order(StringComparer.Ordinal)))
                throw new InvalidDataException("Unexpected study recipes or exports.");
        if (!Directory.EnumerateFiles(Path.Combine(output, "replays")).Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal)
            .SequenceEqual(replayIds.Order(StringComparer.Ordinal)) || File.ReadAllText(Path.Combine(output, "study.md")) != Markdown(rebuilt))
            throw new InvalidDataException("Study replay inventory or Markdown changed.");
        return rebuilt;
    }

    private static IReadOnlyDictionary<string, string> RetainExecutable(string output, ExecutionIdentity execution)
    {
        var source = Path.GetDirectoryName(typeof(TowerBossStudy).Assembly.Location)!;
        var destination = Path.Combine(output, "executable"); Directory.CreateDirectory(destination);
        var copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Copy(string relative)
        {
            if (!copied.Add(relative)) return;
            var target = Path.Combine(destination, relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(source, relative), target, overwrite: false);
        }
        foreach (var name in new[] { "BalanceHarness.dll", "BalanceHarness.deps.json", "BalanceHarness.runtimeconfig.json" }) Copy(name);
        var deps = HarnessJson.Read<JsonElement>(Path.Combine(source, "BalanceHarness.deps.json"));
        var targetName = deps.GetProperty("runtimeTarget").GetProperty("name").GetString()!;
        foreach (var library in deps.GetProperty("targets").GetProperty(targetName).EnumerateObject())
        {
            if (library.Value.TryGetProperty("runtime", out var runtime))
                foreach (var asset in runtime.EnumerateObject()) if (!asset.Name.EndsWith("/_._", StringComparison.Ordinal)) Copy(Path.GetFileName(asset.Name));
            if (library.Value.TryGetProperty("resources", out var resources))
                foreach (var asset in resources.EnumerateObject())
                {
                    var relative = Path.Combine(asset.Value.GetProperty("locale").GetString()!, Path.GetFileName(asset.Name));
                    if (File.Exists(Path.Combine(source, relative))) Copy(relative);
                }
        }
        var runtimes = Path.Combine(source, "runtimes");
        if (Directory.Exists(runtimes)) foreach (var file in Directory.EnumerateFiles(runtimes, "*", SearchOption.AllDirectories)) Copy(Path.GetRelativePath(source, file));
        var hashes = Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories)
            .ToDictionary(p => Path.GetRelativePath(destination, p).Replace('\\', '/'), HarnessJson.FileHash);
        if (execution.AssemblyHashes.Any(p => !hashes.TryGetValue(p.Key + ".dll", out var hash) || hash != p.Value))
            throw new InvalidDataException("Producing assemblies changed while copying the executable.");
        return hashes;
    }
}
