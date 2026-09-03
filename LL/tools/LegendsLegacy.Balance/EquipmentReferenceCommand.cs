using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Services.LL.PowerRatings;

namespace LegendsLegacy.Balance;

internal static class EquipmentReferenceCommand
{
    internal const string Switch = "--equipment-reference-builds";
    private const string Usage = "Equipment progression references: --equipment-reference-builds [--region-two-transition | --meran-pve [--trials <1..1024>] [--essence-level <10|30>] [--dungeon-level <50..65>]] [--seed <number>] [--content-root <API.LL path>] [--output <directory>]";
    internal static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, Converters = { new JsonStringEnumConverter() }
    };

    internal static int Run(string[] args)
    {
        var transition = false;
        var meran = false;
        int? trials = null;
        int? essenceLevel = null;
        int? dungeonLevel = null;
        var seed = BalanceCommandOptions.DefaultSeed;
        string? content = null;
        string? output = null;
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument is Switch) continue;
            if (argument == "--region-two-transition") { transition = true; continue; }
            if (argument == "--meran-pve") { meran = true; continue; }
            if (argument is "--help" or "-h") { Console.WriteLine(Usage); return 0; }
            if (argument is not ("--seed" or "--content-root" or "--output" or "--trials" or "--essence-level" or "--dungeon-level"))
                throw new BalanceCommandException($"Unsupported Equipment progression reference option '{argument}'. {Usage}");
            if (++index == args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                throw new BalanceCommandException($"Missing value for '{argument}'. {Usage}");
            if (argument == "--seed")
            {
                if (!int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                    throw new BalanceCommandException("Reference seed must be an integer.");
            }
            else if (argument == "--dungeon-level")
            {
                if (!int.TryParse(args[index], out var level) || level is < 50 or > 65)
                    throw new BalanceCommandException("Meran dungeon character level must be between 50 and 65.");
                dungeonLevel = level;
            }
            else if (argument == "--essence-level")
            {
                if (!int.TryParse(args[index], out var level) || level is not (10 or 30))
                    throw new BalanceCommandException("Meran Essence level must be 10 or 30.");
                essenceLevel = level;
            }
            else if (argument == "--trials")
            {
                if (!int.TryParse(args[index], out var count) || count is < 1 or > 1024)
                    throw new BalanceCommandException("Trials must be between 1 and 1024.");
                trials = count;
            }
            else if (argument == "--content-root") content = args[index];
            else output = args[index];
        }
        if (meran && transition || (trials.HasValue || essenceLevel.HasValue || dungeonLevel.HasValue) && !meran)
            throw new BalanceCommandException("Use --trials, --essence-level and --dungeon-level with --meran-pve; select only one assessment mode.");
        var contentRoot = BalancePathLocator.FindApiContentRoot(content);
        var outputRoot = Path.GetFullPath(output ?? Path.Combine(BalancePathLocator.FindRepositoryRoot(contentRoot), "balance-output"));
        if (meran)
        {
            var assessment = ProductionBalanceComposition.CreateMeranAssessment(contentRoot).RunAsync(seed, trials ?? 32, essenceLevel ?? 10, dungeonLevel ?? 50).GetAwaiter().GetResult();
            Directory.CreateDirectory(outputRoot);
            var assessmentPath = Path.Combine(outputRoot, "equipment-meran-pve.json");
            File.WriteAllText(assessmentPath, JsonSerializer.Serialize(assessment, JsonOptions));
            Console.WriteLine($"Meran assessment: {assessment.Results.Sum(r => r.Trials.Count)} fights. Report: {assessmentPath}");
            return 0;
        }
        var fixtureBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", EquipmentReferenceReportRunner.FixtureFileName));
        var profiles = JsonSerializer.Deserialize<EquipmentReferenceBuildDefinition[]>(fixtureBytes, JsonOptions)
            ?? throw new InvalidOperationException("Equipment progression reference fixtures are missing.");
        if (transition) profiles = profiles.SelectMany(p => new[] {
            p with { CharacterLevel = 50 }, p with { Id = "tier2-" + p.Id, CharacterLevel = 50, Tier = 2 } }).ToArray();
        var report = ProductionBalanceComposition.CreateEquipmentReferences(contentRoot)
            .RunAsync(profiles, seed, Convert.ToHexString(SHA256.HashData(fixtureBytes)), regionTwoTransition: transition).GetAwaiter().GetResult();
        Directory.CreateDirectory(outputRoot);
        var path = Path.Combine(outputRoot, transition ? "equipment-region-two-transition.json" : "equipment-reference-builds.json");
        File.WriteAllText(path, JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine($"Equipment progression references: {report.Builds.Count} builds and production combat checks. {report.Purpose}");
        Console.WriteLine($"Report: {path}");
        return 0;
    }
}
