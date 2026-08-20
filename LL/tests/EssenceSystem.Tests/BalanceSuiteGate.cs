using System.Security.Cryptography;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Raids;
using Domain.Models.Professions.Crafting.V2;
using Services.LL.Balance;
using Services.LL.PowerRatings;
using Services.LL.WorldTower;

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
/// Locally the suite costs minutes and only produces new information once a balance input moves,
/// so it compares a composite identity covering code versions and data-file hashes with the stamp
/// written by <c>build/run-tests.ps1</c> after a successful balance run.
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

    /// <summary>The complete identity of inputs that can invalidate balance conclusions.</summary>
    public static string? BalanceIdentity
    {
        get
        {
            var root = FindRepositoryRoot();
            if (root is null)
                return null;

            try
            {
                return string.Join('|',
                    $"equipment={EquipmentStatBudgetCatalog.BalanceVersion}",
                    $"combat={PowerRatingAlgorithm.CombatRulesVersion}",
                    $"reference={EquipmentCombatPacingAnalyzer.ReferenceControlVersion}",
                    $"raid={RaidRules.Version}",
                    $"roster={CanonicalCooperativeRosterCatalog.Version}",
                    $"tower={WorldTowerBalanceAnalyzer.BalanceVersion}",
                    $"abilities={HashFile(root, "LL/src/API/API.LL/Data/combat/abilities.json")}",
                    $"raidBosses={HashFile(root, "LL/src/API/API.LL/Data/raids/raid-bosses.json")}",
                    $"towerFloors={HashFile(root, "LL/src/API/API.LL/Data/world-tower/tower-floors.json")}");
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }
    }

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

        var balanceIdentity = BalanceIdentity;
        if (balanceIdentity is null)
            return null;

        if (!stampContent.Trim().Equals(balanceIdentity, StringComparison.Ordinal))
            return null;

        return "Balance suite skipped locally: the composite balance identity is unchanged since "
            + $"the last recorded local balance run. Delete '{StampRelativePath}' or set "
            + $"{OverrideEnvironmentVariable}=1 to run it anyway.";
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

    private static string HashFile(string root, string relativePath)
    {
        var path = relativePath
            .Split('/')
            .Aggregate(root, Path.Combine);
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }

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
