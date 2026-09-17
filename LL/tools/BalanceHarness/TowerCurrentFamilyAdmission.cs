using System.Text.Json;

namespace BalanceHarness;

public sealed record TowerCurrentAdmissionCell(string InputKey, IReadOnlyList<IReadOnlyList<string>> EssencesBySlot,
    IReadOnlyList<int> EntryOrdinals, IReadOnlyList<string> RequiredControlIds, IReadOnlyList<string> AnchorReasons,
    string NativeAdmission, string? NativeParticipantHash);
public sealed record TowerCurrentAdmissionEntry(int Ordinal, JsonElement Source, string SeedFreeHistoricalScenarioHash,
    string? HistoricalScenarioId, string? HistoricalStartsAt, string? InputKey, string Classification,
    IReadOnlyList<string> Reasons, bool CanonicalOrderChangesHistoricalInput, string? AnchorReason);
public sealed record TowerCurrentAdmissionInventory(TowerScenario Template, IReadOnlyList<TowerCurrentAdmissionCell> Cells,
    IReadOnlyList<TowerCurrentAdmissionEntry> Entries, IReadOnlyList<string> RequiredControls);
public sealed record TowerCurrentAdmissionRow(int Ordinal, string InputKey, string Status, string? Error,
    string? ScenarioHash, TowerRetainedAuditIdentity? Identity, string? InputHash, string? ParticipantsHash,
    JsonElement? Participants, string? AliasOf, IReadOnlyList<int> EntryOrdinals,
    IReadOnlyList<string> AnchorReasons, IReadOnlyList<string> RequiredControlIds);
public sealed record TowerCurrentAdmissionResult(string Version, string Status, int Cells, int Materialized,
    int Invalid, int DistinctNativeCells, int AliasCells, int SourceOccurrences, int IncompatibleOccurrences,
    int ForcedInputCells, int ForcedNativeCells, IReadOnlyList<string> MatchedControls,
    bool ReadyForFamilyFreeze, int Fights = 0, int NewSeeds = 0);

