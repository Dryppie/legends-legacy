using Domain.Models.CombatStyles;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;

namespace Services.LL.Combat.Layers.Orchestration.Idle;

public sealed class IdleCombatPlanner : IIdleCombatPlanner
{
    private static readonly TimeSpan EncounterCadence = TimeSpan.FromSeconds(10);

    private readonly ISpawningService _spawningService;

    public IdleCombatPlanner(ISpawningService spawningService)
    {
        _spawningService = spawningService;
    }

    public IdleCombatPlan CreatePlan(IdleCombatOrchestrationRequest request)
    {
        var from = request.NextEncounterAt;
        var to = request.Now;
        var action = request.ActionDetails;

        if (from > to)
        {
            return new IdleCombatPlan(
                CharacterId: request.CharacterId,
                From: from,
                RequestedTo: to,
                ExecutableUntil: from,
                EncounterCadence: EncounterCadence,
                PlayerEntityIds: [.. action.CharacterTeam],
                Area: action.Area,
                PlannedEncounterCount: 0);
        }

        var elapsed = to - from;
        var plannedEncounterCount = 1 + (int)(elapsed.Ticks / EncounterCadence.Ticks);
        var executableUntil = from.AddTicks(plannedEncounterCount * EncounterCadence.Ticks);

        return new IdleCombatPlan(
            CharacterId: request.CharacterId,
            From: from,
            RequestedTo: to,
            ExecutableUntil: executableUntil,
            EncounterCadence: EncounterCadence,
            PlayerEntityIds: [.. action.CharacterTeam],
            Area: action.Area,
            PlannedEncounterCount: plannedEncounterCount);
    }

    public CombatEncounterPlan CreateEncounterPlan(
        IdleCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt,
        CombatStyleSnapshot? combatStyle = null)
    {
        var monsterCount = _spawningService.HowManyMonstersToSpawn(plan.Area.SpawnProbabilities);
        var selectedCreatures = _spawningService.WhatAreaCreaturesToSpawn(
            plan.Area.Creatures.ToList(),
            monsterCount);

        var participants = new List<CombatParticipantSlot>();

        participants.AddRange(
            plan.PlayerEntityIds.Select(id =>
                new CombatParticipantSlot(
                    SlotId: Guid.NewGuid().ToString(),
                    SourceEntityId: id,
                    Side: CombatSide.Friendly)));

        participants.AddRange(
            selectedCreatures.Select(creature =>
                new CombatParticipantSlot(
                    SlotId: Guid.NewGuid().ToString(),
                    SourceEntityId: creature.CreatureId,
                    Side: CombatSide.Hostile)));

        return new CombatEncounterPlan(
            EncounterId: Guid.NewGuid(),
            Mode: CombatMode.Idle,
            Sequence: sequence,
            StartsAt: startsAt,
            Participants: participants,
            SourceContext: new IdleEncounterSourceContext(
                CharacterId: plan.CharacterId,
                Area: plan.Area,
                EncounterCadence: plan.EncounterCadence),
            PlayerCombatStyle: combatStyle);
    }
}
