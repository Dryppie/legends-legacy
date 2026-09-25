using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record TowerThreeReferenceConfirmationBinding(string PlanPath, string AuditorPath, string AuditorHash);

public sealed partial record TowerFixedFamilyRequest
{
    // Omitted for legacy requests so their canonical identities remain unchanged.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TowerThreeReferenceConfirmationBinding? ThreeReference { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TowerThreeReferenceConfirmationBinding? Recognition { get; init; }
}

internal sealed record TowerFixedFamilyPolicy(int Samples, int Candidates, int Family, int EntropyBytes,
    string TeamsHash, bool RetainReferences, int Teams = 8, int Roots = 1)
{
    internal int Fights => Samples * Teams;
    internal int PanelValues => Samples * Roots;
    internal int[] Slices => Enumerable.Repeat(1000, Samples / 1000).Append(Samples % 1000).ToArray();
    internal int MinimumNet => Samples / 20;
}

public static partial class TowerFixedFamilyConfirmation
{
    public const string ThreeReferenceVersion = "tower-practical-three-reference-confirmation-v1";
    internal const string ThreeReferencePlanHash = "c6544a63a569de5197c4e2a35749c01a14388478e6a9986509d991c64bd6a74f";
    internal const string ThreeReferenceTeamsHash = "3ec3b0e93a491ef4837babbf5d8bf6f98745369c53e9be4a6e21f430a590ef32";
    internal static TowerFixedFamilyPolicy Policy(string version) => version switch
    {
        Version => new(Samples, CandidateCount, Family, EntropyBytes, TeamsHash, false),
        ThreeReferenceVersion => new(6500, 5, 38, 52000, ThreeReferenceTeamsHash, true),
        RecognitionVersion => new(256, 6, 540, 24576, RecognitionTeamsHash, true, 108, 12),
        AffinityRecognitionVersion => new(256, 6, 540, 24576, AffinityRecognitionTeamsHash, true, 108, 12),
        PreservationRecognitionVersion => new(256, 109, 799, 24576, PreservationRecognitionTeamsHash, true, 145, 12),
        NeighborhoodRecognitionVersion => new(2048, 41, 46, 65536, NeighborhoodTeamsHash, true, 44),
        _ => throw new InvalidDataException("Unknown fixed-family protocol.")
    };
    internal static long AvailableBytes(TowerFixedFamilyRequest q) => IsRecognition(q.Version)
        ? RecognitionLimits(q.Version).NativeBytes - 4 * 1048576 : q.Version == ThreeReferenceVersion
        ? 3L * 1073741824 - 4 * 1048576 : q.MaximumBytes - q.PriorBytes - CloseoutBytes;
    internal static string Assumption(string version) => version == Version ? SamplingAssumption
        : "One post-freeze cryptographic batch modeled as independent uniform bits; approximate family-38 Wilson coverage; operational completion is not assumed.";
    internal static IReadOnlyList<string> Recommendations(string version, IReadOnlyList<string> qualifiers, IReadOnlyList<string> controls)
        => Policy(version).RetainReferences ? qualifiers.Concat(controls).ToArray() : qualifiers.Count > 0 ? qualifiers : controls;
}
