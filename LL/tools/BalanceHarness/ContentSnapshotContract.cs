namespace BalanceHarness;

/// <summary>Immutable archive allowlists, independent of the current gameplay catalog.</summary>
internal static class ContentSnapshotContract
{
    public const int CurrentVersion = 2;
    private static readonly IReadOnlyList<string> LegacyFiles = Array.AsReadOnly(new[]
    {
        "combat/abilities.json", "combat/statuses.json", "combat/summons.json",
        "combat/creature-abilities.json", "essences/essences.json",
        "world/creature-essence-loot-tables.json", "world/creatures.json", "world/regions.json",
        "progression/region-combat-balance.json", "equipment/equipment-starters.v1.json",
        "equipment/equipment-named.v1.json", "equipment/equipment-styles.v1.json",
        "equipment/equipment-sets.v1.json", "items/items.json"
    });
    public static IReadOnlyList<string> CurrentFiles { get; } = Array.AsReadOnly(new[]
        { "combat-styles/combat-styles.v1.json" }.Concat(LegacyFiles).Order(StringComparer.Ordinal).ToArray());

    public static IReadOnlyList<string> Resolve(RunManifest manifest)
    {
        if (manifest.SchemaVersion is not (1 or CurrentVersion))
            throw new InvalidDataException("Unsupported content snapshot schema.");
        var actual = manifest.ContentHashes.Keys.Order(StringComparer.Ordinal).ToArray();
        if (actual.SequenceEqual(CurrentFiles)) return CurrentFiles;
        // Before versioning the content addition, schema 1 was emitted with either
        // the original 14 files or the 15-file Combat Styles catalog. Preserve both.
        if (manifest.SchemaVersion == 1 && actual.SequenceEqual(LegacyFiles.Order(StringComparer.Ordinal)))
            return LegacyFiles;
        throw new InvalidDataException("Unexpected content snapshot files.");
    }
}
