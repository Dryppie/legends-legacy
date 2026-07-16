using Microsoft.Extensions.Options;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;

namespace Services.LL.Combat.Layers.Orchestration.Idle;

public sealed class IdleCombatPlanner : IIdleCombatPlanner
{
    private readonly ISpawningService _spawningService;
    private readonly IdleCombatProgressionOptions _options;
    private readonly TimeSpan _encounterCadence;
    private readonly TimeSpan _maximumOfflineDuration;

    public IdleCombatPlanner(
        ISpawningService spawningService,
        IOptions<IdleCombatProgressionOptions> options)
    {
        _spawningService = spawningService;
        _options = options.Value;
        _encounterCadence = TimeSpan.FromSeconds(_options.EncounterCadenceSeconds);
        _maximumOfflineDuration = TimeSpan.FromHours(_options.MaximumOfflineHours);
    }

    public IdleCombatPlan CreatePlan(IdleCombatOrchestrationRequest request)
    {
        var to = request.Now;
        var from = request.NextEncounterAt > to - _maximumOfflineDuration
            ? request.NextEncounterAt
            : to - _maximumOfflineDuration;
        var action = request.ActionDetails;

        if (from > to)
        {
            return new IdleCombatPlan(
                CharacterId: request.CharacterId,
                From: from,
                RequestedTo: to,
                ExecutableUntil: from,
                EncounterCadence: _encounterCadence,
                PlayerEntityIds: [.. action.CharacterTeam],
                Area: action.Area,
                PlannedEncounterCount: 0);
        }

        var elapsed = to - from;
        var plannedEncounterCount = checked(
            1 + (int)(elapsed.Ticks / _encounterCadence.Ticks));
        var executableUntil = from.AddTicks(plannedEncounterCount * _encounterCadence.Ticks);

        return new IdleCombatPlan(
            CharacterId: request.CharacterId,
            From: from,
            RequestedTo: to,
            ExecutableUntil: executableUntil,
            EncounterCadence: _encounterCadence,
            PlayerEntityIds: [.. action.CharacterTeam],
            Area: action.Area,
            PlannedEncounterCount: plannedEncounterCount);
    }

    public CombatEncounterPlan CreateEncounterPlan(
        IdleCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt)
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
                EncounterCadence: plan.EncounterCadence));
    }
}
