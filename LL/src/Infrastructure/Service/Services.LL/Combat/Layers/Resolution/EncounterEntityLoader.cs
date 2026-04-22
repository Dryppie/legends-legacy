using Application.Interfaces.Services.LL.Entities;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class EncounterEntityLoader : IEncounterEntityLoader
{
    private readonly IEntityService _entityService;

    public EncounterEntityLoader(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public async Task<LoadedEncounterEntities> LoadAsync(
        CombatEncounterPlan encounterPlan,
        CancellationToken cancellationToken)
    {
        var sourceEntityIds = encounterPlan.Participants
            .Select(x => x.SourceEntityId)
            .Distinct()
            .ToList();

        var entities = await _entityService.GetEntitiesByIdsForCombatAsync(
            sourceEntityIds,
            cancellationToken);

        var sourceEntitiesById = entities.ToDictionary(x => x.Id);

        var missingIds = sourceEntityIds
            .Where(id => !sourceEntitiesById.ContainsKey(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Failed to load all encounter source entities. Missing: {string.Join(", ", missingIds)}");
        }

        return new LoadedEncounterEntities(sourceEntitiesById);
    }
}