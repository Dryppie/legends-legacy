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
        return await _repository.GetCreaturesByKey(enemyCreatureKeys, cancellationToken);
    }
}
