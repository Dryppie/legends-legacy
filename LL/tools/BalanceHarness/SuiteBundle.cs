using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BalanceHarness;

public static class SuiteBundle
{
    public static async Task<SuiteReport> CreateAsync(string apiContentRoot, string suitePath,
        string outputDirectory, int masterSeed, int? samplesPerCell, CancellationToken cancellationToken,
        Action<int, int>? progress = null)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Path.Exists(output)) throw new IOException($"Output already exists: {output}");
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(output);
        var timer = Stopwatch.StartNew();
        try
        {
            var snapshotRoot = Path.Combine(output, "content");
            var hashes = RunBundle.CopyContent(apiContentRoot, snapshotRoot, cancellationToken);
            var (threat, cadence) = RunBundle.ReadCombatSettings(apiContentRoot);
            var content = new OfflineContent(snapshotRoot, threat);
            var definition = HarnessJson.Read<IdleSuiteDefinition>(suitePath);
            if (samplesPerCell.HasValue) definition = definition with { SamplesPerCell = samplesPerCell.Value };
            var input = IdleSuite.Resolve(definition, content, threat, cadence, masterSeed);
            HarnessJson.WriteNew(Path.Combine(output, "suite-input.json"), input);
            HarnessJson.WriteNew(Path.Combine(output, "manifest.json"),
                new RunManifest(1, HarnessJson.Hash(input), hashes, ExecutionIdentity.Current()));
            Directory.CreateDirectory(Path.Combine(output, "battles"));
            var observations = new List<BattleObservation>();
            var cancelled = false;
            var total = input.Cells.Sum(x => x.Trials.Count);
            var compactJson = new JsonSerializerOptions(HarnessJson.Options) { WriteIndented = false };
            using var index = new StreamWriter(new FileStream(Path.Combine(output, "battles.jsonl"), FileMode.CreateNew, FileAccess.Write));
            var runner = new IdleBattleRunner(content);
            progress?.Invoke(0, total);
            foreach (var cell in input.Cells)
            {
                foreach (var trial in cell.Trials)
                {
                    if (cancellationToken.IsCancellationRequested) { cancelled = true; break; }
                    BattleObservation observation;
                    try
                    {
                        var result = await runner.RunAsync(IdleSuite.BattleInput(cell, trial), cancellationToken: cancellationToken);
                        HarnessJson.WriteNew(Path.Combine(output, "battles", trial.BattleId + ".json"), result);
                        observation = BattleObservation.FromResult(cell, trial, result);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        cancelled = true;
                        observation = new(trial.BattleId, cell.Id, trial.Index, trial.Seed, "Cancelled");
                    }
                    catch (Exception exception) when (exception is not IOException and not UnauthorizedAccessException)
                    {
                        observation = new(trial.BattleId, cell.Id, trial.Index, trial.Seed, "Invalid",
                            Error: $"{exception.GetType().Name}: {exception.Message}");
                    }
                    observations.Add(observation);
                    index.WriteLine(JsonSerializer.Serialize(observation, compactJson));
                    index.Flush();
                    if (cancelled) break;
                }
                progress?.Invoke(observations.Count, total);
                if (cancelled) break;
            }
            var report = SuiteScorecard.Create(input, observations, cancelled, timer.Elapsed.TotalSeconds);
            HarnessJson.WriteNew(Path.Combine(output, "scorecard.json"), report);
            await File.WriteAllTextAsync(Path.Combine(output, "scorecard.md"), SuiteScorecard.Markdown(input, report, observations));
            return report;
        }
        catch (Exception exception)
        {
            HarnessJson.WriteNew(Path.Combine(output, "failure.json"), new
            {
                Status = exception is OperationCanceledException ? "Cancelled" : "Invalid",
                ErrorType = exception.GetType().Name, exception.Message
            });
            throw;
        }
    }

    public static async Task<BattleReport> ReplayAsync(string runDirectory, string battleId, bool detailed,
        CancellationToken cancellationToken)
    {
        if (!Regex.IsMatch(battleId, "^[a-z0-9.-]+$", RegexOptions.CultureInvariant) || battleId.Length > 210)
            throw new InvalidDataException("Invalid battle identifier.");
        var input = HarnessJson.Read<SuiteRunInput>(Path.Combine(runDirectory, "suite-input.json"));
        var manifest = HarnessJson.Read<RunManifest>(Path.Combine(runDirectory, "manifest.json"));
        RunBundle.Verify(runDirectory, manifest, HarnessJson.Hash(input), cancellationToken);
        if (input.SchemaVersion != 1 || input.SeedScheduleVersion != IdleSuite.SeedScheduleVersion)
            throw new InvalidDataException("Unsupported suite input or seed schedule.");
        var matches = input.Cells.SelectMany(c => c.Trials.Where(t => t.BattleId == battleId).Select(t => (Cell: c, Trial: t))).ToArray();
        if (matches.Length != 1) throw new InvalidDataException($"Unknown or duplicate battle ID '{battleId}'.");
        var (cell, trial) = matches[0];
        var battleInput = IdleSuite.BattleInput(cell, trial);
        var content = new OfflineContent(Path.Combine(runDirectory, "content"), battleInput.ThreatAndTanking);
        var report = await new IdleBattleRunner(content).RunAsync(battleInput, detailed, cancellationToken);
        var originalPath = Path.Combine(runDirectory, "battles", battleId + ".json");
        if (File.Exists(originalPath)) RunBundle.VerifyResult(HarnessJson.Read<BattleReport>(originalPath), report);
        return report;
    }
}
