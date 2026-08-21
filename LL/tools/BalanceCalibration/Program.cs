using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Services.LL.Combat;
using Services.LL.Combat.Engine;
using Services.LL.Essences;
using Services.LL.Regions;

return await RunAsync(args);

static Task<int> RunAsync(string[] args)
{
    try
    {
        var arguments = CommandArguments.Parse(args);
        if (arguments.ShowHelp)
        {
            Console.WriteLine(CommandArguments.HelpText);
            return Task.FromResult(0);
        }

        var repositoryRoot = FindRepositoryRoot(Environment.CurrentDirectory);
        var apiRoot = arguments.ContentRootPath
                      ?? Path.Combine(repositoryRoot, "LL", "src", "API", "API.LL");
        var outputRoot = arguments.OutputDirectory
                         ?? Path.Combine(
                             repositoryRoot,
                             "artifacts",
                             "balance-calibration",
                             arguments.StaggerOnly
                                 ? "stagger"
                                 : arguments.IsFocused ? "focused" : string.Empty);
        var dataPath = Path.Combine(apiRoot, "Data");
        if (!Directory.Exists(dataPath))
            throw new DirectoryNotFoundException($"API content directory was not found: {dataPath}");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Content:Root"] = "Data"
            })
            .Build();
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
        if (arguments.StaggerOnly)
        {
            var staggerCatalog = new StaggerCalibrationCatalogFactory(
                    configuration,
                    apiRoot,
                    jsonOptions)
                .CreateCatalog();
            var staggerReport = new StaggerCalibrationRunner().Run(
                staggerCatalog,
                new StaggerCalibrationRunOptions(
                    arguments.EncounterIds,
                    arguments.CohortIds,
                    arguments.StaggerProfileIds,
                    arguments.Samples));
            var staggerArtifact = StaggerCalibrationReportRenderer.CreateArtifact(
                staggerCatalog,
                staggerReport);
            Directory.CreateDirectory(outputRoot);
            var staggerJsonPath = Path.Combine(outputRoot, "stagger-calibration-report.json");
            var staggerMarkdownPath = Path.Combine(outputRoot, "stagger-calibration-report.md");
            var staggerUtf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(
                staggerJsonPath,
                StaggerCalibrationReportRenderer.RenderJson(staggerArtifact),
                staggerUtf8WithoutBom);
            File.WriteAllText(
                staggerMarkdownPath,
                StaggerCalibrationReportRenderer.RenderMarkdown(staggerArtifact),
                staggerUtf8WithoutBom);

            Console.WriteLine(
                $"Stagger calibration completed: {staggerArtifact.Summary.ResultCount} results, " +
                $"{staggerArtifact.Summary.SampleCount} deterministic samples, " +
                $"{staggerArtifact.Summary.ExceptionCount} exceptions.");
            Console.WriteLine($"JSON: {Path.GetFullPath(staggerJsonPath)}");
            Console.WriteLine($"Markdown: {Path.GetFullPath(staggerMarkdownPath)}");
            return Task.FromResult(0);
        }

        var regionScaling = new RegionCreatureScalingProvider(configuration, apiRoot, jsonOptions);
        var creatureAbilities = new JsonCreatureAbilityDefinitionProvider(configuration, apiRoot, jsonOptions);
        var encounterFactory = new AuthoredEncounterCalibrationFactory(
            configuration,
            apiRoot,
            jsonOptions,
            regionScaling,
            creatureAbilities);
        var snapshotFactory = new PlayerProgressionSnapshotFactory(configuration, apiRoot, jsonOptions);
        var slotUnlocks = new EssenceSlotUnlockService();
        var playerFactory = new EssenceCalibrationMatrixFactory(
            configuration,
            apiRoot,
            jsonOptions,
            snapshotFactory,
            slotUnlocks);
        var abilityCatalog = new JsonAbilityCatalogProvider(configuration, apiRoot, jsonOptions);
        var essenceDefinitions = new JsonEssenceDefinitionRepository(
            configuration,
            apiRoot,
            jsonOptions,
            new EssenceDefinitionValidator());
        var catalog = encounterFactory.CreateCatalog();
        var report = new EncounterCalibrationRunner(abilityCatalog, essenceDefinitions)
            .Run(
                catalog,
                playerFactory.CreateScenarios(),
                new EncounterCalibrationRunOptions(
                    arguments.EncounterIds,
                    arguments.GearEnvelopeIds,
                    arguments.BuildFamilyIds,
                    arguments.PartyCompositionIds,
                    arguments.EssenceEnvelopeIds,
                    arguments.Samples));
        EncounterCalibrationArtifact? baseline = null;
        if (!string.IsNullOrWhiteSpace(arguments.BaselinePath))
        {
            var baselinePath = Path.GetFullPath(arguments.BaselinePath);
            baseline = EncounterCalibrationReportRenderer.ReadJson(
                File.ReadAllText(baselinePath));
        }

        var artifact = EncounterCalibrationReportRenderer.CreateArtifact(report, catalog, baseline);
        Directory.CreateDirectory(outputRoot);
        var jsonPath = Path.Combine(outputRoot, "encounter-calibration-report.json");
        var markdownPath = Path.Combine(outputRoot, "encounter-calibration-report.md");
        var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(
            jsonPath,
            EncounterCalibrationReportRenderer.RenderJson(artifact),
            utf8WithoutBom);
        File.WriteAllText(
            markdownPath,
            EncounterCalibrationReportRenderer.RenderMarkdown(artifact),
            utf8WithoutBom);

        Console.WriteLine($"Encounter calibration completed: {artifact.Summary.ResultCount} results, {artifact.Summary.SeededSampleCount} seeded samples, {artifact.Summary.ExceptionCount} exceptions.");
        Console.WriteLine($"JSON: {Path.GetFullPath(jsonPath)}");
        Console.WriteLine($"Markdown: {Path.GetFullPath(markdownPath)}");
        if (artifact.Comparison is not null)
        {
            Console.WriteLine(
                $"Baseline delta: {artifact.Comparison.ResultChanges.Count} changed rows, " +
                $"{artifact.Comparison.IntroducedExceptions.Count} introduced exceptions, " +
                $"{artifact.Comparison.ResolvedExceptions.Count} resolved exceptions.");
        }

        return Task.FromResult(0);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Encounter calibration failed: {exception.Message}");
        return Task.FromResult(1);
    }
}

