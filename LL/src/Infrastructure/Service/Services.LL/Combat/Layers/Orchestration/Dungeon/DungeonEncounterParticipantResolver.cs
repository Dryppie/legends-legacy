using Application.Interfaces.Services.AdminDashboard;
using Services.LL.Interfaces.Combat.Resolution.Dungeon;

namespace Services.LL.Combat.Layers.Orchestration.Dungeon;

public sealed class DungeonEncounterParticipantResolver : IDungeonEncounterParticipantResolver
{
    private static readonly IReadOnlyDictionary<string, string> EncounterKeyAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["skeleton"] = "skeleton_warrior",
            ["poisonous_rat"] = "large_rat",
            ["cave_bat"] = "vampire_bat",
            ["giant_bat"] = "vampire_bat",
            ["necroshade_wraith"] = "specter",
            ["goblin_shaman"] = "hobgoblin",
            ["queens_guard_ant"] = "ant_worker",
            ["ant_queen"] = "fire_ant",
            ["ant_king"] = "fire_ant"
        };

    private readonly ICreatureService _repository;

    public DungeonEncounterParticipantResolver(ICreatureService repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Guid>> ResolveAsync(IReadOnlyList<string> enemyCreatureKeys, CancellationToken cancellationToken)
    {
        var normalizedKeys = enemyCreatureKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(NormalizeEncounterKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var resolved = await _repository.GetCreaturesByKey(normalizedKeys, cancellationToken);
        if (normalizedKeys.Length > 0 && resolved.Count < normalizedKeys.Length)
        {
            throw new InvalidOperationException(
                $"Dungeon encounter could not resolve creature keys: {string.Join(", ", normalizedKeys)}.");
        }

        return resolved;
    }

    private static string NormalizeEncounterKey(string key) =>
        EncounterKeyAliases.GetValueOrDefault(key, key);
}
