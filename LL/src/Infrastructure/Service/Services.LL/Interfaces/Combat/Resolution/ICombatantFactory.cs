using Domain.Models.Combat;
using Domain.Models.Entities;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Resolution;

public interface ICombatantFactory
{
    CombatEntity Create(
        CombatParticipantSlot slot,
        Entity sourceEntity,
        CombatEncounterSourceContext sourceContext);
}