/// <summary>Seed-free projected-input admission. No search, allocation, sampling or balance decision.</summary>
public static partial class TowerCurrentFamilyAdmission
{
    public const string Version = "tower-current-family-admission-v1";
    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message)
    { if (!condition) throw new InvalidDataException(message); }

    internal static void ValidateInventory(TowerCurrentAdmissionInventory inventory)
    {
        var s = inventory.Template;
        Require(s is not null && s.SchemaVersion == 1 && TowerBenchmark.SafeId(s.Id) && s.FloorNumber == 5
            && s.StartsAt == new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero)
            && s.PreparationState == "uncleared-no-contributions" && s.Seeds is { Count: 0 }
            && s.Assumptions is not null && s.Assumptions.All(a => a is not null), "Changed seed-free cohort.");
        TowerBossDiscovery.ValidateEquipment(s!.Party, TowerPartyProgression.Budget(5) with { PriorityFloor = 5 }, 10);
        var neutral = Enumerable.Range(1, 5).Select(i => $"neutral-identity-slot-{i}").ToArray();
        Require(s.Party.All(p => p.Build.IdentityEssenceIds is not null
            && p.Build.IdentityEssenceIds.SequenceEqual(neutral)), "Non-neutral identity requires a separate context.");
        Require(inventory.Cells is { Count: > 0 and <= 46077 } && inventory.Entries is { Count: > 0 and <= 51624 }
            && inventory.RequiredControls is { Count: > 0 }
            && inventory.RequiredControls.All(TowerContractJson.Hash)
            && inventory.RequiredControls.Distinct().Count() == inventory.RequiredControls.Count, "Invalid admission family.");
        var owners = new Dictionary<int, string>(); var keys = new HashSet<string>(StringComparer.Ordinal);
        var controls = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cell in inventory.Cells!)
        {
            Require(cell is not null && TowerContractJson.Hash(cell.InputKey) && keys.Add(cell.InputKey)
                && cell.EntryOrdinals is { Count: > 0 } && cell.AnchorReasons is not null && cell.RequiredControlIds is not null
                && cell.NativeAdmission == "Pending" && cell.NativeParticipantHash is null,
                "Duplicate, empty or already-admitted projection.");
            Require(cell!.AnchorReasons.All(r => !string.IsNullOrWhiteSpace(r))
                && cell.AnchorReasons.Distinct().Count() == cell.AnchorReasons.Count
                && cell.EntryOrdinals.SequenceEqual(cell.EntryOrdinals.Distinct().Order()), "Invalid projection provenance.");
            foreach (var ordinal in cell.EntryOrdinals)
                Require(ordinal >= 0 && ordinal < inventory.Entries!.Count && owners.TryAdd(ordinal, cell.InputKey),
                    "Missing, duplicate or out-of-range source occurrence.");
            foreach (var id in cell.RequiredControlIds)
                Require(inventory.RequiredControls!.Contains(id) && controls.Add(id) && cell.AnchorReasons.Count > 0,
                    "Missing, duplicate or unforced required control.");
        }
        Require(controls.SetEquals(inventory.RequiredControls!), "Required controls omitted.");
        for (var i = 0; i < inventory.Entries!.Count; i++)
        {
            var entry = inventory.Entries[i];
            Require(entry is not null && entry.Ordinal == i && TowerContractJson.Hash(entry.SeedFreeHistoricalScenarioHash)
                && entry.Source.ValueKind == JsonValueKind.Object && entry.Source.EnumerateObject().Any()
                && entry.Reasons is not null, "Invalid or reordered origin ledger.");
            if (entry!.Classification == "StaticCompatibleProjection")
                Require(entry.Reasons.Count == 0 && entry.InputKey is not null
                    && owners.TryGetValue(i, out var owner) && owner == entry.InputKey, "Unmapped compatible origin.");
            else
                Require(entry.Classification == "Incompatible" && entry.InputKey is null && entry.Reasons.Count > 0
                    && !owners.ContainsKey(i), "Incompatible origin was discarded or admitted.");
        }
        // Anchor reasons must originate in the preserved ledger; a consumer cannot demote a known control.
        foreach (var cell in inventory.Cells)
        {
            var reasons = cell.EntryOrdinals.Select(i => inventory.Entries[i].AnchorReason).Where(r => r is not null).ToHashSet();
            Require(reasons.SetEquals(cell.AnchorReasons), "Changed mandatory anchor reasons.");
        }
    }

    internal static TowerScenario Scenario(TowerScenario template, TowerCurrentAdmissionCell cell)
    {
        Require(cell.EssencesBySlot is { Count: 10 } && cell.EssencesBySlot.All(ids => ids is { Count: 5 }
            && ids.All(id => !string.IsNullOrWhiteSpace(id)) && ids.Distinct(StringComparer.Ordinal).Count() == 5
            && ids.SequenceEqual(ids.Order(StringComparer.Ordinal))), "Noncanonical, duplicate or incomplete Essence vector.");
        var partyId = HarnessJson.Hash(cell.EssencesBySlot.Select((ids,i)=>(ids,i))
            .ToDictionary(x=>x.i+1,x=>x.ids));
        Require(cell.RequiredControlIds.All(id=>id==partyId), "Required control is attached to a different party.");
        return template with { Seeds = [], Party = template.Party.Select((p, i) => p with {
            Build = p.Build with { EssenceIds = cell.EssencesBySlot[i].ToArray() } }).ToArray() };
    }

    private sealed class Ledger(TowerCurrentAdmissionInventory inventory)
    {
        private int count, invalid;
        private readonly Dictionary<string, TowerCurrentAdmissionRow> identities = new(StringComparer.Ordinal);
        private readonly HashSet<string> matched = new(StringComparer.Ordinal), forced = new(StringComparer.Ordinal);

        public void Add(TowerCurrentAdmissionRow row)
        {
            Require(count < inventory.Cells.Count, "Extra admission row.");
            var cell = inventory.Cells[count];
            Require(row.Ordinal == count++ && row.InputKey == cell.InputKey
                && row.EntryOrdinals.SequenceEqual(cell.EntryOrdinals) && row.AnchorReasons.SequenceEqual(cell.AnchorReasons)
                && row.RequiredControlIds.SequenceEqual(cell.RequiredControlIds), "Changed row order, origin or control.");
            if (row.Status == "Invalid")
            {
                Require(!string.IsNullOrWhiteSpace(row.Error) && row.Participants is null && row.ParticipantsHash is null
                    && row.InputHash is null && row.AliasOf is null, "Invalid row masquerades as prepared evidence.");
                invalid++; return;
            }
            var scenario = Scenario(inventory.Template, cell);
            var identity = TowerConfirmationContext.Identity(scenario);
            Require(row.Status == "Materialized" && row.Error is null && row.ScenarioHash == HarnessJson.Hash(scenario)
                && row.Identity == identity && TowerContractJson.Hash(row.InputHash!) && row.Participants is not null
                && row.ParticipantsHash == HarnessJson.Hash(row.Participants.Value), "Changed native identity or participants.");
            TowerRetainedFamilyAudit.ParticipantShape(row.Participants!.Value, 10);
            if (identities.TryGetValue(identity.CellHash, out var prior))
                Require(row.AliasOf == prior.InputKey && row.ParticipantsHash == prior.ParticipantsHash
                    && row.InputHash == prior.InputHash, "Native alias has different input/participants.");
            else
            {
                Require(row.AliasOf is null, "Unknown native alias target.");
                identities.Add(identity.CellHash, row);
            }
            if (cell.AnchorReasons.Count > 0) forced.Add(identity.CellHash);
            matched.UnionWith(cell.RequiredControlIds);
        }

        public TowerCurrentAdmissionResult Finish()
        {
            Require(count == inventory.Cells.Count, "Incomplete admission rows.");
            var incompatible = inventory.Entries.Count(e => e.Classification == "Incompatible");
            var complete = invalid == 0 && matched.SetEquals(inventory.RequiredControls);
            return new(Version, !complete ? "AdmissionIssues" : incompatible > 0 ? "AdmittedWithContextExceptions" : "Admitted",
                count, count-invalid, invalid, identities.Count, count-invalid-identities.Count, inventory.Entries.Count,
                incompatible, inventory.Cells.Count(c => c.AnchorReasons.Count > 0), forced.Count, matched.Order().ToArray(),
                complete && incompatible == 0);
        }
    }

    internal static async Task<TowerCurrentAdmissionResult> Scan(TowerCurrentAdmissionInventory inventory, Stream output,
        Func<TowerScenario, CancellationToken, Task<(string InputHash, JsonElement Participants)>> prepare,
        Action checkpoint, CancellationToken token = default, Action<string>? progress = null)
    {
        ValidateInventory(inventory);
        using var guard = new TowerPerformanceTrace(_ => throw new InvalidOperationException("Admission cannot execute combat.")).Activate();
        var ledger = new Ledger(inventory);
        var aliases = new Dictionary<string, TowerCurrentAdmissionRow>(StringComparer.Ordinal);
        for (var i = 0; i < inventory.Cells.Count; i++)
        {
            token.ThrowIfCancellationRequested(); checkpoint();
            var cell = inventory.Cells[i]; TowerCurrentAdmissionRow row;
            string? scenarioHash = null; TowerRetainedAuditIdentity? identity = null;
            try
            {
                var scenario = Scenario(inventory.Template, cell); scenarioHash = HarnessJson.Hash(scenario);
                identity = TowerConfirmationContext.Identity(scenario);
                var prepared = await prepare(scenario, token);
                token.ThrowIfCancellationRequested(); checkpoint();
                Require(TowerContractJson.Hash(prepared.InputHash), "Invalid prepared input hash.");
                TowerRetainedFamilyAudit.ParticipantShape(prepared.Participants, 10);
                var participantsHash = HarnessJson.Hash(prepared.Participants);
                aliases.TryGetValue(identity.CellHash, out var previous);
                Require(previous is null || (previous.InputHash == prepared.InputHash && previous.ParticipantsHash == participantsHash),
                    "Native alias conflicts with earlier preparation.");
                row = new(i, cell.InputKey, "Materialized", null, scenarioHash, identity, prepared.InputHash,
                    participantsHash, prepared.Participants, previous?.InputKey, cell.EntryOrdinals, cell.AnchorReasons, cell.RequiredControlIds);
                aliases.TryAdd(identity.CellHash, row);
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or KeyNotFoundException or JsonException)
            {
                row = new(i, cell.InputKey, "Invalid", e.GetType().Name+": "+e.Message, scenarioHash, identity,
                    null, null, null, null, cell.EntryOrdinals, cell.AnchorReasons, cell.RequiredControlIds);
            }
            ledger.Add(row);
            await output.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(row, TowerRetainedFamilyAudit.Strict), token);
            await output.WriteAsync(new byte[] { 10 }, token);
            if ((i+1)%256 == 0) { await output.FlushAsync(token); checkpoint(); progress?.Invoke($"Admission: {i+1}/{inventory.Cells.Count}"); }
        }
        await output.FlushAsync(token); checkpoint(); token.ThrowIfCancellationRequested();
        return ledger.Finish();
    }

    internal static async Task<TowerCurrentAdmissionResult> Audit(TowerCurrentAdmissionInventory inventory, Stream rows,
        Action checkpoint, CancellationToken token = default)
    {
        ValidateInventory(inventory); var ledger = new Ledger(inventory);
        await foreach (var row in ReadLines<TowerCurrentAdmissionRow>(rows, token)) { checkpoint(); ledger.Add(row); }
        checkpoint(); token.ThrowIfCancellationRequested(); return ledger.Finish();
    }

    internal static async IAsyncEnumerable<T> ReadLines<T>(Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        while (await reader.ReadLineAsync(token) is { } line)
        {
            Require(line.Length is > 0 and <= 1048576, "Empty or oversized admission row.");
            yield return JsonSerializer.Deserialize<T>(line, TowerRetainedFamilyAudit.Strict)
                ?? throw new InvalidDataException("Null admission row.");
        }
    }
}
