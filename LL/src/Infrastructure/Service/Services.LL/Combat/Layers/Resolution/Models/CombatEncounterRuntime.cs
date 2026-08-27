using Domain.Models.Combat;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Resolution.Models;

public sealed class CombatEncounterRuntime
{
    public CombatEncounterRuntime(
        CombatEncounterPlan plan,
        IReadOnlyList<CombatRuntimeParticipant> friendlyParticipants,
        IReadOnlyList<CombatRuntimeParticipant> hostileParticipants,
        IReadOnlyList<IReadOnlyList<CombatRuntimeParticipant>>? hostileReinforcementWaves = null,
        Func<int, IReadOnlyList<CombatRuntimeParticipant>?>? hostileWaveFactory = null)
    {
        var expectedMode = plan.ContentType.ToCombatMode();
        if (plan.Mode != expectedMode)
        {
            throw new InvalidOperationException(
                $"Combat content type '{plan.ContentType}' requires mode '{expectedMode}', not '{plan.Mode}'.");
        }

        Plan = plan;
        FriendlyParticipants = friendlyParticipants;
        HostileParticipants = hostileParticipants;
        HostileReinforcementWaves = hostileReinforcementWaves ?? [];
        HostileWaveFactory = hostileWaveFactory;
    }

    public CombatEncounterPlan Plan { get; }

    public IReadOnlyList<CombatRuntimeParticipant> FriendlyParticipants { get; }

    public IReadOnlyList<CombatRuntimeParticipant> HostileParticipants { get; }

    public IReadOnlyList<IReadOnlyList<CombatRuntimeParticipant>> HostileReinforcementWaves { get; }

    public Func<int, IReadOnlyList<CombatRuntimeParticipant>?>? HostileWaveFactory { get; }

    public IReadOnlyList<CombatRuntimeParticipant> AllHostileParticipants =>
        [.. HostileParticipants, .. HostileReinforcementWaves.SelectMany(x => x)];

    public IReadOnlyList<CombatEntity> AllCombatants =>
        [.. FriendlyParticipants.Select(x => x.Combatant), .. AllHostileParticipants.Select(x => x.Combatant)];
}
