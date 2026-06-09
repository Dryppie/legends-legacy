using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public class DungeonCombatRewardFactBuilder : IDungeonCombatRewardFactBuilder
{
    private readonly IEntityService _entityService;
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly IDungeonDefinitions _dungeonDefinitions;

    public DungeonCombatRewardFactBuilder(
        IEntityService entityService,
        IDungeonRunRepository dungeonRuns,
        IDungeonDefinitions dungeonDefinitions)
    {
        _entityService = entityService;
        _dungeonRuns = dungeonRuns;
        _dungeonDefinitions = dungeonDefinitions;
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
                                $"Hostile creature '{id}' could not be loaded for dungeon reward calculation.");
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

        var run = await _dungeonRuns.GetDungeonRunByDungeonIdAsync(
            context.DungeonRunId,
            cancellationToken);

        var monsterLootModifiers = run is null
            ? new Dictionary<ItemType, double>()
            : _dungeonDefinitions.GetByKey(run.DungeonDefinitionId).MonsterLootModifiers;

        return new DungeonCombatRewardFacts(
            DungeonRunId: context.DungeonRunId,
            CharacterId: context.CharacterId,
            CurrentRoomIndex: context.OrchestrationRequest.CurrentRoomIndex,
            MonsterLootModifiers: monsterLootModifiers,
            PlayerEntityIds: [.. context.PlayerEntityIds],
            Encounters: encounterFacts);
    }
}
