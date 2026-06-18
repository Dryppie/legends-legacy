using Domain.Models.Combat;
using Domain.Models.Entities;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution.Idle;

public sealed class IdleCombatResolutionSession : ICombatResolutionSession
{
    public IdleCombatTemplateCatalog Catalog = null!;
    private readonly ICombatEngineExecutor _engineExecutor;
    private readonly ICombatEncounterResultFactory _resultFactory;

    public IdleCombatResolutionSession(
        ICombatEngineExecutor engineExecutor,
        ICombatEncounterResultFactory resultFactory)
    {
        _engineExecutor = engineExecutor;
        _resultFactory = resultFactory;
    }

    public IReadOnlyDictionary<Guid, Entity> SourceEntitiesById => Catalog.SourceEntitiesById;

    public async Task<CombatEncounterResolutionResult> ResolveAsync(
        CombatEncounterPlan encounterPlan,
        CancellationToken cancellationToken)
    {
        var friendlyParticipants = encounterPlan.FriendlyParticipants
            .Select(CreateFriendlyRuntimeParticipant)
            .ToList();

        var hostileParticipants = encounterPlan.HostileParticipants
            .Select(CreateHostileRuntimeParticipant)
            .ToList();

        var runtime = new CombatEncounterRuntime(
            encounterPlan,
            friendlyParticipants,
            hostileParticipants);

        var combatResult = await _engineExecutor.ExecuteAsync(runtime, cancellationToken);

        return _resultFactory.Create(runtime, combatResult);
    }

    private CombatRuntimeParticipant CreateFriendlyRuntimeParticipant(
        CombatParticipantSlot slot)
    {
        if (!Catalog.SourceEntitiesById.TryGetValue(slot.SourceEntityId, out var sourceEntity))
        {
            throw new InvalidOperationException(
                $"Friendly source entity '{slot.SourceEntityId}' was not found in idle resolution catalog.");
        }

        if (!Catalog.FriendlyTemplatesBySourceEntityId.TryGetValue(slot.SourceEntityId, out var template))
        {
            throw new InvalidOperationException(
                $"Friendly template for source entity '{slot.SourceEntityId}' was not found in idle resolution catalog.");
        }

        var combatant = CloneTemplate(template, slot);

        return new CombatRuntimeParticipant(
            slot,
            sourceEntity,
            combatant);
    }

    private CombatRuntimeParticipant CreateHostileRuntimeParticipant(
        CombatParticipantSlot slot)
    {
        if (!Catalog.SourceEntitiesById.TryGetValue(slot.SourceEntityId, out var sourceEntity))
        {
            throw new InvalidOperationException(
                $"Hostile source entity '{slot.SourceEntityId}' was not found in idle resolution catalog.");
        }

        if (!Catalog.HostileTemplatesBySourceEntityId.TryGetValue(slot.SourceEntityId, out var template))
        {
            throw new InvalidOperationException(
                $"Hostile template for source entity '{slot.SourceEntityId}' was not found in idle resolution catalog.");
        }

        var combatant = CloneTemplate(template, slot);

        return new CombatRuntimeParticipant(
            slot,
            sourceEntity,
            combatant);
    }

    private static CombatEntity CloneTemplate(
        CombatEntity template,
        CombatParticipantSlot slot)
    {
        var combatant = template.DeepCloneForEncounter(); // This must be a deep copy.

        combatant.Id = slot.SlotId;
        combatant.OriginalId = slot.SourceEntityId;

        return combatant;
    }
}
