using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.Combat.Resolution.Dungeon;

namespace Services.LL.Combat.Layers.Resolution.Dungeon;

public sealed class DungeonCombatResolutionSessionFactory : IDungeonCombatResolutionSessionFactory
{
    private readonly IEntityService _entityService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly ICombatEngineExecutor _engineExecutor;
    private readonly ICombatEncounterResultFactory _resultFactory;

    public DungeonCombatResolutionSessionFactory(
        IEntityService entityService,
        ICombatSetupService combatSetupService,
        ICombatEngineExecutor engineExecutor,
        ICombatEncounterResultFactory resultFactory)
    {
        _entityService = entityService;
        _combatSetupService = combatSetupService;
        _engineExecutor = engineExecutor;
        _resultFactory = resultFactory;
    }

    public async Task<ICombatResolutionSession> CreateAsync(
        DungeonCombatPlan plan,
        CancellationToken cancellationToken)
    {
        var playerIds = plan.PlayerEntityIds
            .Distinct()
            .ToArray();

        var hostileIds = plan.EnemySourceEntityIds
            .ToArray();

        var allSourceIds = playerIds
            .Concat(hostileIds)
            .Distinct()
            .ToArray();

        var entities = await _entityService.GetEntitiesByIdsForCombatAsync(
            [.. allSourceIds],
            cancellationToken);

        var sourceEntitiesById = entities.ToDictionary(x => x.Id);

        var missingIds = allSourceIds
            .Where(id => !sourceEntitiesById.ContainsKey(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Failed to preload idle combat source entities. Missing: {string.Join(", ", missingIds)}");
        }

        var friendlyTemplates = BuildFriendlyTemplates(playerIds, sourceEntitiesById);
        var hostileTemplates = BuildHostileTemplates(hostileIds, sourceEntitiesById, new Area() { DifficultyTier = 1 });

        await _combatSetupService.PrepareEntitiesForCombat(
            [.. friendlyTemplates.Values, .. hostileTemplates.Values]);

        var catalog = new DungeonCombatTemplateCatalog(
            sourceEntitiesById,
            friendlyTemplates,
            hostileTemplates);

        return new DungeonCombatResolutionSession(
            _engineExecutor,
            _resultFactory)
        { Catalog = catalog };
    }

    private Dictionary<Guid, CombatEntity> BuildFriendlyTemplates(
        IReadOnlyCollection<Guid> playerIds,
        Dictionary<Guid, Entity> sourceEntitiesById)
    {
        var templates = new Dictionary<Guid, CombatEntity>();

        foreach (var playerId in playerIds)
        {
            if (sourceEntitiesById[playerId] is not Character character)
            {
                throw new InvalidOperationException(
                    $"Dungeon combat player source entity '{playerId}' is not a Character.");
            }

            var template = _combatSetupService
                .CreatePlayerCombatEntities([character])
                .Single();

            templates.Add(playerId, template);
        }

        return templates;
    }

    private Dictionary<Guid, CombatEntity> BuildHostileTemplates(
        IReadOnlyCollection<Guid> hostileIds,
        Dictionary<Guid, Entity> sourceEntitiesById,
        Area area)
    {
        var templates = new Dictionary<Guid, CombatEntity>();

        foreach (var hostileId in hostileIds)
        {
            if (sourceEntitiesById[hostileId] is not Creature creature)
            {
                throw new InvalidOperationException(
                    $"Dungeon combat hostile source entity '{hostileId}' is not a Creature.");
            }

            var template = _combatSetupService
                .CreateCreatureCombatEntities([creature], area)
                .Single();

            templates.Add(hostileId, template);
        }

        return templates;
    }
}