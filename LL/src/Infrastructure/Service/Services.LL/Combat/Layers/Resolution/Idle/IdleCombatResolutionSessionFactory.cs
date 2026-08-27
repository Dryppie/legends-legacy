using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Combat;
using Domain.Models.Entities;
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
    private readonly ICombatPreparationPipeline _combatPreparation;
    private readonly ICombatEngineExecutor _engineExecutor;
    private readonly ICombatEncounterResultFactory _resultFactory;
    private HostileTemplateCache? _hostileCache;

    public IdleCombatResolutionSessionFactory(
        IEntityService entityService,
        ICombatPreparationPipeline combatPreparation,
        ICombatEngineExecutor engineExecutor,
        ICombatEncounterResultFactory resultFactory)
    {
        _entityService = entityService;
        _combatPreparation = combatPreparation;
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

            var friendly = await _combatPreparation.PrepareAsync(
                CombatContentType.Idle,
                CreateFriendlyRequests(playerIds, sourceEntitiesById),
                cancellationToken);
            friendlyTemplates = friendly.ToDictionary(x => x.Slot.SourceEntityId, x => x.Combatant);
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

            var prepared = await _combatPreparation.PrepareAsync(
                CombatContentType.Idle,
                [
                    .. CreateFriendlyRequests(playerIds, sourceEntitiesById),
                    .. hostileIds.Select(hostileId => new CombatantPreparationRequest(
                        new CombatParticipantSlot(
                            $"idle-template-hostile-{hostileId:N}",
                            hostileId,
                            CombatSide.Hostile),
                        new LiveCombatantPreparationSource(sourceEntitiesById[hostileId], plan.Area)))
                ],
                cancellationToken);
            friendlyTemplates = prepared
                .Where(x => x.Slot.Side == CombatSide.Friendly)
                .ToDictionary(x => x.Slot.SourceEntityId, x => x.Combatant);
            var builtHostileTemplates = prepared
                .Where(x => x.Slot.Side == CombatSide.Hostile)
                .ToDictionary(x => x.Slot.SourceEntityId, x => x.Combatant);

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

    private static CombatantPreparationRequest[] CreateFriendlyRequests(
        IReadOnlyCollection<Guid> playerIds,
        Dictionary<Guid, Entity> sourceEntitiesById)
        => playerIds.Select(playerId => new CombatantPreparationRequest(
            new CombatParticipantSlot(
                $"idle-template-friendly-{playerId:N}",
                playerId,
                CombatSide.Friendly),
            new LiveCombatantPreparationSource(sourceEntitiesById[playerId]))).ToArray();

    private sealed record HostileTemplateCache(
        string AreaId,
        IReadOnlyList<Guid> HostileIds,
        IReadOnlyDictionary<Guid, Entity> SourceEntitiesById,
        IReadOnlyDictionary<Guid, CombatEntity> TemplatesBySourceEntityId);
}
