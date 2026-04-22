namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record CombatParticipantSlot(
    string SlotId,
    Guid SourceEntityId,
    CombatSide Side);