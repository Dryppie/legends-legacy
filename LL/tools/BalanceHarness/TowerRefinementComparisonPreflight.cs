using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerRefinementPreflightRequest(string ContentRoot, string DefinitionPath,
    string CampaignPath, string LedgerPath, string RegistrySnapshotPath, string DriverReceiptPath,
    int ExpectedReservations, IReadOnlyDictionary<string, string> Pins,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] string? ComparisonVersion = null);

public sealed record TowerRefinementPreflightResult(string Status, bool RunAuthorized, string Version,
    int Reservations, string HistoryHash, string RegistryMode, bool RequiresLiveRegistryRefresh,
    int RequiredFreshValues, int MaximumAttempts, IReadOnlyDictionary<string, string> ControlHashes,
    IReadOnlyDictionary<string, string> ContentHashes, string SettingsHash, ExecutionIdentity Execution,
    string RequestHash);

/// <summary>Read-only binding to sealed inputs. This does not reserve seeds or authorize execution.</summary>
public static class TowerRefinementComparisonPreflight
{
    public static TowerRefinementPreflightResult Check(TowerRefinementPreflightRequest request,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var version = TowerRefinementComparisonModel.ResolveVersion(request.ComparisonVersion);
        var required = new[] { request.DefinitionPath, request.CampaignPath, request.LedgerPath,
            request.RegistrySnapshotPath, request.DriverReceiptPath, Path.Combine(request.ContentRoot, "appsettings.json") };
        VerifyPins(request.Pins, required, token);
        var source = TowerBossDiscovery.Read(request.DefinitionPath);
        var content = TowerCompactBundle.ContentHashes(request.ContentRoot, token);
        VerifyPins(request.Pins, content.Keys.Select(n => Path.Combine(request.ContentRoot, "Data", n)), token);
        Require(HarnessJson.Hash(source.ContentHashes) == HarnessJson.Hash(content), "Changed captured content.");
        var settings = HarnessJson.Hash(TowerBundle.ReadSettings(request.ContentRoot));
        Require(source.SettingsHash == settings, "Changed captured settings.");
        var execution = ExecutionIdentity.Current();
        var campaign = HarnessJson.Read<JsonElement>(request.CampaignPath);
        var captured = campaign.GetProperty("scope").GetProperty("execution").GetProperty("assemblyHashes")
            .EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
        VerifyGameplay(captured, execution.AssemblyHashes);
        var controls = Controls(source);
        var history = History(HarnessJson.Read<JsonElement>(request.LedgerPath), request.ExpectedReservations);
        var registry = HarnessJson.Read<Dictionary<string, string>>(request.RegistrySnapshotPath);
        Require(registry.Count > 0 && registry.All(p => Path.IsPathFullyQualified(p.Key) && IsHash(p.Value)),
            "Invalid sealed registry snapshot.");
        var receipt = HarnessJson.Read<JsonElement>(request.DriverReceiptPath);
        VerifyDriverReceipt(receipt, history.Length, version);
        VerifyPins(request.Pins, required, token);
        return new("BoundAwaitingAuthorization", false, version,
            history.Length, HarnessJson.Hash(history), "sealed-ledger-requires-live-refresh", true,
            TowerRefinementComparisonModel.Stages.Sum(s => s.Count), TowerRefinementComparisonRun.MaximumAttempts,
            controls, content, settings, execution, HarnessJson.Hash(request));
    }

    internal static void VerifyDriverReceipt(JsonElement receipt, int reservations, string version)
    {
        version = TowerRefinementComparisonModel.ResolveVersion(version);
        Require(receipt.GetProperty("status").GetString() == "VerifiedRefinementComparisonDriver"
            && receipt.GetProperty("reservations").GetInt32() == reservations, "Unverified driver/history receipt.");
        if (version != TowerRefinementComparisonModel.Version)
            Require(receipt.TryGetProperty("comparisonVersion", out var comparison) && comparison.GetString() == version
                && receipt.TryGetProperty("refinementPolicy", out var policy)
                && policy.GetString() == TowerRefinementComparisonModel.RefinementPolicy(version), "Driver receipt does not verify the requested refinement policy.");
    }

    internal static void VerifyPins(IReadOnlyDictionary<string, string> pins, IEnumerable<string> required,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Require(pins.Count > 0 && pins.All(p => Path.IsPathFullyQualified(p.Key) && IsHash(p.Value)), "Invalid input pins.");
        var normalized = pins.Select(p => Path.GetFullPath(p.Key)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Require(normalized.Count == pins.Count && required.All(p => normalized.Contains(Path.GetFullPath(p))), "Missing/aliased input pin.");
        foreach (var (path, hash) in pins) {
            token.ThrowIfCancellationRequested();
            Require(HarnessJson.FileHash(path) == hash, "Changed input: " + path);
        }
    }

    internal static void VerifyGameplay(IReadOnlyDictionary<string, string> captured, IReadOnlyDictionary<string, string> current)
    {
        foreach (var name in new[] { "Application", "Common", "Domain", "Services.LL" })
            Require(captured.TryGetValue(name, out var expected) && current.TryGetValue(name, out var actual)
                && IsHash(expected) && actual == expected, "Changed gameplay assembly: " + name);
    }

    internal static int[] History(JsonElement ledger, int expected)
    {
        var history = TowerSearchBenchmark.History(ledger);
        Require(expected > 0 && history.Length == expected, "Reservation count differs from the frozen request.");
        return history;
    }

    internal static Dictionary<string, string> Controls(TowerBossDiscoveryDefinition source)
    {
        Require(source.Contexts.Count == 1 && source.References.Select(r => r.Id).Order(StringComparer.Ordinal)
            .SequenceEqual(TowerRefinementComparisonModel.Controls.Order(StringComparer.Ordinal)), "Changed controls/context.");
        foreach (var r in source.References) {
            var canonical = TowerRefinementComparisonModel.Canonical(r.Scenario);
            var choice = TowerPartySelection.Choice("preflight-context", canonical.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds));
            Require(TowerRefinementComparisonModel.Context(canonical) == TowerRefinementComparisonModel.Context(
                TowerBossDiscovery.Scenario(source, r.Context, choice, [])), "Control/generated context differs.");
        }
        return source.References.ToDictionary(r => r.Id, r => HarnessJson.Hash(TowerRefinementComparisonModel.Canonical(r.Scenario)));
    }

    private static bool IsHash(string value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static void Require(bool condition, string message) => TowerRefinementComparisonModel.Require(condition, message);
}
