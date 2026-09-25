namespace BalanceHarness;

public static partial class TowerFixedFamilyConfirmation
{
    public const string AffinityRecognitionVersion = "tower-affinity-creation-recognition-v1";
    internal const string AffinityRecognitionPlanHash = "170065b593a49609e72142d766443c3a47f7a271b937cc88d52436de2b4792a2";
    internal const string AffinityRecognitionTeamsHash = "9fcf23a8934986ad08b2a34a3096230369d9d0817e8e70dc5b2d6b46d715c7e5";

    internal static bool IsRecognition(string version) => version is RecognitionVersion or AffinityRecognitionVersion or PreservationRecognitionVersion or NeighborhoodRecognitionVersion;

    // Each profile admits exactly its own frozen cohort; neither accepts an arbitrary plan.
    internal static string RecognitionPlanPin(string version) => version switch
    {
        RecognitionVersion => RecognitionPlanHash,
        AffinityRecognitionVersion => AffinityRecognitionPlanHash,
        PreservationRecognitionVersion => PreservationRecognitionPlanHash,
        NeighborhoodRecognitionVersion => NeighborhoodPlanHash,
        _ => throw new InvalidDataException("Unknown recognition protocol.")
    };

    internal static string RecognitionCommandPrefix(string version) => version switch
    {
        RecognitionVersion => "tower-frozen-pool-recognition",
        AffinityRecognitionVersion => "tower-affinity-creation-recognition",
        PreservationRecognitionVersion => "tower-affinity-preservation-recognition",
        NeighborhoodRecognitionVersion => "tower-affinity-neighborhood-recognition",
        _ => throw new InvalidDataException("Unknown recognition command profile.")
    };
}
