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
using Services.LL.Interfaces.Combat.Resolution.Idle;
using Services.LL.Combat;

namespace Services.LL.Combat.Layers.Resolution.Idle;

public sealed class IdleCombatResolutionSessionFactory : IIdleCombatResolutionSessionFactory
{
    private readonly IEntityService _entityService;
    private readonly ICombatSetupService _combatSetupService;
    private readonly ICombatEngineExecutor _engineExecutor;
    private readonly ICombatEncounterResultFactory _resultFactory;
    private HostileTemplateCache? _hostileCache;

    public IdleCombatResolutionSessionFactory(
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
        IdleCombatPlan plan,
        CancellationToken cancellationToken)
    {
        var startedAt = IdleCombatTelemetry.Start();
        var playerIds = plan.PlayerEntityIds
            .Distinct()
            .Order()
            .ToArray();

        var hostileIds = plan.Area.Creatures
            .Select(x => x.CreatureId)
            .Distinct()
            .Order()
            .ToArray();

        var reuseHostiles = CanReuseHostiles(plan.Area.Id, hostileIds);
        IReadOnlyDictionary<Guid, Entity> hostileSources;
        IReadOnlyDictionary<Guid, CombatEntity> hostileTemplates;
        Dictionary<Guid, Entity> sourceEntitiesById;
        Dictionary<Guid, CombatEntity> friendlyTemplates;

        if (reuseHostiles)
        {
            var playerEntities = await _entityService.GetEntitiesByIdsForCombatAsync(
                [.. playerIds],
                cancellationToken);
            sourceEntitiesById = playerEntities.ToDictionary(x => x.Id);
            EnsureAllLoaded(playerIds, sourceEntitiesById);

            hostileSources = _hostileCache!.SourceEntitiesById;
            hostileTemplates = _hostileCache.TemplatesBySourceEntityId;
            foreach (var (id, source) in hostileSources)
            {
                sourceEntitiesById[id] = source;
            }

            friendlyTemplates = BuildFriendlyTemplates(playerIds, sourceEntitiesById);
            await _combatSetupService.PrepareEntitiesForCombat([.. friendlyTemplates.Values]);
        }
        else
        {
            var allSourceIds = playerIds
                .Concat(hostileIds)
                .Distinct()
                .ToArray();
            var entities = await _entityService.GetEntitiesByIdsForCombatAsync(
                [.. allSourceIds],
                cancellationToken);
            sourceEntitiesById = entities.ToDictionary(x => x.Id);
            EnsureAllLoaded(allSourceIds, sourceEntitiesById);

            friendlyTemplates = BuildFriendlyTemplates(playerIds, sourceEntitiesById);
            var builtHostileTemplates = BuildHostileTemplates(hostileIds, sourceEntitiesById, plan.Area);
            await _combatSetupService.PrepareEntitiesForCombat(
                [.. friendlyTemplates.Values, .. builtHostileTemplates.Values]);

            hostileSources = hostileIds.ToDictionary(id => id, id => sourceEntitiesById[id]);
            hostileTemplates = builtHostileTemplates;
            _hostileCache = new HostileTemplateCache(
                plan.Area.Id,
                hostileIds,
                hostileSources,
                hostileTemplates);
        }

        var catalog = new IdleCombatTemplateCatalog(
            sourceEntitiesById,
            friendlyTemplates,
            hostileTemplates);

        IdleCombatTelemetry.RecordTemplatePreparation(startedAt, reuseHostiles);

        return new IdleCombatResolutionSession(
            _engineExecutor,
            _resultFactory)
        { Catalog = catalog };
    }

    private bool CanReuseHostiles(string areaId, IReadOnlyList<Guid> hostileIds) =>
        _hostileCache is not null &&
        string.Equals(_hostileCache.AreaId, areaId, StringComparison.Ordinal) &&
        _hostileCache.HostileIds.SequenceEqual(hostileIds);

    private static void EnsureAllLoaded(
        IReadOnlyCollection<Guid> expectedIds,
        IReadOnlyDictionary<Guid, Entity> sourceEntitiesById)
    {
        var missingIds = expectedIds
            .Where(id => !sourceEntitiesById.ContainsKey(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Failed to preload idle combat source entities. Missing: {string.Join(", ", missingIds)}");
        }
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
                    $"Idle combat player source entity '{playerId}' is not a Character.");
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
                    $"Idle combat hostile source entity '{hostileId}' is not a Creature.");
            }

            var template = _combatSetupService
                .CreateCreatureCombatEntities([creature], area)
                .Single();

            templates.Add(hostileId, template);
        }

        return templates;
    }

    private sealed record HostileTemplateCache(
        string AreaId,
        IReadOnlyList<Guid> HostileIds,
        IReadOnlyDictionary<Guid, Entity> SourceEntitiesById,
        IReadOnlyDictionary<Guid, CombatEntity> TemplatesBySourceEntityId);
}
