using Domain.Models.CombatStyles;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record CombatEncounterPlan(
    Guid EncounterId,
    CombatMode Mode,
    int Sequence,
    DateTimeOffset StartsAt,
    IReadOnlyList<CombatParticipantSlot> Participants,
    CombatEncounterSourceContext SourceContext,
    CombatStyleSnapshot? PlayerCombatStyle = null,
    CombatStyleSnapshot? HostileCombatStyle = null)
{
    public IReadOnlyList<CombatParticipantSlot> FriendlyParticipants =>
        [.. Participants.Where(x => x.Side == CombatSide.Friendly)];

    public IReadOnlyList<CombatParticipantSlot> HostileParticipants =>
        [.. Participants.Where(x => x.Side == CombatSide.Hostile)];
}
