using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatRewardFactBuilder : IIdleCombatRewardFactBuilder
{
    public Task<IdleCombatRewardFacts> BuildAsync(
        IdleCombatOutcomeContext context,
        CancellationToken cancellationToken)
    {
        var hostileSourceIds = context.Encounters
            .SelectMany(x => x.Plan.HostileParticipants)
            .Select(x => x.SourceEntityId)
            .Distinct()
            .ToList();

        var hostileCreaturesById = new Dictionary<Guid, Creature>();
        if (hostileSourceIds.Count > 0)
        {
            var sourceEntitiesById = context.OrchestrationResult.SourceEntitiesById
                ?? throw new InvalidOperationException(
                    "Idle combat orchestration did not provide its preloaded source entities.");

            foreach (var hostileSourceId in hostileSourceIds)
            {
                if (!sourceEntitiesById.TryGetValue(hostileSourceId, out var entity) || entity is not Creature creature)
                {
                    throw new InvalidOperationException(
                        $"Hostile creature '{hostileSourceId}' was not available in the idle source catalog.");
                }

                hostileCreaturesById[hostileSourceId] = creature;
            }
        }

        var encounterFacts = context.Encounters
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

        return Task.FromResult(new IdleCombatRewardFacts(
            CharacterId: context.CharacterId,
            From: context.Details.From,
            RequestedTo: context.Details.RequestedTo,
            ProcessedUntil: context.Details.ProcessedUntil,
            ProcessedDuration: context.Details.ProcessedDuration,
            Area: context.Area,
            PlayerEntityIds: [.. context.PlayerEntityIds],
            Encounters: encounterFacts) { ScheduleGeneration = context.OrchestrationRequest.CharacterAction.ScheduleGeneration });
    }

}
