namespace BalanceHarness;

internal sealed record ProposalPendingFamiliesBudget(string Version, long MaxFileBytes, long MaxLiveBytes, long MaxTotalWrittenBytes);

/// <summary>Finite command catalogue, including recipe-dependent names. This bounds
/// selected pending writers, not all study files or filesystem operations.</summary>
internal sealed class TowerProposalPendingFamilies
{
    internal const string Version = "tower-proposal-pending-families-v1";
    internal const string StorageVersion = "tower-native-pending-storage-v2";
    internal readonly string Root, Phase;
    internal readonly ProposalPendingFamiliesBudget Budget;
    private readonly int[] members = new int[12];
    internal TowerProposalPendingFamilies(string root, string phase, ProposalPendingFamiliesBudget budget)
    {
        TowerProposalStudy.Require(budget.Version == Version && budget.MaxFileBytes >= 0 && budget.MaxLiveBytes >= 0
            && budget.MaxTotalWrittenBytes >= 0 && phase is "native" or "nativeAudit" or "publication",
            "Invalid proposal pending family declaration.");
        TowerProposalStudy.Require(phase == "native" || budget.MaxFileBytes == 0 && budget.MaxLiveBytes == 0 && budget.MaxTotalWrittenBytes == 0,
            "Audit/publication phases prohibit pending writers.");
        Root = Path.GetFullPath(root); Phase = phase; Budget = budget with { };
    }
    internal static IEnumerable<string> FixedNames()
    {
        foreach (var name in new[] { "history-files.json", "entropy-intent.json", "history-input.json", "entropy.bin", "allocation.json",
            "seed-ledger.json", "provisional-result.json", "native-receipt.json", "study/binding.json", "study/freeze.json",
            "study/summary.json", "study/evidence-storage.json" }) yield return name;
        for (var root = 1; root <= 12; root++)
        {
            yield return $"study/pair-{root:D2}.json";
            yield return $"study/placement-catalogue-{root:D2}.json";
        }
    }
    private PendingFileBudget File(string name)
    {
        var target = Path.GetFullPath(Path.Combine(Root, name));
        return new(target + ".pending", target, Budget.MaxFileBytes, name == "history-input.json" ? 2 : 1);
    }
    internal PendingStorageBudget FixedBudget() => new(Phase == "native" ? FixedNames().Select(File).ToArray() : [],
        Budget.MaxLiveBytes, Budget.MaxTotalWrittenBytes);

    // Called under the pending scope lock, only for a never-before-seen path.
    // Counts are consumed before the factory and never refunded by delete/move.
    internal PendingFileBudget Add(string path)
    {
        var relative = Path.GetRelativePath(Root, path).Replace('\\', '/');
        var name = relative.EndsWith(".pending", StringComparison.Ordinal) ? relative[..^8] : "";
        var valid = Phase == "native" && name.Length == 86 && name.StartsWith("study/heldout-", StringComparison.Ordinal)
            && name[16] == '-' && name.EndsWith(".json", StringComparison.Ordinal)
            && name.AsSpan(17, 64).ContainsAnyExcept("0123456789abcdef".AsSpan()) == false;
        var index = valid && name[14] is >= '0' and <= '1' && name[15] is >= '0' and <= '9'
            ? (name[14] - '0') * 10 + name[15] - '0' - 1 : -1;
        TowerProposalStudy.Require(index is >= 0 and < 12 && members[index] < 3, "Undeclared or exhausted pending family.");
        members[index]++;
        return File(name);
    }
}
