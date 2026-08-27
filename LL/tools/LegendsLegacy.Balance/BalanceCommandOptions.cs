using System.Globalization;

namespace LegendsLegacy.Balance;

public sealed record BalanceCommandOptions(
    int Seed,
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
          --content-root <path>   API.LL directory containing the production Data folder.
          --output <path>         Report root (default: <repository>/balance-output).
          --full                  Run the currently implemented balance pipeline.
          --help, -h              Show this help.
        """;

    public static BalanceCommandOptions Parse(IReadOnlyList<string> args)
    {
        var seed = DefaultSeed;
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
                case "--output":
                    outputRoot = ReadValue(args, ref index, argument);
                    break;
                default:
                    throw new BalanceCommandException($"Unknown balance-runner argument '{argument}'.");
            }
        }

        return new BalanceCommandOptions(seed, contentRoot, outputRoot, showHelp);
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string argument)
    {
        if (++index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
            throw new BalanceCommandException($"Argument '{argument}' requires a value.");

        return args[index];
    }
}

public sealed class BalanceCommandException(string message) : Exception(message);
