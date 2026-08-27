using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LegendsLegacy.Balance;

public sealed class BalanceReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public BalanceReportPaths Write(BalanceRunReport report, string outputRoot)
    {
        ValidateRunId(report.Metadata.RunId);
        var root = Path.GetFullPath(outputRoot);
        var latestDirectory = Path.Combine(root, "latest");
        var historyDirectory = Path.Combine(root, "history", report.Metadata.RunId);
        if (Directory.Exists(historyDirectory))
            throw new InvalidOperationException($"Balance history run '{report.Metadata.RunId}' already exists.");

        Directory.CreateDirectory(latestDirectory);
        Directory.CreateDirectory(historyDirectory);

        var json = JsonSerializer.Serialize(report, JsonOptions);
        var gearPackagesJson = JsonSerializer.Serialize(report.GearPackages, JsonOptions);
        var essenceBuildsJson = JsonSerializer.Serialize(report.EssenceBuilds, JsonOptions);
        var benchmarksJson = JsonSerializer.Serialize(report.Benchmarks, JsonOptions);
        var markdown = RenderMarkdown(report);
        var latestJsonPath = Path.Combine(latestDirectory, "summary.json");
        var latestMarkdownPath = Path.Combine(latestDirectory, "summary.md");
        var latestGearPackagesJsonPath = Path.Combine(latestDirectory, "gear-packages.json");
        var latestEssenceBuildsJsonPath = Path.Combine(latestDirectory, "essence-builds.json");
        var latestBenchmarksJsonPath = Path.Combine(latestDirectory, "benchmarks.json");
        var historyJsonPath = Path.Combine(historyDirectory, "summary.json");
        var historyMarkdownPath = Path.Combine(historyDirectory, "summary.md");
        var historyGearPackagesJsonPath = Path.Combine(historyDirectory, "gear-packages.json");
        var historyEssenceBuildsJsonPath = Path.Combine(historyDirectory, "essence-builds.json");
        var historyBenchmarksJsonPath = Path.Combine(historyDirectory, "benchmarks.json");

        WriteUtf8(historyJsonPath, json);
        WriteUtf8(historyMarkdownPath, markdown);
        WriteUtf8(historyGearPackagesJsonPath, gearPackagesJson);
        WriteUtf8(historyEssenceBuildsJsonPath, essenceBuildsJson);
        WriteUtf8(historyBenchmarksJsonPath, benchmarksJson);
        WriteUtf8(latestJsonPath, json);
        WriteUtf8(latestMarkdownPath, markdown);
        WriteUtf8(latestGearPackagesJsonPath, gearPackagesJson);
        WriteUtf8(latestEssenceBuildsJsonPath, essenceBuildsJson);
        WriteUtf8(latestBenchmarksJsonPath, benchmarksJson);

        return new BalanceReportPaths(
            latestJsonPath,
            latestMarkdownPath,
            latestGearPackagesJsonPath,
            latestEssenceBuildsJsonPath,
            latestBenchmarksJsonPath,
            historyJsonPath,
            historyMarkdownPath,
            historyGearPackagesJsonPath,
            historyEssenceBuildsJsonPath,
            historyBenchmarksJsonPath);
    }

    public static string RenderMarkdown(BalanceRunReport report)
    {
        var metadata = report.Metadata;
        var content = report.Content;
        var simulation = report.Simulation;
        var gearPackageRows = string.Join(
            Environment.NewLine,
            report.GearPackages.Select(package =>
                $"| {EscapeCell(package.Definition.ProgressionAnchor)} " +
                $"| `{package.Definition.Id}` " +
                $"| {package.Definition.Tier} " +
                $"| {package.Definition.Rarity} " +
                $"| {package.Definition.Quality} " +
                $"| {package.Definition.Archetype} " +
                $"| {package.Equipment.Count} " +
                $"| {package.CombatRating.DisplayOverall} " +
                $"| {package.CombatRating.RawOverall} |"));
        var essenceProfileRows = string.Join(
            Environment.NewLine,
            report.EssenceBuilds
                .GroupBy(build => build.ProfileId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var builds = group.ToArray();
                    var minimumCr = builds.Min(build => build.Character.CombatRating.DisplayOverall);
                    var maximumCr = builds.Max(build => build.Character.CombatRating.DisplayOverall);
                    return $"| `{group.Key}` " +
                           $"| {builds[0].SlotCount} " +
                           $"| {builds.Length} " +
                           $"| `{builds[0].Character.GearPackageId}` " +
                           $"| {minimumCr}-{maximumCr} |";
                }));
        var benchmarkProfileRows = string.Join(
            Environment.NewLine,
            report.Benchmarks.Builds
                .GroupBy(build => build.ProfileId, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var builds = group.ToArray();
                    var leader = builds.OrderBy(build => build.ProfileRank).First();
                    return $"| `{group.Key}` " +
                           $"| {builds.Length} " +
                           $"| {FormatScore(builds.Min(build => build.AggregateScore))}-{FormatScore(builds.Max(build => build.AggregateScore))} " +
                           $"| `{leader.BuildId}` " +
                           $"| {FormatScore(leader.AggregateScore)} |";
                }));
        var benchmarkLeaderRows = string.Join(
            Environment.NewLine,
            report.Benchmarks.Builds
                .Where(build => build.ProfileRank == 1)
                .OrderBy(build => build.ProfileId, StringComparer.Ordinal)
                .Select(build =>
                    $"| `{build.BuildId}` | {FormatScore(build.AggregateScore)} | " +
                    string.Join(" | ", build.Components.Select(component => FormatScore(component.Score))) + " |"));
        var benchmarkComponentHeaders = string.Join(
            " | ",
            report.Benchmarks.Scenarios.Select(scenario => EscapeCell(scenario.DisplayName)));
        var benchmarkComponentDividers = string.Join(
            " | ",
            report.Benchmarks.Scenarios.Select(_ => "---:"));
        var gitCommit = string.IsNullOrWhiteSpace(metadata.GitCommitHash)
            ? "Unavailable"
            : metadata.GitCommitHash;

        return $$"""
            # LegendsLegacy Balance Report

            ## Run

            | Field | Value |
            | --- | --- |
            | Run ID | `{{metadata.RunId}}` |
            | Created (UTC) | `{{metadata.CreatedAtUtc:O}}` |
            | Seed | `{{metadata.Seed}}` |
            | Balance schema | `{{metadata.BalanceSchemaVersion}}` |
            | Simulator algorithm | `{{metadata.SimulatorAlgorithmVersion}}` |
            | Combat engine | `{{metadata.CombatEngineVersion}}` |
            | Git commit | `{{gitCommit}}` |

            ## Production Content Loaded

            | Abilities | Statuses | Summons | Essences |
            | ---: | ---: | ---: | ---: |
            | {{content.AbilityCount}} | {{content.StatusCount}} | {{content.SummonCount}} | {{content.EssenceCount}} |

            ## Random Legal Essence Builds

            | Profile | Slots | Builds | Reference Gear Package | CR Range |
            | --- | ---: | ---: | --- | ---: |
            {{essenceProfileRows}}

            Combat Rating currently excludes Essence ability performance. A zero-width CR range within a profile is therefore expected; PvE benchmark scores provide the performance comparison.

            ## PvE Benchmark Performance

            | Profile | Builds | Aggregate Range | Leading Sampled Build | Leader Score |
            | --- | ---: | ---: | --- | ---: |
            {{benchmarkProfileRows}}

            | Leading Build | Aggregate | {{benchmarkComponentHeaders}} |
            | --- | ---: | {{benchmarkComponentDividers}} |
            {{benchmarkLeaderRows}}

            Scores use production-engine combat telemetry. Rankings describe this run's random sample and do not yet represent optimizer-selected builds.

            ## Region 1 Gear Packages

            | Progression anchor | Package | Tier | Rarity | Quality | Archetype | Items | CR | Raw CR |
            | --- | --- | ---: | --- | --- | --- | ---: | ---: | ---: |
            {{gearPackageRows}}

            ## Deterministic Smoke Simulation

            | Field | Value |
            | --- | --- |
            | Scenario | `{{simulation.ScenarioId}}` |
            | Friendly | {{EscapeCell(simulation.FriendlyBuild)}} (`{{simulation.FriendlyEssenceId}}`) |
            | Hostile | {{EscapeCell(simulation.HostileBuild)}} (`{{simulation.HostileEssenceId}}`) |
            | Outcome | **{{simulation.Outcome}}** |
            | Duration | {{simulation.DurationTicks}} ticks |
            | Friendly damage done / taken | {{simulation.FriendlyDamageDone}} / {{simulation.FriendlyDamageTaken}} |
            | Hostile damage done / taken | {{simulation.HostileDamageDone}} / {{simulation.HostileDamageTaken}} |

            Re-run with the same production content and seed to reproduce the combat result.
            """;
    }

    private static void ValidateRunId(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId)
            || !string.Equals(Path.GetFileName(runId), runId, StringComparison.Ordinal)
            || runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("Balance run ID is not safe for use as a history directory name.");
        }
    }

    private static string EscapeCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string FormatScore(double score) =>
        score.ToString("F2", CultureInfo.InvariantCulture);

    private static void WriteUtf8(string path, string contents) =>
        File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed record BalanceReportPaths(
    string LatestJsonPath,
    string LatestMarkdownPath,
    string LatestGearPackagesJsonPath,
    string LatestEssenceBuildsJsonPath,
    string LatestBenchmarksJsonPath,
    string HistoryJsonPath,
    string HistoryMarkdownPath,
    string HistoryGearPackagesJsonPath,
    string HistoryEssenceBuildsJsonPath,
    string HistoryBenchmarksJsonPath);
