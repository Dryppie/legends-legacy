using Application.Interfaces.Services.LL.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Items.Equipments.Slots;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Idle;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed class IdleCombatRewardFactBuilder : IIdleCombatRewardFactBuilder
{
    private readonly IEntityService _entityService;

    public IdleCombatRewardFactBuilder(IEntityService entityService)
    {
        _entityService = entityService;
    }

    public async Task<IdleCombatRewardFacts> BuildAsync(
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
            CharacterId: context.CharacterId,
            From: context.Details.From,
            RequestedTo: context.Details.RequestedTo,
            ProcessedUntil: context.Details.ProcessedUntil,
            ProcessedDuration: context.Details.ProcessedDuration,
            Area: context.Area,
            PlayerEntityIds: [.. context.PlayerEntityIds],
            EquippedTool: ResolveEquippedTool(context),
            Encounters: encounterFacts);
    }

    private static EquippedGatheringTool? ResolveEquippedTool(IdleCombatOutcomeContext context)
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
