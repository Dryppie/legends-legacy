using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;

namespace Services.LL.Combat.Layers.Orchestration.Dungeon;

public sealed class DungeonCombatPlanner : IDungeonCombatPlanner
{
    public DungeonCombatPlan CreatePlan(Guid dungeonRunId, Guid characterId, IReadOnlyList<Guid> playerEntityIds, IReadOnlyList<Guid> enemySourceEntityIds) =>
        new(DungeonRunId: dungeonRunId, CharacterId: characterId, PlayerEntityIds: playerEntityIds, EnemySourceEntityIds: enemySourceEntityIds);

    public CombatEncounterPlan CreateEncounterPlan(DungeonCombatPlan plan, int sequence, DateTimeOffset startsAt)
    {
        var participants = new List<CombatParticipantSlot>();

        participants.AddRange(
            plan.PlayerEntityIds.Select(id =>
                new CombatParticipantSlot(
                    SlotId: Guid.NewGuid().ToString(),
                    SourceEntityId: id,
                    Side: CombatSide.Friendly)));

        participants.AddRange(plan.EnemySourceEntityIds.Select(id =>
                new CombatParticipantSlot(
                    SlotId: Guid.NewGuid().ToString(),
                    SourceEntityId: id,
                    Side: CombatSide.Hostile)));

        return new CombatEncounterPlan(
            EncounterId: Guid.NewGuid(),
            Mode: CombatMode.Idle,
            Sequence: sequence,
            StartsAt: startsAt,
            Participants: participants,
            SourceContext: new DungeonEncounterSourceContext(
                DungeonRunId: plan.DungeonRunId));
    }
}