using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.LootTables;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public class DungeonCombatRewardFactBuilder : IDungeonCombatRewardFactBuilder
{
    private readonly IEntityService _entityService;
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IItemBaseRepository _itemBases;

    public DungeonCombatRewardFactBuilder(
        IEntityService entityService,
        IDungeonRunRepository dungeonRuns,
        IDungeonDefinitions dungeonDefinitions,
        IItemBaseRepository itemBases)
    {
        _entityService = entityService;
        _dungeonRuns = dungeonRuns;
        _dungeonDefinitions = dungeonDefinitions;
        _itemBases = itemBases;
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
        if (dungeon.GatheringNodes.Count == 0)
        {
            return [];
        }

        var itemIds = dungeon.GatheringNodes
            .SelectMany(node => node.Loot)
            .Select(entry => entry.ItemId)
            .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var itemBases = await _itemBases.GetItemBasesByIdsAsync(itemIds, cancellationToken);

        return dungeon.GatheringNodes
            .Select(node => ToCombatGatheringNode(node, itemBases))
            .Where(node => node.LootTable.Entries.Count > 0)
            .ToArray();
    }

    private static CombatGatheringNode ToCombatGatheringNode(
        DungeonGatheringNodeDefinition node,
        IReadOnlyDictionary<string, ItemBase> itemBases)
    {
        var itemEntries = node.Loot
            .Where(entry => itemBases.ContainsKey(entry.ItemId))
            .Select(entry => new LootTableItem
            {
                Id = Guid.NewGuid(),
                ItemId = entry.ItemId,
                Item = itemBases[entry.ItemId],
                Weight = entry.Weight,
                MinQuantity = entry.MinQuantity,
                MaxQuantity = entry.MaxQuantity,
                IsRare = entry.IsRare
            })
            .ToList<LootTableEntry>();

        var itemTable = new LootTable
        {
            Id = Guid.NewGuid(),
            Weight = itemEntries.Sum(entry => Math.Max(0, entry.Weight)),
            Entries = itemEntries
        };

        return new CombatGatheringNode(
            node.Id,
            node.Name,
            node.Type,
            node.LevelRequirement,
            node.ProcChance,
            new LootTable
            {
                Id = Guid.NewGuid(),
                Entries = itemEntries.Count == 0 ? [] : [itemTable]
            });
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
            ?.EquipmentInstance
            ?.EquipmentBase;

        if (tool?.GatheringType is null)
        {
            return null;
        }

        return new EquippedGatheringTool(
            Name: tool.Name,
            GatheringType: tool.GatheringType.Value,
            YieldBonusPercent: tool.YieldBonusPercent,
            RareChanceBonusPercent: tool.RareChanceBonusPercent,
            DoubleGatherChancePercent: tool.DoubleGatherChancePercent);
    }
}
