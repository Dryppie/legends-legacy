using System.Globalization;
using System.Text.Json;

namespace BalanceHarness;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += cancelHandler;
        try
        {
            if (args.Length == 0 || args[0] is "--help" or "-h")
            {
                Console.WriteLine("BalanceHarness run --output <new-directory> [--seed <int>] [--content-root <API.LL-directory>] [--scenario <json>] [--detailed]");
                Console.WriteLine("BalanceHarness suite --output <new-directory> [--seed <int>] [--suite <json>] [--samples <per-cell>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness tower --output <new-directory> [--scenario <json>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness tower-benchmark --output <new-directory> [--catalog <json>] [--seed <int>] [--samples <per-cell>] [--reference <benchmark-directory>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness tower-compare --reference <benchmark-directory> --run <benchmark-directory> --output <new-directory>");
                Console.WriteLine("BalanceHarness tower-search --output <new-directory> [--content-root <API.LL-directory>] [--catalogs-root <directory>]");
                Console.WriteLine("BalanceHarness tower-dashboard [--port <1-65535>] [--runs-root <directory>] [--catalogs-root <directory>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness replay --run <directory> [--battle <suite-battle-id>] [--detailed]");
                Console.WriteLine("BalanceHarness baseline accept --run <suite-directory> --output <new-manifest.json> --reason <text>");
                Console.WriteLine("BalanceHarness compare --baseline <manifest.json> --run <suite-directory> --output <new-directory>");
                Console.WriteLine("BalanceHarness evaluate --run <suite-directory> --output <new-directory> [--goals <json>] [--baseline <manifest.json>]");
                Console.WriteLine("BalanceHarness investigate-handoff --output <new-directory> [--samples <1-500>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness investigate-creek-pressure --output <new-directory> [--samples <1-100>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness investigate-creek-pressure-fine --output <new-directory> [--samples <1-100>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness investigate-entry --output <new-directory> [--samples <1-100>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness investigate-pressure --output <new-directory> [--samples <1-100>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness investigate-pressure-fine --output <new-directory> [--samples <1-100>] [--content-root <API.LL-directory>]");
                Console.WriteLine("BalanceHarness validate-blood-grove --output <new-directory> [--samples <1-100>] [--content-root <API.LL-directory>]");
                return 0;
            }
            var command = args[0];
            var optionStart = 1;
            if (command == "baseline")
            {
                if (args.Length < 2 || args[1] != "accept") throw new ArgumentException("Expected: baseline accept.");
                optionStart = 2;
            }
            var allowed = command switch
            {
                "run" => new[] { "--output", "--seed", "--content-root", "--scenario", "--detailed" },
                "suite" => new[] { "--output", "--seed", "--content-root", "--suite", "--samples" },
                "tower" => new[] { "--output", "--content-root", "--scenario" },
                "tower-benchmark" => new[] { "--output", "--content-root", "--catalog", "--seed", "--samples", "--reference" },
                "tower-compare" => new[] { "--reference", "--run", "--output" },
                "tower-search" => new[] { "--output", "--content-root", "--catalogs-root" },
                "tower-dashboard" => new[] { "--port", "--runs-root", "--catalogs-root", "--content-root" },
                "replay" => new[] { "--run", "--battle", "--detailed" },
                "baseline" => new[] { "--run", "--output", "--reason" },
                "compare" => new[] { "--baseline", "--run", "--output" },
                "evaluate" => new[] { "--run", "--output", "--goals", "--baseline" },
                "investigate-entry" => new[] { "--output", "--samples", "--content-root" },
                "investigate-pressure" or "investigate-pressure-fine" => new[] { "--output", "--samples", "--content-root" },
                "validate-blood-grove" => new[] { "--output", "--samples", "--content-root" },
                "investigate-handoff" => new[] { "--output", "--samples", "--content-root" },
                "investigate-creek-pressure" or "investigate-creek-pressure-fine" => new[] { "--output", "--samples", "--content-root" },
                _ => throw new ArgumentException($"Unknown command '{command}'.")
            };
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = optionStart; index < args.Length; index++)
            {
                var key = args[index];
                if (!allowed.Contains(key) || options.ContainsKey(key))
                    throw new ArgumentException($"Unknown or duplicate option '{key}'.");
                if (key == "--detailed") options.Add(key, "true");
                else if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Missing value for '{key}'.");
                else options.Add(key, args[index]);
            }
            var detailed = options.ContainsKey("--detailed");
            if (command == "tower-search")
            {
                var catalogs = options.GetValueOrDefault("--catalogs-root") ?? Path.Combine(AppContext.BaseDirectory, "Fixtures");
                var report = await TowerEssenceSearch.RunAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    catalogs, Required(options, "--output"), HarnessJson.Read<TowerEssenceSearchDefinition>(Path.Combine(catalogs, TowerEssenceSearch.ConfigFile)),
                    cancellation.Token, Console.WriteLine);
                Console.WriteLine($"{report.Status}; {report.Selection.Parties.Count} candidates independently confirmed. No optimal-build or balance acceptance claim.");
                return 0;
            }
            if (command == "tower-dashboard")
            {
                var root = options.GetValueOrDefault("--content-root") ?? FindContentRoot();
                await TowerDashboardServer.RunAsync(root, options.GetValueOrDefault("--catalogs-root") ?? Path.Combine(AppContext.BaseDirectory, "Fixtures"),
                    options.GetValueOrDefault("--runs-root") ?? Path.GetFullPath(Path.Combine(root, "../../../../TestResults/balance")),
                    int.Parse(options.GetValueOrDefault("--port") ?? "5087", CultureInfo.InvariantCulture), cancellation.Token);
                return 0;
            }
            if (command == "tower-benchmark")
            {
                var result = await TowerBenchmark.RunAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    options.GetValueOrDefault("--catalog") ?? Path.Combine(AppContext.BaseDirectory, "Fixtures", "tower-benchmark.json"),
                    Required(options, "--output"), int.Parse(options.GetValueOrDefault("--seed") ?? "1337", CultureInfo.InvariantCulture),
                    options.TryGetValue("--samples", out var sampleCount) ? int.Parse(sampleCount, CultureInfo.InvariantCulture) : null,
                    options.GetValueOrDefault("--reference"), cancellation.Token, Console.WriteLine);
                Console.WriteLine($"{result.Status}; {result.ValidBattles}/{result.PlannedBattles} battles. Descriptive only. Report: {Path.GetFullPath(Path.Combine(options["--output"], "benchmark.md"))}");
                return result.Status == "Complete" ? 0 : 2;
            }
            if (command == "tower-compare")
            {
                var result = TowerBenchmarkComparison.Create(Required(options, "--reference"), Required(options, "--run"),
                    Required(options, "--output"), cancellation.Token);
                Console.WriteLine($"{result.Status}; {result.Cells.Sum(c => c.GameplayChanges)} changed gameplay records. Descriptive only.");
                return result.Status == "Compared" ? 0 : 2;
            }
            if (command == "tower")
            {
                var score = await TowerBundle.CreateAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    options.GetValueOrDefault("--scenario") ?? Path.Combine(AppContext.BaseDirectory, "Fixtures", "tower-floor-1.json"),
                    Required(options, "--output"), cancellation.Token, Console.WriteLine);
                Console.WriteLine($"{score.Status}; descriptive only. Wins {score.Wins}, defeats {score.Defeats}, draws {score.Draws}.");
                return score.Status == "Complete" ? 0 : 2;
            }
            if (command is "investigate-creek-pressure" or "investigate-creek-pressure-fine")
            {
                var result = await CrystalCreekPressureExperiment.RunAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    Path.Combine(AppContext.BaseDirectory, "Fixtures"), Required(options, "--output"),
                    int.Parse(options.GetValueOrDefault("--samples") ?? "100", CultureInfo.InvariantCulture), cancellation.Token, Console.WriteLine,
                    command == "investigate-creek-pressure-fine" ? CreekPressureSweep.Fine : CreekPressureSweep.Coarse);
                Console.WriteLine($"{result.Status}; {result.ValidBattles} valid battles; confirmation policy: {result.Confirmation.GateStatus}. No production promotion.");
                return 0;
            }
            if (command == "investigate-handoff")
            {
                var result = await CrystalCreekHandoff.RunAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    Path.Combine(AppContext.BaseDirectory, "Fixtures", CrystalCreekHandoff.Fixture), Required(options, "--output"),
                    int.Parse(options.GetValueOrDefault("--samples") ?? "500", CultureInfo.InvariantCulture), cancellation.Token, Console.WriteLine);
                Console.WriteLine($"{result.Status}; advisory only; {result.ValidBattles} valid battles. No baseline promotion.");
                return 0;
            }
            if (command == "validate-blood-grove")
            {
                var result = await BloodGroveLocalValidation.RunAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    Path.Combine(AppContext.BaseDirectory, "Fixtures"), Required(options, "--output"),
                    int.Parse(options.GetValueOrDefault("--samples") ?? "100", CultureInfo.InvariantCulture), cancellation.Token, Console.WriteLine);
                Console.WriteLine($"{result.Status}; {result.ValidBattles} valid battles; policy: {result.Evaluation.GateStatus}. No automatic baseline promotion.");
                return 0;
            }
            if (command is "investigate-pressure" or "investigate-pressure-fine")
            {
                var result = await BloodGrovePressureExperiment.RunAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    Path.Combine(AppContext.BaseDirectory, "Fixtures"), Required(options, "--output"),
                    int.Parse(options.GetValueOrDefault("--samples") ?? "100", CultureInfo.InvariantCulture), cancellation.Token, Console.WriteLine,
                    command == "investigate-pressure-fine" ? PressureSweep.Fine : PressureSweep.Coarse);
                Console.WriteLine($"{result.Status}; {result.ValidBattles} valid battles. Confirmation: {result.Confirmation.GateStatus}. {result.Disposition}");
                return 0;
            }
            if (command == "investigate-entry")
            {
                var result = await BloodGroveEntryExperiment.RunAsync(options.GetValueOrDefault("--content-root") ?? FindContentRoot(),
                    Path.Combine(AppContext.BaseDirectory, "Fixtures", "idle-first-hunt.json"), Required(options, "--output"),
                    int.Parse(options.GetValueOrDefault("--samples") ?? "100", CultureInfo.InvariantCulture), cancellation.Token, Console.WriteLine);
                Console.WriteLine($"{result.Status}; advisory only. {result.ValidBattles} valid battles. Summary: {Path.GetFullPath(Path.Combine(options["--output"], "summary.md"))}");
                return 0;
            }
            if (command == "evaluate")
            {
                var goals = options.GetValueOrDefault("--goals") ?? Path.Combine(AppContext.BaseDirectory, "Fixtures", "idle-goals.json");
                var evaluation = GoalEvaluationBundle.Create(goals, Required(options, "--run"),
                    options.GetValueOrDefault("--baseline"), Required(options, "--output"), cancellation.Token);
                Console.WriteLine($"Assessment: {evaluation.Assessment}; enforcement: {evaluation.GateStatus}; exit code: {evaluation.ExitCode}.");
                Console.WriteLine($"Evaluation: {Path.GetFullPath(Path.Combine(options["--output"], "evaluation.md"))}");
                return evaluation.ExitCode;
            }
            if (command == "baseline")
            {
                BaselineManifest.Accept(Required(options, "--run"), Required(options, "--output"),
                    Required(options, "--reason"), cancellation.Token);
                Console.WriteLine($"Baseline recorded: {Path.GetFullPath(options["--output"])}. Advisory reference; no balance targets accepted.");
                return 0;
            }
            if (command == "compare")
            {
                var comparison = SuiteComparison.Create(Required(options, "--baseline"), Required(options, "--run"),
                    Required(options, "--output"), cancellation.Token);
                Console.WriteLine($"{comparison.Status}; advisory only. Compared {comparison.Cells.Count(c => c.Status == "Compared")}/{comparison.Cells.Count} cells; {comparison.Cells.Sum(c => c.GameplayChanges)} changed gameplay records.");
                Console.WriteLine($"Comparison: {Path.GetFullPath(Path.Combine(options["--output"], "comparison.md"))}");
                return comparison.Status == "Complete" ? 0 : 2;
            }
            if (command == "replay")
            {
                var run = Required(options, "--run");
                if (File.Exists(Path.Combine(run, "benchmark-input.json")))
                {
                    var benchmarkReplay = await TowerBenchmark.ReplayAsync(run, Required(options, "--battle"), detailed, cancellation.Token);
                    Console.WriteLine(JsonSerializer.Serialize(benchmarkReplay, HarnessJson.Options));
                    Console.Error.WriteLine("Tower benchmark replay matched the saved trial.");
                    return 0;
                }
                if (File.Exists(Path.Combine(run, "tower-input.json")))
                {
                    var towerReplay = await TowerBundle.ReplayAsync(run, Required(options, "--battle"), detailed, cancellation.Token);
                    Console.WriteLine(JsonSerializer.Serialize(towerReplay, HarnessJson.Options));
                    Console.Error.WriteLine("Tower replay completed; saved preparation, combat and outcome matched.");
                    return 0;
                }
                if (File.Exists(Path.Combine(run, "suite-input.json")) && !options.ContainsKey("--battle"))
                    throw new ArgumentException("Suite replay requires --battle; IDs are listed in scorecard.md and battles.jsonl.");
                var replay = options.TryGetValue("--battle", out var battleId)
                    ? await SuiteBundle.ReplayAsync(run, battleId, detailed, cancellation.Token)
                    : await RunBundle.ReplayAsync(run, detailed, cancellation.Token);
                Console.WriteLine(JsonSerializer.Serialize(replay, HarnessJson.Options));
                Console.Error.WriteLine("Replay completed; any saved result matched.");
            }
            else
            {
                var output = Required(options, "--output");
                var root = options.GetValueOrDefault("--content-root") ?? FindContentRoot();
                var seed = int.Parse(options.GetValueOrDefault("--seed") ?? "1337", CultureInfo.InvariantCulture);
                if (command == "suite")
                {
                    var suite = options.GetValueOrDefault("--suite")
                        ?? Path.Combine(AppContext.BaseDirectory, "Fixtures", "idle-reference.json");
                    int? samples = options.TryGetValue("--samples", out var sampleText)
                        ? int.Parse(sampleText, CultureInfo.InvariantCulture) : null;
                    var scorecard = await SuiteBundle.CreateAsync(root, suite, output, seed, samples, cancellation.Token,
                        (done, total) => Console.WriteLine($"Idle suite: {done}/{total} battles processed."));
                    Console.WriteLine($"{scorecard.Status}; advisory only. Valid {scorecard.Valid}, invalid {scorecard.Invalid}, cancelled {scorecard.Cancelled}, not run {scorecard.NotRun}.");
                    Console.WriteLine($"Scorecard: {Path.GetFullPath(Path.Combine(output, "scorecard.md"))}");
                    return scorecard.Status switch { "Complete" => 0, "Cancelled" => 130, _ => 2 };
                }
                var scenario = options.GetValueOrDefault("--scenario")
                    ?? Path.Combine(AppContext.BaseDirectory, "Fixtures", "idle-starter.json");
                var report = await RunBundle.CreateAsync(root, scenario, output, seed, detailed, cancellation.Token);
                Console.WriteLine($"{report.ScenarioId}: {report.Summary.ContentOutcome}; {report.Summary.DurationTicks} ticks ({report.Summary.DurationSeconds.ToString(CultureInfo.InvariantCulture)} s); seed {seed}");
                Console.WriteLine($"Result: {Path.GetFullPath(Path.Combine(output, "result.json"))}");
            }
            return 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Run cancelled."); return 130; }
        catch (Exception exception) { Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}"); return 2; }
        finally { Console.CancelKeyPress -= cancelHandler; }
    }

    private static string Required(IReadOnlyDictionary<string, string> options, string key) =>
        options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value : throw new ArgumentException($"Required option: {key}");

    private static string FindContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "API", "API.LL");
            if (File.Exists(Path.Combine(candidate, "appsettings.json"))) return candidate;
        }
        throw new DirectoryNotFoundException("Supply --content-root pointing to LL/src/API/API.LL.");
    }
}
