using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities.Creatures;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatRewardFactBuilder : IIdleCombatRewardFactBuilder
{
    private readonly IEntityService _entityService;

    public IdleCombatRewardFactBuilder(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public async Task<IdleCombatRewardFacts> BuildAsync(
        CombatOutcomeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.OrchestrationRequest is not IdleCombatOrchestrationRequest idleRequest)
        {
            throw new ArgumentException(
                $"Expected {nameof(IdleCombatOrchestrationRequest)} but got {request.OrchestrationRequest.GetType().Name}.",
                nameof(request));
        }

        var hostileSourceIds = request.OrchestrationResult.Encounters
            .SelectMany(x => x.Plan.HostileParticipants)
            .Select(x => x.SourceEntityId)
            .Distinct()
            .ToList();

        var hostileCreaturesById = new Dictionary<Guid, Creature>();

        if (hostileSourceIds.Count > 0)
        {
            var hostileEntities = await _entityService.GetEntitiesByIdsForCombatAsync(
                hostileSourceIds,
                cancellationToken);

            hostileCreaturesById = hostileEntities
                .OfType<Creature>()
                .ToDictionary(x => x.Id);
        }

        var encounterFacts = request.OrchestrationResult.Encounters
            .Select(record =>
            {
                var hostileIds = record.Plan.HostileParticipants
                    .Select(x => x.SourceEntityId)
                    .ToArray();

                var hostileCreatures = hostileIds
                    .Select(id =>
                    {
                        if (!hostileCreaturesById.TryGetValue(id, out var creature))
                        {
                            throw new InvalidOperationException(
                                $"Hostile creature '{id}' could not be loaded for idle reward calculation.");
                        }

                        return creature;
                    })
                    .ToArray();

                return new IdleEncounterRewardFacts(
                    EncounterId: record.Plan.EncounterId,
                    Sequence: record.Plan.Sequence,
                    StartedAt: record.Plan.StartsAt,
                    Outcome: record.Resolution.Outcome,
                    HostileSourceEntityIds: hostileIds,
                    HostileCreatures: hostileCreatures,
                    CombatResult: record.Resolution.CombatResult);
            })
            .ToArray();

        return new IdleCombatRewardFacts(
            CharacterId: idleRequest.CharacterId,
            From: request.OrchestrationResult.From,
            RequestedTo: request.OrchestrationResult.RequestedTo,
            ProcessedUntil: request.OrchestrationResult.ProcessedUntil,
            ProcessedDuration: request.OrchestrationResult.ProcessedUntil - request.OrchestrationResult.From,
            Area: idleRequest.ActionDetails.Area,
            PlayerEntityIds: idleRequest.ActionDetails.CharacterTeam.ToArray(),
            Encounters: encounterFacts);
    }
}