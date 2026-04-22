using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatEncounterResultFactory
{
    CombatEncounterResolutionResult Create(
        CombatEncounterRuntime runtime,
        CombatResult combatResult);
}