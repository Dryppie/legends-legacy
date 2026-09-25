namespace BalanceHarness;

/// <summary>Phase restrictions for explicit proposal pending declarations.
/// Dynamic path enumeration and resource admission are separate obligations.</summary>
internal static class TowerProposalPendingPlan
{
    internal static string[] ProtectedPaths(string root) => new[] { "content", "executable", "source", "request.json", "launch.json",
        "launcher.py", "auditor.py", "bounded_windows_process.py", "admission-files.json", "admission-receipt.json", "admission-binding.json" }
        .Select(name => Path.Combine(root, name)).ToArray();
    internal static void Validate(string root, string phase, PendingStorageBudget budget)
    {
        if (phase is "nativeAudit" or "publication")
        {
            TowerProposalStudy.Require(budget.Files is { Count: 0 } && budget.MaxLiveBytes == 0 && budget.MaxTotalWrittenBytes == 0,
                "Audit/publication phases prohibit pending writers.");
            return;
        }
        TowerProposalStudy.Require(phase == "native" && budget.Files is { Count: > 0 }, "Incomplete native pending phase declaration.");
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar;
        foreach (var file in budget.Files!)
        {
            TowerProposalStudy.Require(file is not null && Path.GetFullPath(file.Destination).StartsWith(prefix, comparison)
                && string.Equals(Path.GetFullPath(file.Path), Path.GetFullPath(file.Destination) + ".pending", comparison),
                "Proposal pending paths must be target-adjacent members of the study output.");
        }
    }
}
