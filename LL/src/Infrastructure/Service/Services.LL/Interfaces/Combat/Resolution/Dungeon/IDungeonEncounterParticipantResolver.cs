namespace Services.LL.Interfaces.Combat.Resolution.Dungeon;

public interface IDungeonEncounterParticipantResolver
{
    Task<IReadOnlyList<Guid>> ResolveAsync(
        IReadOnlyList<string> enemyCreatureKeys,
        CancellationToken cancellationToken);
}
