using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class CombatPreparationPipeline(
    ISnapshotCombatantBuilder snapshotCombatants,
    ICombatSetupService combatSetup) : ICombatPreparationPipeline
{
    public const int SchemaVersion = 1;

    public async Task<IReadOnlyList<CombatRuntimeParticipant>> PrepareAsync(
        CombatContentType contentType,
        IReadOnlyList<CombatantPreparationRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ValidateRequests(requests);
        if (requests.Count == 0)
            return [];

        var participants = new CombatRuntimeParticipant?[requests.Count];
        var snapshotRequests = requests
            .Select((request, index) => (Request: request, Index: index))
            .Where(x => x.Request.Source is SnapshotCombatantPreparationSource)
            .ToArray();

        if (snapshotRequests.Length > 0)
        {
            var builtSnapshots = await snapshotCombatants.BuildAsync(
                snapshotRequests.Select(x => new SnapshotCombatantRequest(
                    ((SnapshotCombatantPreparationSource)x.Request.Source).Snapshot,
                    x.Request.Slot)).ToArray(),
                cancellationToken);

            for (var i = 0; i < snapshotRequests.Length; i++)
                participants[snapshotRequests[i].Index] = builtSnapshots[i];
        }

        for (var index = 0; index < requests.Count; index++)
        {
            if (participants[index] is not null)
                continue;

            var request = requests[index];
            var live = request.Source as LiveCombatantPreparationSource
                ?? throw new InvalidOperationException(
                    $"Unsupported combatant source '{request.Source.GetType().Name}'.");
            var combatant = live.Entity switch
            {
                Character character => combatSetup.CreatePlayerCombatEntities([character]).Single(),
                Creature creature when live.CreatureArea is not null =>
                    combatSetup.CreateCreatureCombatEntities([creature], live.CreatureArea).Single(),
                Creature => throw new InvalidOperationException(
                    $"Creature combatant '{live.Entity.Id}' requires an explicit scaling area."),
                _ => throw new NotSupportedException(
                    $"Entity type '{live.Entity.GetType().Name}' is not supported for combat preparation.")
            };

            combatant.Id = request.Slot.SlotId;
            combatant.OriginalId = request.Slot.SourceEntityId;
            participants[index] = new CombatRuntimeParticipant(request.Slot, live.Entity, combatant);
        }

        var prepared = participants.Select(x => x!).ToArray();
        for (var index = 0; index < prepared.Length; index++)
            requests[index].ConfigureBeforePreparation?.Invoke(prepared[index].Combatant);

        await combatSetup.PrepareEntitiesForCombat(
            prepared.Select(x => x.Combatant).ToList(),
            contentType.ToEssenceActivity());

        for (var index = 0; index < prepared.Length; index++)
        {
            requests[index].ConfigureAfterPreparation?.Invoke(prepared[index].Combatant);
            ValidatePreparedParticipant(prepared[index]);
        }

        return prepared;
    }

    private static void ValidateRequests(IReadOnlyList<CombatantPreparationRequest> requests)
    {
        var duplicateSlot = requests
            .GroupBy(x => x.Slot.SlotId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicateSlot is not null)
            throw new InvalidOperationException($"Combat slot '{duplicateSlot.Key}' is duplicated.");

        foreach (var request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.Slot.SlotId))
                throw new InvalidOperationException("Combat slots require a stable runtime ID.");

            var sourceId = request.Source switch
            {
                LiveCombatantPreparationSource live => live.Entity.Id,
                SnapshotCombatantPreparationSource snapshot => snapshot.Snapshot.CharacterId,
                _ => throw new InvalidOperationException(
                    $"Unsupported combatant source '{request.Source.GetType().Name}'.")
            };
            if (sourceId != request.Slot.SourceEntityId)
            {
                throw new InvalidOperationException(
                    $"Combat slot '{request.Slot.SlotId}' identifies source '{request.Slot.SourceEntityId}', "
                    + $"but its preparation source is '{sourceId}'.");
            }
        }
    }

    private static void ValidatePreparedParticipant(CombatRuntimeParticipant participant)
    {
        var combatant = participant.Combatant;
        if (!string.Equals(combatant.Id, participant.Slot.SlotId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Prepared combatant '{combatant.Id}' lost its stable slot identity.");
        if (combatant.OriginalId != participant.Slot.SourceEntityId)
            throw new InvalidOperationException($"Prepared combatant '{combatant.Id}' lost its source identity.");

        var maxHealth = combatant.GetAttributeValue(AttributeType.MaxHealth);
        var health = combatant.GetCurrentHealthValue();
        if (maxHealth <= 0 || health < 0 || health > maxHealth)
        {
            throw new InvalidOperationException(
                $"Prepared combatant '{combatant.Id}' has invalid health '{health}/{maxHealth}'.");
        }
    }
}
