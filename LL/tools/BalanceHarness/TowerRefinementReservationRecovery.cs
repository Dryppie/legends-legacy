using System.Text.Json;

namespace BalanceHarness;

internal sealed record TowerRefinementRecoveryRecord(string Version, string Status, string StudyRoot,
    string ManifestPath, string ManifestHash, int Master, string HistoricalHash, int[] Reserved);

/// <summary>An external exclusion receipt for an abandoned binding, never a launch or resume permit.</summary>
internal static class TowerRefinementReservationRecovery
{
    internal const string Version = "tower-refinement-abandoned-reservation-v1";
    private static void Require(bool value, string message) => TowerRefinementComparisonModel.Require(value, message);

    internal static TowerRefinementRecoveryRecord Audit(string study, string manifestPath, string manifestHash,
        CancellationToken ct, Func<string, int, int>? candidate = null)
    {
        ct.ThrowIfCancellationRequested();
        Require(Path.IsPathFullyQualified(study) && Path.IsPathFullyQualified(manifestPath)
            && !Path.GetFullPath(manifestPath).StartsWith(Path.GetFullPath(study) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Recovery manifest must be external to the failed study.");
        void VerifySource()
        {
            ct.ThrowIfCancellationRequested();
            Require(TowerContractJson.Hash(manifestHash) && HarnessJson.FileHash(manifestPath) == manifestHash, "Changed recovery source manifest.");
            var files = HarnessJson.Read<Dictionary<string, string>>(manifestPath);
            var actual = TowerBulkCampaign.Paths(study).ToDictionary(p => Path.GetRelativePath(study, p).Replace('\\', '/'), p => p, StringComparer.Ordinal);
            // Only the complete allocation / failed binding boundary has enough evidence for this receipt.
            string[] required = ["allocation-journal.jsonl", "authorization.json", "binding-failure.json", "binding-started.json",
                "definitions.json", "history-input.json", "request.json", "reservation-intent.json", "seed-ledger.json", "seeds.json"];
            Require(files.Keys.Order().SequenceEqual(required.Order()) && actual.Keys.Order().SequenceEqual(files.Keys.Order()),
                "Recovery requires an exact abandoned binding inventory, with no launch or completed binding.");
            foreach (var (name, hash) in files)
            {
                ct.ThrowIfCancellationRequested();
                Require(TowerContractJson.Hash(hash) && HarnessJson.FileHash(actual[name]) == hash, "Changed failed study artifact.");
            }
        }
        VerifySource();
        string P(string name) => Path.Combine(study, name);
        var failure = HarnessJson.Read<JsonElement>(P("binding-failure.json"));
        Require(failure.GetProperty("noRetry").GetBoolean(), "Recovery requires a permanent no-retry failure.");
        var request = HarnessJson.Read<TowerRefinementLaunchRequest>(P("request.json"));
        var permit = HarnessJson.Read<TowerRefinementLaunchAuthorization>(P("authorization.json"));
        Require(string.Equals(Path.GetFullPath(request.StudyRoot), Path.GetFullPath(study), StringComparison.OrdinalIgnoreCase)
            && permit.RequestHash == HarnessJson.Hash(request) && permit.FreshValues == 45 && permit.Retries == 0,
            "Changed failed binding authorization.");
        var seeds = HarnessJson.Read<TowerRefinementComparisonSeeds>(P("seeds.json"));
        var recorded = new Dictionary<(string Stage, int Ordinal), int>();
        using (var lines = File.ReadLines(P("allocation-journal.jsonl")).GetEnumerator())
        {
            while (lines.MoveNext())
            {
                ct.ThrowIfCancellationRequested();
                var start = JsonSerializer.Deserialize<TowerReservationEvent>(lines.Current, HarnessJson.Options)!;
                Require(lines.MoveNext(), "Unresolved allocation start cannot be recovered.");
                var result = JsonSerializer.Deserialize<TowerReservationEvent>(lines.Current, HarnessJson.Options)!;
                Require(start == new TowerReservationEvent("Start", result.Stage, result.Ordinal)
                    && result.Kind == "Candidate" && result.Value.HasValue
                    && recorded.TryAdd((result.Stage, result.Ordinal), result.Value.Value), "Invalid recorded allocation pair.");
            }
        }
        int RecordedCandidate(string stage, int ordinal)
        {
            // Never derive a value whose completed candidate row is absent, including corrupt/truncated transcripts.
            Require(recorded.TryGetValue((stage, ordinal), out var value), "Missing recorded candidate.");
            Require(value == (candidate is null ? TowerRefinementReservation.Candidate(request.Master, stage, ordinal) : candidate(stage, ordinal)),
                "Changed recorded candidate value.");
            return value;
        }
        TowerRefinementReservation.Verify(study, seeds, request.Master, ct, RecordedCandidate, false);
        VerifySource(); ct.ThrowIfCancellationRequested();
        return new(Version, "AbandonedPermanentlyReserved", Path.GetFullPath(study), Path.GetFullPath(manifestPath),
            manifestHash, request.Master, HarnessJson.Hash(seeds.Historical),
            seeds.Generation.Concat(seeds.Discovery).Concat(seeds.Selection).Concat(seeds.Confirmation).ToArray());
    }

    internal static int[] ReadPending(string pendingPath, string recoveryPath, CancellationToken ct,
        Func<string, int, int>? candidate = null)
    {
        ct.ThrowIfCancellationRequested();
        var hash = HarnessJson.FileHash(recoveryPath);
        var record = HarnessJson.Read<TowerRefinementRecoveryRecord>(recoveryPath);
        Require(string.Equals(Path.GetFullPath(pendingPath), Path.Combine(record.StudyRoot, "history-input.json"), StringComparison.OrdinalIgnoreCase),
            "Recovery receipt belongs to another Pending file.");
        var verified = Audit(record.StudyRoot, record.ManifestPath, record.ManifestHash, ct, candidate);
        Require(HarnessJson.Hash(record) == HarnessJson.Hash(verified) && HarnessJson.FileHash(recoveryPath) == hash,
            "Changed recovery receipt.");
        return verified.Reserved;
    }
}
