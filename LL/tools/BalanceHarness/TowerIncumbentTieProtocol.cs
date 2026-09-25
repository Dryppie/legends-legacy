namespace BalanceHarness;

public static partial class TowerIncumbentTieComparison
{
    public const string ThreeReferenceVersion = "tower-three-reference-tie-comparison-v1";
    internal const string ThreeReferencePlanHash = "2ae905286313000e04170f45acd849c552251954487c99186d4cea1b96fcdf24";

    // Preserve the original protocol and its byte-stable defaults for archived reconstruction.
    internal sealed record Protocol(int References, string Baseline, string Candidate, int SearchFights,
        int MaximumSeconds, long MaximumBytes, int PriorSeconds, long PriorBytes, int NativeSeconds, long NativeBytes,
        string Support, string Negative)
    {
        internal int Nominees => References + 2;
        internal int FightsPerSearch => SearchFights / Restarts;
        internal int MaximumFights => SearchFights + Restarts * Samples * 2;
        internal int ExecutionSeconds => MaximumSeconds - PriorSeconds;
        internal long ExecutionBytes => MaximumBytes - PriorBytes;
    }

    internal static Protocol Policy(string version) => version switch {
        Version => new(2, TowerBossStudyPolicy.ZeroWinVersion, TowerBossStudyPolicy.IncumbentTieVersion,
            SearchFights, MaximumSeconds, MaximumBytes, 0, 0, NativeSeconds, NativeBytes,
            "SupportsIncumbentTieForFrozenOutputs", "DoNotPromoteIncumbentTie"),
        ThreeReferenceVersion => new(3, TowerBossStudyPolicy.IncumbentTieVersion, TowerBossStudyPolicy.ThreeReferenceTieVersion,
            12672, 10800, 6442450944, 600, 536870912, 10080, 5637144576,
            "SupportsThreeReferenceTieForFrozenOutputs", "DoNotPromoteThreeReferenceTie"),
        _ => throw new InvalidDataException("Unknown selector comparison protocol.")
    };
}
