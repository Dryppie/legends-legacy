using System;
using System.Collections.Generic;
using System.Text;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record CombatOrchestrationResult(
    Guid SessionId,
    CombatMode Mode,
    DateTimeOffset From,
    DateTimeOffset RequestedTo,
    DateTimeOffset ProcessedUntil,
    int PlannedEncounterCount,
    IReadOnlyList<CombatEncounterRecord> Encounters)
{
    public bool HasAnyCombat => Encounters.Count > 0;

    public CombatEncounterRecord? LastEncounter =>
        Encounters.Count == 0 ? null : Encounters[^1];

    public static CombatOrchestrationResult None(
        CombatMode mode,
        DateTimeOffset from,
        DateTimeOffset requestedTo)
    {
        return new CombatOrchestrationResult(
            SessionId: Guid.NewGuid(),
            Mode: mode,
            From: from,
            RequestedTo: requestedTo,
            ProcessedUntil: from,
            PlannedEncounterCount: 0,
            Encounters: []
            );
    }
}