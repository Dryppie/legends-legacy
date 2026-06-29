using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Orchestration;
using Services.LL.Interfaces.Combat.Resolution.Idle;

namespace Services.LL.Combat.Layers.Orchestration.Idle;

public sealed class IdleCombatOrchestrator : ICombatOrchestrator
{
    private readonly IIdleCombatPlanner _planner;
    private readonly IIdleCombatResolutionSessionFactory _resolutionSessionFactory;

    public IdleCombatOrchestrator(
        IIdleCombatPlanner planner,
        IIdleCombatResolutionSessionFactory resolutionSessionFactory)
    {
        _planner = planner;
        _resolutionSessionFactory = resolutionSessionFactory;
    }

    public CombatMode Mode => CombatMode.Idle;

    public async Task<CombatOrchestrationResult> OrchestrateAsync(
        CombatOrchestrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is not IdleCombatOrchestrationRequest idleRequest)
        {
            throw new ArgumentException(
                $"Expected {nameof(IdleCombatOrchestrationRequest)} but got {request.GetType().Name}.",
                nameof(request));
        }

        var plan = _planner.CreatePlan(idleRequest);

        if (plan.PlannedEncounterCount == 0)
        {
            return CombatOrchestrationResults.None(CombatMode.Idle, new IdleCombatOrchestrationDetails(
                From: plan.From,
                RequestedTo: plan.RequestedTo,
                ProcessedUntil: plan.From,
                PlannedEncounterCount: plan.PlannedEncounterCount,
                EncounterCadence: plan.EncounterCadence));
        }

        var records = new List<CombatEncounterRecord>(plan.PlannedEncounterCount);
        var cursor = plan.From;

        var resolutionSession = await _resolutionSessionFactory.CreateAsync(
            plan,
            cancellationToken);

        for (var sequence = 1; sequence <= plan.PlannedEncounterCount; sequence++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var encounterPlan = _planner.CreateEncounterPlan(plan, sequence, cursor);
            var resolution = await resolutionSession.ResolveAsync(encounterPlan, cancellationToken);

            records.Add(new CombatEncounterRecord(encounterPlan, resolution));

            cursor = cursor.Add(plan.EncounterCadence);
        }

        return new CombatOrchestrationResult(
            SessionId: Guid.NewGuid(),
            Mode: CombatMode.Idle,
            Encounters: records,
            Details: new IdleCombatOrchestrationDetails(
                From: plan.From,
                RequestedTo: plan.RequestedTo,
                ProcessedUntil: cursor,
                PlannedEncounterCount: plan.PlannedEncounterCount,
                EncounterCadence: plan.EncounterCadence),
            SourceEntitiesById: resolutionSession.SourceEntitiesById);
    }
}
