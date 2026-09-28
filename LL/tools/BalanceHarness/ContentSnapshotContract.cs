namespace BalanceHarness;

/// <summary>Immutable archive allowlists, independent of the current gameplay catalog.</summary>
internal static class ContentSnapshotContract
{
    public const int CurrentVersion = 4;
    private static readonly IReadOnlyList<string> LegacyFiles = Array.AsReadOnly(new[]
    {
        "combat/abilities.json", "combat/statuses.json", "combat/summons.json",
        "combat/creature-abilities.json", "essences/essences.json",
        "world/creature-essence-loot-tables.json", "world/creatures.json", "world/regions.json",
        "progression/region-combat-balance.json", "equipment/equipment-starters.v1.json",
        "equipment/equipment-named.v1.json", "equipment/equipment-styles.v1.json",
        "equipment/equipment-sets.v1.json", "items/items.json"
    });
    internal static readonly IReadOnlyList<string> VersionTwoFiles = Array.AsReadOnly(new[]
        { "combat-styles/combat-styles.v1.json" }.Concat(LegacyFiles).Order(StringComparer.Ordinal).ToArray());
    internal static readonly IReadOnlyList<string> VersionThreeFiles = Array.AsReadOnly(new[]
        { "equipment/equipment-starters.legacy-v1.json", "equipment/equipment-styles.legacy-v1.json", "equipment/equipment-sets.legacy-v1.json" }
        .Concat(VersionTwoFiles).Order(StringComparer.Ordinal).ToArray());
    public static IReadOnlyList<string> CurrentFiles { get; } = Array.AsReadOnly(new[]
    {
        "equipment/equipment-releases.json",
        "equipment/equipment-starters.v3.json", "equipment/equipment-styles.v3.json",
        "equipment/equipment-sets.v3.json", "equipment/equipment-named.v3.json",
        "equipment/equipment-starters.v4.json", "equipment/equipment-styles.v4.json",
        "equipment/equipment-sets.v4.json", "equipment/equipment-named.v4.json",
        "combat/ability-balance.healing-v1.json"
    }.Concat(VersionThreeFiles).Order(StringComparer.Ordinal).ToArray());

    public static bool IsKnownTowerFiles(IEnumerable<string> files)
    {
        var actual = files.Order(StringComparer.Ordinal).ToArray();
        return new[] { LegacyFiles, VersionTwoFiles, VersionThreeFiles, CurrentFiles }.Any(list =>
            actual.SequenceEqual(list.Append(TowerBattleRunner.FloorFile).Order(StringComparer.Ordinal)));
    }

    public static IReadOnlyList<string> Resolve(RunManifest manifest)
    {
        if (manifest.SchemaVersion is not (1 or 2 or 3 or CurrentVersion))
            throw new InvalidDataException("Unsupported content snapshot schema.");
        var actual = manifest.ContentHashes.Keys.Order(StringComparer.Ordinal).ToArray();
        if (manifest.SchemaVersion == CurrentVersion && actual.SequenceEqual(CurrentFiles)) return CurrentFiles;
        if (manifest.SchemaVersion == 3 && actual.SequenceEqual(VersionThreeFiles)) return VersionThreeFiles;
        if (manifest.SchemaVersion is 1 or 2 && actual.SequenceEqual(VersionTwoFiles)) return VersionTwoFiles;
        // Before versioning the content addition, schema 1 was emitted with either
        // the original 14 files or the 15-file Combat Styles catalog. Preserve both.
        if (manifest.SchemaVersion == 1 && actual.SequenceEqual(LegacyFiles.Order(StringComparer.Ordinal)))
            return LegacyFiles;
        throw new InvalidDataException("Unexpected content snapshot files.");
    }
}
