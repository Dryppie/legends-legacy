namespace BalanceHarness;

// Resource versions are independent of the frozen scientific design. A missing
// field is the original archive contract, never an alias for the latest limits.
public sealed record ProposalStudyResources(string Version, int NativeSeconds, int AuditSeconds);

public static partial class TowerProposalStudy
{
    public const string ResourceV1 = "tower-proposal-resource-envelope-v1";
    public const string ResourceV2 = "tower-proposal-resource-envelope-v2";

    internal static ProposalStudyResources Resources(string? version) => version switch {
        null or ResourceV1 => new(ResourceV1, 9600, 1200),
        ResourceV2 => new(ResourceV2, 9000, 1800),
        _ => throw new InvalidDataException("Unknown proposal resource envelope.")
    };
}
