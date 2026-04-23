using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities.Creatures;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public class DungeonCombatRewardFactBuilder : IDungeonCombatRewardFactBuilder
{
    private readonly IEntityService _entityService;

    public DungeonCombatRewardFactBuilder(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public async Task<DungeonCombatRewardFacts> BuildAsync(
        DungeonCombatOutcomeContext context,
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
            var hostileEntities = await _entityService.GetEntitiesByIdsForCombatAsync(
                hostileSourceIds,
                cancellationToken);

            hostileCreaturesById = hostileEntities
                .OfType<Creature>()
                .ToDictionary(x => x.Id);
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

                return new DungeonEncounterRewardFacts(
                    EncounterId: record.Plan.EncounterId,
                    Outcome: record.Resolution.Outcome,
                    HostileSourceEntityIds: hostileIds,
                    HostileCreatures: hostileCreatures,
                    CombatResult: record.Resolution.CombatResult);
            })
            .ToArray();

        return new DungeonCombatRewardFacts(
            CharacterId: context.CharacterId,
            PlayerEntityIds: [.. context.PlayerEntityIds],
            Encounters: encounterFacts);
    }
}