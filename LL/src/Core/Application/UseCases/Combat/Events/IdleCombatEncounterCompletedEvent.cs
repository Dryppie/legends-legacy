using MediatR;

namespace Application.UseCases.Combat.Events;

public sealed record IdleCombatEncounterCompletedEvent(
    Guid CharacterId,
    string AreaId,
    bool WonEncounter) : INotification;
