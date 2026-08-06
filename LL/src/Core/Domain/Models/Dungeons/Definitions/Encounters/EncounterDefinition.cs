namespace Domain.Models.Dungeons.Definitions.Encounters;

public sealed class EncounterDefinition
{
    public string CreatureId { get; init; } = string.Empty;
    public EncounterKind Kind { get; init; }
}

public static class DungeonEncounterIdentity
{
    private static readonly IReadOnlyDictionary<string, string> CreatureKeyAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["necroshade_wraith"] = "specter",
            ["queens_guard_ant"] = "ant_worker",
            ["ant_queen"] = "fire_ant",
            ["ant_king"] = "fire_ant"
        };

    public static string NormalizeCreatureKey(string key)
    {
        var normalized = key.Trim();
        const string monsterPrefix = "monster.";
        if (normalized.StartsWith(monsterPrefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[monsterPrefix.Length..];

        return CreatureKeyAliases.GetValueOrDefault(normalized, normalized);
    }

    public static string ToMonsterDefinitionId(string key) =>
        $"monster.{NormalizeCreatureKey(key)}";
}
