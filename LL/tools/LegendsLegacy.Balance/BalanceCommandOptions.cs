using System.Globalization;

namespace LegendsLegacy.Balance;

public sealed record BalanceCommandOptions(
    int Seed,
    int EssenceBuildsPerProfile,
    EssenceOptimizerOptions OptimizerOptions,
    RepresentativeBuildOptions RepresentativeBuildOptions,
    ProgressionBandOptions ProgressionBandOptions,
    WorldTowerAnalysisOptions WorldTowerAnalysisOptions,
    EssenceMetaAnalysisOptions EssenceMetaAnalysisOptions,
    EncounterCalibrationOptions EncounterCalibrationOptions,
    EncounterSpecificOptimizationOptions EncounterSpecificOptimizationOptions,
    ScalingValidationOptions ScalingValidationOptions,
    string? ContentRoot,
    string? OutputRoot,
    bool ShowHelp)
{
    public const int DefaultSeed = 1337;

    public const string Usage = """
        LegendsLegacy balance runner

        Usage:
          dotnet run --project LL/tools/LegendsLegacy.Balance -- [--full] [options]

        Options:
          --seed <number>         Deterministic simulation seed (default: 1337).
          --build-count <number>  Random builds per 4/5/6-slot profile (default: 10).
          --optimizer-population <number>  Candidates per profile (default: 20).
          --optimizer-generations <number> Generations to evolve (default: 4).
          --optimizer-elites <number>      Elites retained per generation (default: 5).
          --optimizer-mutation <number>    Per-slot mutation rate, 0.01-1.00 (default: 0.25).
          --optimizer-random <number>      Random injection rate, 0.00-0.50 (default: 0.10).
          --optimizer-diversity <number>   Similarity penalty, 0-100 (default: 8).
          --optimizer-retained <number>    Final candidates per profile (default: 10).
          --representative-count <number>  Builds retained per P50/P75/P90 profile (default: 10).
          --progression-curve <value>      linear, ease-in, ease-out, or smooth-step (default).
          --tower-simulations <number>     Seeded party simulations per Floor 1-10 (default: 10).
          --calibration-iterations <number>  Bounded encounter-search iterations (default: 10).
          --encounter-candidate-simulations <number>  Trials per specialized candidate (default: 3).
          --encounter-retained <number>      Specialized builds retained per floor (default: 5).
          --validation-seeds <number>        Deterministic holdout seeds per floor (default: 8).
          --validation-simulations <number>  Calibrated trials per holdout seed (default: 50).
          --validation-probe-simulations <number>  Trials per sensitivity probe and seed (default: 25).
          --meta-simulator-battles <number>  Complementary 1v1 Essence battles (default: 2000).
          --content-root <path>   API.LL directory containing the production Data folder.
          --output <path>         Report root (default: <repository>/balance-output).
          --full                  Run the currently implemented balance pipeline.
          --help, -h              Show this help.
        """;

    public static BalanceCommandOptions Parse(IReadOnlyList<string> args)
    {
        var seed = DefaultSeed;
        var essenceBuildsPerProfile = 10;
        var optimizerPopulation = 20;
        var optimizerGenerations = 4;
        var optimizerElites = 5;
        var optimizerMutation = 0.25;
        var optimizerRandom = 0.10;
        var optimizerDiversity = 8d;
        var optimizerRetained = 10;
        var representativeCount = 10;
        var progressionCurve = ProgressionCurveKind.SmoothStep;
        var towerSimulations = 10;
        var calibrationIterations = 10;
        var encounterCandidateSimulations = 3;
        var encounterRetained = 5;
        var validationSeeds = 8;
        var validationSimulations = 50;
        var validationProbeSimulations = 25;
        var metaSimulatorBattles = 2_000;
        string? contentRoot = null;
        string? outputRoot = null;
        var showHelp = false;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--full":
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                case "--seed":
                    var seedValue = ReadValue(args, ref index, argument);
                    if (!int.TryParse(seedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                        throw new BalanceCommandException($"Invalid seed '{seedValue}'. Expected a 32-bit integer.");
                    break;
                case "--content-root":
                    contentRoot = ReadValue(args, ref index, argument);
                    break;
                case "--optimizer-population":
                    optimizerPopulation = ReadInt(args, ref index, argument, 4, 500);
                    break;
                case "--optimizer-generations":
                    optimizerGenerations = ReadInt(args, ref index, argument, 1, 100);
                    break;
                case "--optimizer-elites":
                    optimizerElites = ReadInt(args, ref index, argument, 1, 499);
                    break;
                case "--optimizer-mutation":
                    optimizerMutation = ReadDouble(args, ref index, argument, 0.01, 1);
                    break;
                case "--optimizer-random":
                    optimizerRandom = ReadDouble(args, ref index, argument, 0, 0.5);
                    break;
                case "--optimizer-diversity":
                    optimizerDiversity = ReadDouble(args, ref index, argument, 0, 100);
                    break;
                case "--optimizer-retained":
                    optimizerRetained = ReadInt(args, ref index, argument, 1, 500);
                    break;
                case "--representative-count":
                    representativeCount = ReadInt(args, ref index, argument, 1, 500);
                    break;
                case "--progression-curve":
                    progressionCurve = ParseProgressionCurve(ReadValue(args, ref index, argument));
                    break;
                case "--tower-simulations":
                    towerSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--calibration-iterations":
                    calibrationIterations = ReadInt(args, ref index, argument, 1, 20);
                    break;
                case "--encounter-candidate-simulations":
                    encounterCandidateSimulations = ReadInt(args, ref index, argument, 1, 100);
                    break;
                case "--encounter-retained":
                    encounterRetained = ReadInt(args, ref index, argument, 1, 50);
                    break;
                case "--validation-seeds":
                    validationSeeds = ReadInt(args, ref index, argument, 2, 50);
                    break;
                case "--validation-simulations":
                    validationSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--validation-probe-simulations":
                    validationProbeSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--meta-simulator-battles":
                    metaSimulatorBattles = ReadInt(args, ref index, argument, 1, 1_000_000);
                    break;
                case "--build-count":
                    var buildCountValue = ReadValue(args, ref index, argument);
                    if (!int.TryParse(
                            buildCountValue,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out essenceBuildsPerProfile)
                        || essenceBuildsPerProfile is < 1 or > 1_000)
                    {
                        throw new BalanceCommandException(
                            $"Invalid build count '{buildCountValue}'. Expected a number from 1 to 1,000.");
                    }
                    break;
                case "--output":
                    outputRoot = ReadValue(args, ref index, argument);
                    break;
                default:
                    throw new BalanceCommandException($"Unknown balance-runner argument '{argument}'.");
            }
        }

        var optimizer = new EssenceOptimizerOptions(
            optimizerPopulation,
            optimizerGenerations,
            optimizerElites,
            optimizerMutation,
            optimizerRandom,
            optimizerDiversity,
            optimizerRetained);
        try
        {
            optimizer.Validate();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new BalanceCommandException(exception.Message);
        }
        var representativeBuilds = new RepresentativeBuildOptions(representativeCount);
        try
        {
            representativeBuilds.Validate();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new BalanceCommandException(exception.Message);
        }
        var minimumEvaluatedCandidates = optimizer.PopulationSize
                                         + optimizer.Generations * (optimizer.PopulationSize - optimizer.EliteCount);
        if (representativeBuilds.BuildsPerProfile > minimumEvaluatedCandidates)
        {
            throw new BalanceCommandException(
                $"Representative build count must not exceed the optimizer's minimum evaluated population " +
                $"of {minimumEvaluatedCandidates} candidates per slot profile.");
        }
        if (encounterRetained > minimumEvaluatedCandidates)
        {
            throw new BalanceCommandException(
                $"Encounter retained build count must not exceed the optimizer's minimum evaluated population " +
                $"of {minimumEvaluatedCandidates} candidates per slot profile.");
        }

        return new BalanceCommandOptions(
            seed,
            essenceBuildsPerProfile,
            optimizer,
            representativeBuilds,
            new ProgressionBandOptions(progressionCurve),
            new WorldTowerAnalysisOptions(towerSimulations),
            new EssenceMetaAnalysisOptions(metaSimulatorBattles),
            new EncounterCalibrationOptions(SearchIterations: calibrationIterations),
            new EncounterSpecificOptimizationOptions(
                CandidateSimulations: encounterCandidateSimulations,
                RetainedBuilds: encounterRetained),
            new ScalingValidationOptions(
                HoldoutSeeds: validationSeeds,
                SimulationsPerSeed: validationSimulations,
                ProbeSimulationsPerSeed: validationProbeSimulations),
            contentRoot,
            outputRoot,
            showHelp);
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string argument)
    {
        if (++index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
            throw new BalanceCommandException($"Argument '{argument}' requires a value.");

        return args[index];
    }

    private static int ReadInt(
        IReadOnlyList<string> args,
        ref int index,
        string argument,
        int minimum,
        int maximum)
    {
        var value = ReadValue(args, ref index, argument);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            || result < minimum
            || result > maximum)
        {
            throw new BalanceCommandException(
                $"Invalid value '{value}' for '{argument}'. Expected {minimum} through {maximum}.");
        }
        return result;
    }

    private static double ReadDouble(
        IReadOnlyList<string> args,
        ref int index,
        string argument,
        double minimum,
        double maximum)
    {
        var value = ReadValue(args, ref index, argument);
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            || !double.IsFinite(result)
            || result < minimum
            || result > maximum)
        {
            throw new BalanceCommandException(
                $"Invalid value '{value}' for '{argument}'. Expected {minimum} through {maximum}.");
        }
        return result;
    }

    private static ProgressionCurveKind ParseProgressionCurve(string value) =>
        value.ToLowerInvariant() switch
        {
            "linear" => ProgressionCurveKind.Linear,
            "ease-in" => ProgressionCurveKind.EaseIn,
            "ease-out" => ProgressionCurveKind.EaseOut,
            "smooth-step" => ProgressionCurveKind.SmoothStep,
            _ => throw new BalanceCommandException(
                $"Invalid progression curve '{value}'. Expected linear, ease-in, ease-out, or smooth-step.")
        };
}

public sealed class BalanceCommandException(string message) : Exception(message);
