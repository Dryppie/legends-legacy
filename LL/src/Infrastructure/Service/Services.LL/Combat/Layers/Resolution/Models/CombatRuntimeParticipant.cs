using Domain.Models.Combat;
using Domain.Models.Entities;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Resolution.Models;

public sealed record CombatRuntimeParticipant(
    CombatParticipantSlot Slot,
    Entity SourceEntity,
    CombatEntity Combatant);
