using Application.Interfaces.Services.AdminDashboard;
using Services.LL.Interfaces.Combat.Resolution.Dungeon;

namespace Services.LL.Combat.Layers.Orchestration.Dungeon;

public sealed class DungeonEncounterParticipantResolver : IDungeonEncounterParticipantResolver
{
    private readonly ICreatureService _repository;

    public DungeonEncounterParticipantResolver(ICreatureService repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Guid>> ResolveAsync(IReadOnlyList<string> enemyCreatureKeys, CancellationToken cancellationToken)
    {
        var normalizedKeys = enemyCreatureKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(Domain.Models.Dungeons.Definitions.Encounters.DungeonEncounterIdentity.NormalizeCreatureKey)
            .ToArray();

        var resolved = await _repository.GetCreaturesByKey(normalizedKeys, cancellationToken);
        if (resolved.Count != normalizedKeys.Length)
        {
            throw new InvalidOperationException(
                $"Dungeon encounter could not resolve creature keys: {string.Join(", ", normalizedKeys)}.");
        }

        return resolved;
    }
}
