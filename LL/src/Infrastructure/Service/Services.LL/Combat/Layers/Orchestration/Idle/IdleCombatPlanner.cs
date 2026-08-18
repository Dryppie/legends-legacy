using Microsoft.Extensions.Options;
using Services.LL.CharacterActions;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Common.Randomness;
using System.Globalization;

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
        var nextEncounterAt = request.NextEncounterAt;

        // A resolved encounter advances the boundary by exactly one cadence, so
        // anything farther ahead is corrupted scheduling state. Treat it as due
        // now rather than allowing clients to remain frozen until that timestamp.
        if (nextEncounterAt > to + _encounterCadence)
        {
            nextEncounterAt = to;
        }

        var from = nextEncounterAt > to - _maximumOfflineDuration
            ? nextEncounterAt
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
                ScheduleGeneration: request.CharacterAction.ScheduleGeneration,
                PlayerEntityIds: [.. action.CharacterTeam],
                Area: action.Area,
                PlannedEncounterCount: 0)
            {
                CaptureFinalEncounterLog = request.CaptureFinalEncounterLog
            };
        }

        var elapsed = to - from;
        var dueEncounterCount = checked(
            1 + (int)(elapsed.Ticks / _encounterCadence.Ticks));
        var plannedEncounterCount = Math.Min(
            dueEncounterCount,
            _options.MaximumEncountersPerResolution);
        var executableUntil = from.AddTicks(plannedEncounterCount * _encounterCadence.Ticks);

        return new IdleCombatPlan(
            CharacterId: request.CharacterId,
            From: from,
            RequestedTo: to,
            ExecutableUntil: executableUntil,
            EncounterCadence: _encounterCadence,
            ScheduleGeneration: request.CharacterAction.ScheduleGeneration,
            PlayerEntityIds: [.. action.CharacterTeam],
            Area: action.Area,
            PlannedEncounterCount: plannedEncounterCount)
        {
            CaptureFinalEncounterLog = request.CaptureFinalEncounterLog
        };
    }

    public CombatEncounterPlan CreateEncounterPlan(
        IdleCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt)
    {
        var generation = plan.ScheduleGeneration.ToString(CultureInfo.InvariantCulture);
        var boundary = startsAt.UtcTicks.ToString(CultureInfo.InvariantCulture);
        var identity = new[]
        {
            "idle-combat-v1",
            plan.CharacterId.ToString("N"),
            generation,
            boundary,
            sequence.ToString(CultureInfo.InvariantCulture)
        };
        var randomSeed = StableRandom.Seed(identity);
        var random = new Random(randomSeed);
        var encounterId = StableRandom.Guid(identity);
        var monsterCount = _spawningService.HowManyMonstersToSpawn(plan.Area.SpawnProbabilities, random);
        var selectedCreatures = _spawningService.WhatAreaCreaturesToSpawn(
            plan.Area.Creatures.OrderBy(x => x.CreatureId).ToList(),
            monsterCount,
            random);

        var participants = new List<CombatParticipantSlot>();

        participants.AddRange(
            plan.PlayerEntityIds.Order().Select((id, index) =>
                new CombatParticipantSlot(
                    SlotId: StableRandom.Guid([.. identity, "friendly", index.ToString(CultureInfo.InvariantCulture), id.ToString("N")]).ToString("N"),
                    SourceEntityId: id,
                    Side: CombatSide.Friendly)));

        participants.AddRange(
            selectedCreatures.Select((creature, index) =>
                new CombatParticipantSlot(
                    SlotId: StableRandom.Guid([.. identity, "hostile", index.ToString(CultureInfo.InvariantCulture), creature.CreatureId.ToString("N")]).ToString("N"),
                    SourceEntityId: creature.CreatureId,
                    Side: CombatSide.Hostile)));

        return new CombatEncounterPlan(
            EncounterId: encounterId,
            Mode: CombatMode.Idle,
            Sequence: sequence,
            StartsAt: startsAt,
            Participants: participants,
            SourceContext: new IdleEncounterSourceContext(
                CharacterId: plan.CharacterId,
                Area: plan.Area,
                EncounterCadence: plan.EncounterCadence))
        {
            RandomSeed = randomSeed,
            CaptureEventLog = plan.CaptureFinalEncounterLog &&
                sequence == plan.PlannedEncounterCount
        };
    }
}
