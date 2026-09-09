using Domain.Models.Entities;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatResolutionSession
{
    IReadOnlyDictionary<Guid, Entity> SourceEntitiesById { get; }

    IReadOnlyDictionary<Guid, Domain.Models.CombatStyles.CombatStyleSnapshot> CapturedCombatStyles =>
        new Dictionary<Guid, Domain.Models.CombatStyles.CombatStyleSnapshot>();

    void AdvanceCombatStyle(Guid characterId, int level) { }

    Task<CombatEncounterResolutionResult> ResolveAsync(
        CombatEncounterPlan encounterPlan,
        CancellationToken cancellationToken);
}