static string FindRepositoryRoot(string startPath)
{
    for (var current = new DirectoryInfo(Path.GetFullPath(startPath)); current is not null; current = current.Parent)
    {
        if (Directory.Exists(Path.Combine(current.FullName, "LL", "src", "API", "API.LL", "Data")))
            return current.FullName;
    }

    throw new DirectoryNotFoundException(
        "Could not locate the LegendsLegacy repository root from the current directory.");
}

internal sealed record CommandArguments(
    string? ContentRootPath,
    string? OutputDirectory,
    string? BaselinePath,
    IReadOnlyList<string> EncounterIds,
    IReadOnlyList<string> GearEnvelopeIds,
    IReadOnlyList<string> BuildFamilyIds,
    IReadOnlyList<string> PartyCompositionIds,
    IReadOnlyList<string> EssenceEnvelopeIds,
    IReadOnlyList<string> CohortIds,
    IReadOnlyList<string> StaggerProfileIds,
    int? Samples,
    bool StaggerOnly,
    bool ShowHelp)
{
    public bool IsFocused => EncounterIds.Count > 0
                             || GearEnvelopeIds.Count > 0
                             || BuildFamilyIds.Count > 0
                             || PartyCompositionIds.Count > 0
                             || EssenceEnvelopeIds.Count > 0
                             || Samples.HasValue;

    public const string HelpText = """
        LegendsLegacy authored encounter calibration

        Options:
          --content-root <path>       API.LL directory containing Data (auto-detected by default)
          --output <path>             Output directory (focused runs default to a focused subdirectory)
          --baseline <path>           Previous encounter-calibration-report.json to compare
          --encounter <id>            Include an encounter; repeat or use comma-separated IDs
          --gear <id>                 Include a gear envelope; repeat or use comma-separated IDs
          --build <id>                Include a solo build family; repeat or use comma-separated IDs
          --composition <id>          Include a Tower party composition; repeat or use comma-separated IDs
          --essence <id>              Include an Essence envelope; repeat or use comma-separated IDs
          --stagger-only              Run the isolated Tower/Raid Stagger calibration
          --cohort <id>               Include a Stagger party-size cohort; repeat or comma-separate
          --stagger-profile <id>      Include a Stagger control profile; repeat or comma-separate
          --samples <1-1000>          Deterministic samples per selected result row
          --help                      Show this help
        """;

    public static CommandArguments Parse(IReadOnlyList<string> args)
    {
        string? contentRoot = null;
        string? output = null;
        string? baseline = null;
        var encounters = new List<string>();
        var gearEnvelopes = new List<string>();
        var buildFamilies = new List<string>();
        var partyCompositions = new List<string>();
        var essenceEnvelopes = new List<string>();
        var cohorts = new List<string>();
        var staggerProfiles = new List<string>();
        int? samples = null;
        var staggerOnly = false;
        var showHelp = false;
        for (var index = 0; index < args.Count; index++)
        {
            switch (args[index])
            {
                case "--content-root":
                    contentRoot = ReadValue(args, ref index, "--content-root");
                    break;
                case "--output":
                    output = ReadValue(args, ref index, "--output");
                    break;
                case "--baseline":
                    baseline = ReadValue(args, ref index, "--baseline");
                    break;
                case "--encounter":
                    AddValues(encounters, ReadValue(args, ref index, "--encounter"));
                    break;
                case "--gear":
                    AddValues(gearEnvelopes, ReadValue(args, ref index, "--gear"));
                    break;
                case "--build":
                    AddValues(buildFamilies, ReadValue(args, ref index, "--build"));
                    break;
                case "--composition":
                    AddValues(partyCompositions, ReadValue(args, ref index, "--composition"));
                    break;
                case "--essence":
                    AddValues(essenceEnvelopes, ReadValue(args, ref index, "--essence"));
                    break;
                case "--stagger-only":
                    staggerOnly = true;
                    break;
                case "--cohort":
                    AddValues(cohorts, ReadValue(args, ref index, "--cohort"));
                    break;
                case "--stagger-profile":
                    AddValues(staggerProfiles, ReadValue(args, ref index, "--stagger-profile"));
                    break;
                case "--samples":
                    var value = ReadValue(args, ref index, "--samples");
                    if (!int.TryParse(value, out var parsedSamples) || parsedSamples is < 1 or > 1_000)
                        throw new ArgumentException("--samples must be an integer between 1 and 1,000.");
                    samples = parsedSamples;
                    break;
                case "--help" or "-h":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{args[index]}'. Use --help for usage.");
            }
        }

        return new CommandArguments(
            contentRoot is null ? null : Path.GetFullPath(contentRoot),
            output is null ? null : Path.GetFullPath(output),
            baseline is null ? null : Path.GetFullPath(baseline),
            encounters,
            gearEnvelopes,
            buildFamilies,
            partyCompositions,
            essenceEnvelopes,
            cohorts,
            staggerProfiles,
            samples,
            staggerOnly,
            showHelp);
    }

    private static void AddValues(ICollection<string> target, string value)
    {
        foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!target.Contains(item, StringComparer.OrdinalIgnoreCase))
                target.Add(item);
        }
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string option)
    {
        index++;
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
            throw new ArgumentException($"{option} requires a path value.");
        return args[index];
    }
}
