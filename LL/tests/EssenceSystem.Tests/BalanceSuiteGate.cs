using System.Globalization;
using Domain.Models.Professions.Crafting.V2;

namespace EssenceSystem.Tests;

/// <summary>
/// Decides whether the exhaustive balance suite executes in the current test session.
/// </summary>
/// <remarks>
/// <para>
/// Continuous integration owns the decision through workflow level test filters, so the gate is
/// inert there and never skips.
/// </para>
/// <para>
/// Locally the suite costs minutes and only produces new information once the equipment stat
/// budget moves, so it runs when <see cref="EquipmentStatBudgetCatalog.BalanceVersion"/> differs
/// from the version recorded in the stamp file and is skipped otherwise. The stamp is written by
/// <c>build/run-tests.ps1</c> after a successful balance run.
/// </para>
/// </remarks>
internal static class BalanceSuiteGate
{
    /// <summary>Repository relative location of the local "last balance run" stamp.</summary>
    public const string StampRelativePath = ".artifacts/balance-suite.version";

    /// <summary>Set to <c>1</c> to force the suite or <c>0</c> to skip it, ignoring the stamp.</summary>
    public const string OverrideEnvironmentVariable = "LL_RUN_BALANCE";

    private static readonly Lazy<string?> LazySkipReason =
        new(ResolveSkipReason, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The equipment balance version the current build was compiled against.</summary>
    public static int EquipmentBalanceVersion => EquipmentStatBudgetCatalog.BalanceVersion;

    /// <summary>
    /// <c>null</c> when the suite should run, otherwise the reason reported by the test runner.
    /// </summary>
    public static string? SkipReason => LazySkipReason.Value;

    /// <summary>
    /// The absolute stamp path, or <c>null</c> when the repository root cannot be located.
    /// </summary>
    public static string? FindStampPath()
    {
        var root = FindRepositoryRoot();
        if (root is null)
            return null;

        var combined = root;
        foreach (var segment in StampRelativePath.Split('/'))
            combined = Path.Combine(combined, segment);

        return Path.GetFullPath(combined);
    }

    private static string? ResolveSkipReason()
    {
        if (TryReadOverride(out var forced))
        {
            return forced
                ? null
                : $"Balance suite skipped: {OverrideEnvironmentVariable} disables it for this run.";
        }

        // CI selects the balance shards with test filters, so never second guess it there.
        if (IsContinuousIntegration())
            return null;

        var stampPath = FindStampPath();
        if (stampPath is null)
            return null;

        string stampContent;
        try
        {
            if (!File.Exists(stampPath))
                return null;

            stampContent = File.ReadAllText(stampPath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        if (!int.TryParse(
                stampContent.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var recordedVersion))
        {
            return null;
        }

        if (recordedVersion != EquipmentBalanceVersion)
            return null;

        return $"Balance suite skipped locally: equipment balance version v{EquipmentBalanceVersion} is "
            + $"unchanged since the last recorded local balance run. Bump "
            + $"{nameof(EquipmentStatBudgetCatalog)}.{nameof(EquipmentStatBudgetCatalog.BalanceVersion)}, "
            + $"delete '{StampRelativePath}', or set {OverrideEnvironmentVariable}=1 to run it anyway.";
    }

    private static bool TryReadOverride(out bool forced)
    {
        forced = false;
        var value = Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();
        if (IsTrue(value))
        {
            forced = true;
            return true;
        }

        if (IsFalse(value))
            return true;

        return false;
    }

    private static bool IsContinuousIntegration() =>
        IsTrue(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
        || IsTrue(Environment.GetEnvironmentVariable("TF_BUILD"))
        || IsTrue(Environment.GetEnvironmentVariable("CI"));

    private static bool IsTrue(string? value) =>
        value is not null
        && (value.Trim().Equals("1", StringComparison.Ordinal)
            || value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase));

    private static bool IsFalse(string? value) =>
        value is not null
        && (value.Trim().Equals("0", StringComparison.Ordinal)
            || value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase)
            || value.Trim().Equals("no", StringComparison.OrdinalIgnoreCase));

    private static string? FindRepositoryRoot()
    {
        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var current = new DirectoryInfo(startPath);
            while (current is not null)
            {
                var gitPath = Path.Combine(current.FullName, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                    return current.FullName;

                current = current.Parent;
            }
        }

        return null;
    }
}
