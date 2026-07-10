using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Rewards;
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

        var dungeon = run is null
            ? null
            : _dungeonDefinitions.GetByKey(run.DungeonDefinitionId);

        var monsterLootModifiers = dungeon?.MonsterLootModifiers ?? [];
        var gatheringNodes = dungeon is null
            ? []
            : await BuildGatheringNodesAsync(dungeon, cancellationToken);

        return new DungeonCombatRewardFacts(
            DungeonRunId: context.DungeonRunId,
            CharacterId: context.CharacterId,
            CurrentRoomIndex: context.OrchestrationRequest.CurrentRoomIndex,
            MonsterLootModifiers: monsterLootModifiers,
            PlayerEntityIds: [.. context.PlayerEntityIds],
            EquippedTool: ResolveEquippedTool(context),
            GatheringNodes: gatheringNodes,
            Encounters: encounterFacts);
    }

    private async Task<IReadOnlyList<CombatGatheringNode>> BuildGatheringNodesAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken)
    {
        return await Task.FromResult(dungeon.GatheringNodes
            .Select(node => ToCombatGatheringNode(dungeon.Id, node))
            .Where(node => node.HasRewards)
            .ToArray());
    }

    private static CombatGatheringNode ToCombatGatheringNode(
        string dungeonDefinitionId,
        DungeonGatheringNodeDefinition node)
    {
        var rewardTable = string.IsNullOrWhiteSpace(node.RewardTableId)
            ? BuildInlineRewardTable(dungeonDefinitionId, node)
            : null;

        return new CombatGatheringNode(
            node.Id,
            node.Name,
            node.Type,
            node.LevelRequirement,
            node.ProcChance,
            RewardTableId: node.RewardTableId,
            RewardTable: rewardTable);
    }

    private static RewardTableDefinition? BuildInlineRewardTable(
        string dungeonDefinitionId,
        DungeonGatheringNodeDefinition node)
    {
        if (node.Loot.Count == 0)
        {
            return null;
        }

        var totalWeight = node.Loot.Sum(entry => Math.Max(0, entry.Weight));
        var entries = node.Loot
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ItemId) && entry.Weight > 0)
            .Select(entry => new RewardEntryDefinition
            {
                Id = entry.ItemId,
                ItemId = entry.ItemId,
                Type = RewardEntryType.Item,
                Weight = entry.Weight,
                Quantity = new RewardQuantityRange
                {
                    Min = entry.MinQuantity,
                    Max = entry.MaxQuantity
                },
                Tags = entry.IsRare ? ["rare"] : []
            })
            .ToList();

        return new RewardTableDefinition
        {
            Id = $"reward.dungeon.{dungeonDefinitionId}.gathering.{node.Id}",
            DisplayName = node.Name,
            Rolls =
            [
                new RewardRollDefinition
                {
                    Id = "gathering_weighted_drop",
                    Type = RewardRollType.WeightedWithNoDrop,
                    NoDropWeight = Math.Max(0, 100 - totalWeight),
                    Entries = entries
                }
            ]
        };
    }

    private static EquippedGatheringTool? ResolveEquippedTool(DungeonCombatOutcomeContext context)
    {
        if (context.OrchestrationResult.SourceEntitiesById is null ||
            !context.OrchestrationResult.SourceEntitiesById.TryGetValue(context.CharacterId, out var character))
        {
            return null;
        }

        var tool = character.EquipmentSlots
            .FirstOrDefault(slot => slot.EquipmentSlotType == EquipmentSlotType.Tool)
            ?.EquipmentInstance;

        if (tool?.EquipmentBase.GatheringType is null)
        {
            return null;
        }

        return EquippedGatheringTool.From(tool);
    }
}
