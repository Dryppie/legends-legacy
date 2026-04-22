using Domain.Models.Combat;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Resolution.Models;

public sealed class CombatEncounterRuntime
{
    public CombatEncounterRuntime(
        CombatEncounterPlan plan,
        IReadOnlyList<CombatRuntimeParticipant> friendlyParticipants,
        IReadOnlyList<CombatRuntimeParticipant> hostileParticipants)
    {
        Plan = plan;
        FriendlyParticipants = friendlyParticipants;
        HostileParticipants = hostileParticipants;
    }

    public CombatEncounterPlan Plan { get; }

    public IReadOnlyList<CombatRuntimeParticipant> FriendlyParticipants { get; }

    public IReadOnlyList<CombatRuntimeParticipant> HostileParticipants { get; }

    public IReadOnlyList<CombatEntity> AllCombatants =>
        [.. FriendlyParticipants.Select(x => x.Combatant), .. HostileParticipants.Select(x => x.Combatant)];
}
