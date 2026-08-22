using Application.Interfaces.Services.LL.Entities;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Encounters;
using Domain.Models.Dungeons.Definitions.Gathering;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Dungeons.Mastery;
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
            : await BuildGatheringNodesAsync(
                dungeon,
                run?.State?.MasteryLevelAtStart ?? 0,
                cancellationToken);
        var room = run?.Rooms.FirstOrDefault(x =>
            x.RoomIndex == context.OrchestrationRequest.CurrentRoomIndex);
        var roomType = room?.Type ?? RoomType.Unknown;
        var featuredEssenceMonsterDefinitionId =
            roomType is RoomType.MiniBoss or RoomType.Boss
                ? room?.EncounterIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(DungeonEncounterIdentity.ToMonsterDefinitionId)
                    .FirstOrDefault()
                : null;

        return new DungeonCombatRewardFacts(
            DungeonRunId: context.DungeonRunId,
            CharacterId: context.CharacterId,
            CurrentRoomIndex: context.OrchestrationRequest.CurrentRoomIndex,
            DungeonTier: dungeon?.Tier ?? throw new InvalidOperationException(
                $"Dungeon definition for run '{context.DungeonRunId}' could not be resolved."),
            RoomType: roomType,
            FeaturedEssenceMonsterDefinitionId: featuredEssenceMonsterDefinitionId,
            MonsterLootModifiers: monsterLootModifiers,
            PlayerEntityIds: [.. context.PlayerEntityIds],
            EquippedTool: ResolveEquippedTool(context),
            GatheringNodes: gatheringNodes,
            Encounters: encounterFacts);
    }

    private async Task<IReadOnlyList<CombatGatheringNode>> BuildGatheringNodesAsync(
        DungeonDefinition dungeon,
        int masteryLevel,
        CancellationToken cancellationToken)
    {
        var gatheringChanceBonus = DungeonMasteryBenefits
            .Resolve(masteryLevel)
            .GatheringProcChanceBonus;

        return await Task.FromResult(dungeon.GatheringNodes
            .Select(node => ToCombatGatheringNode(dungeon.Id, node, gatheringChanceBonus))
            .Where(node => node.HasRewards)
            .ToArray());
    }

    private static CombatGatheringNode ToCombatGatheringNode(
        string dungeonDefinitionId,
        DungeonGatheringNodeDefinition node,
        double gatheringChanceBonus)
    {
        var rewardTable = string.IsNullOrWhiteSpace(node.RewardTableId)
            ? BuildInlineRewardTable(dungeonDefinitionId, node)
            : null;

        return new CombatGatheringNode(
            node.Id,
            node.Name,
            node.Type,
            node.LevelRequirement,
            (float)Math.Clamp(node.ProcChance + gatheringChanceBonus, 0d, 1d),
            RewardTableId: node.RewardTableId,
            RewardTable: rewardTable);
    }

    private static RewardTableDefinition? BuildInlineRewardTable(
        string dungeonDefinitionId,
        DungeonGatheringNodeDefinition node)
    {
        if (node.Loot.Count == 0 && node.BonusRewardTableIds.Count == 0)
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

        var rolls = new List<RewardRollDefinition>();
        if (entries.Count > 0)
        {
            rolls.Add(new RewardRollDefinition
            {
                Id = "gathering_weighted_drop",
                Type = RewardRollType.WeightedWithNoDrop,
                NoDropWeight = Math.Max(0, 100 - totalWeight),
                Entries = entries
            });
        }

        rolls.AddRange(node.BonusRewardTableIds.Select((rewardTableId, index) =>
            new RewardRollDefinition
            {
                Id = $"gathering_bonus_reference_{index + 1}",
                Type = RewardRollType.Reference,
                Entries =
                [
                    new RewardEntryDefinition
                    {
                        Id = $"bonus_reference_{index + 1}",
                        Type = RewardEntryType.RewardTableReference,
                        RewardTableId = rewardTableId,
                        Weight = 1,
                        Quantity = new RewardQuantityRange { Min = 1, Max = 1 }
                    }
                ]
            }));

        return new RewardTableDefinition
        {
            Id = $"reward.dungeon.{dungeonDefinitionId}.gathering.{node.Id}",
            DisplayName = node.Name,
            Rolls = rolls
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
