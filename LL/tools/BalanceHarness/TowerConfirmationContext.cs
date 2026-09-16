namespace BalanceHarness;

public sealed record TowerConfirmationCell(string InventoryKey, string LegacyCellHash, string ContextHash,
    string RecipeHash, string CellHash, string ParticipantsHash, IReadOnlyList<string> AnchorReasons);
public sealed record TowerConfirmationContextSummary(int Entries, int Contexts, int Anchors, int FirstStageCells,
    IReadOnlyDictionary<string, int> ContextCounts, IReadOnlyDictionary<string, int> AnchorCounts);

/// <summary>
/// Versioned confirmation identity. Original audit/input hashes retain their timestamp spelling.
/// Use only after the producing runtime's UTC-equivalence evidence has been verified and bound.
/// </summary>
public static class TowerConfirmationContext
{
    public const string Policy = "tower-confirmation-context-utc-v1";

    public static TowerRetainedAuditIdentity Identity(TowerScenario scenario)
    {
        var legacy = TowerRetainedFamilyAudit.Identity(scenario);
        var context = HarnessJson.Hash(new { Policy, scenario.SchemaVersion, scenario.Id, scenario.FloorNumber,
            StartsAtUtc = scenario.StartsAt.ToUniversalTime(), scenario.PreparationState, Equipment = legacy.EquipmentHash });
        return new(legacy.RecipeHash, context, HarnessJson.Hash(new { Context = context, Recipe = legacy.RecipeHash }), legacy.EquipmentHash);
    }

    // This family has no aliases. Never silently discard a recipe or resolve a collision by outcome.
    public static TowerConfirmationContextSummary Validate(IReadOnlyList<TowerConfirmationCell> cells,
        int expectedEntries, IReadOnlyCollection<string> requiredAnchorReasons, CancellationToken token = default)
    {
        if (expectedEntries < 1 || expectedEntries > 43879 || cells.Count != expectedEntries
            || requiredAnchorReasons.Count == 0 || requiredAnchorReasons.Count != requiredAnchorReasons.Distinct(StringComparer.Ordinal).Count())
            throw new InvalidDataException("Incomplete family or invalid fixed anchor requirements.");
        var inventory = new HashSet<string>(StringComparer.Ordinal); var legacy = new HashSet<string>(StringComparer.Ordinal);
        var identities = new HashSet<string>(StringComparer.Ordinal); var reasons = new HashSet<string>(StringComparer.Ordinal);
        var required = requiredAnchorReasons.ToHashSet(StringComparer.Ordinal);
        var contexts = new Dictionary<string, int>(StringComparer.Ordinal); var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
        var anchorCount = 0;
        foreach (var cell in cells)
        {
            token.ThrowIfCancellationRequested();
            if (new[] { cell.InventoryKey, cell.LegacyCellHash, cell.ContextHash, cell.RecipeHash, cell.CellHash, cell.ParticipantsHash }.Any(h => !TowerContractJson.Hash(h))
                || !inventory.Add(cell.InventoryKey) || !legacy.Add(cell.LegacyCellHash) || !identities.Add(cell.CellHash)
                || cell.CellHash != HarnessJson.Hash(new { Context = cell.ContextHash, Recipe = cell.RecipeHash }))
                throw new InvalidDataException("Invalid identity, missing entry or confirmation collision; retain the evidence.");
            contexts[cell.ContextHash] = contexts.GetValueOrDefault(cell.ContextHash) + 1;
            if (cell.AnchorReasons.Count == 0) continue;
            foreach (var reason in cell.AnchorReasons)
                if (!required.Contains(reason) || !reasons.Add(reason)) throw new InvalidDataException("Unexpected, duplicate or ambiguous anchor origin.");
            anchors[cell.ContextHash] = anchors.GetValueOrDefault(cell.ContextHash) + 1; anchorCount++;
        }
        if (!reasons.SetEquals(required) || contexts.Keys.Any(c => anchors.GetValueOrDefault(c) == 0))
            throw new InvalidDataException("Every fixed anchor and every required context must be covered.");
        if (anchorCount > 4096) throw new InvalidDataException("Fixed anchors exceed the new design's second-stage capacity.");
        return new(cells.Count, contexts.Count, anchorCount, cells.Count - anchorCount, contexts, anchors);
    }
}
