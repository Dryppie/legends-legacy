using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Domain.Models.Analytics;
using Domain.Models.Combat;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Combat.Layers.Resolution.Models;

namespace Services.LL.Analytics;

public static class ItemizationTelemetryCapture
{
    public static Task RecordEquipmentAsync(this IGameEventOutbox outbox, string kind, string operation,
        Guid characterId, EquipmentData equipment, string context, CancellationToken ct,
        IReadOnlyList<Guid>? replaced = null, ItemizationChoiceContext? choices = null) => outbox.EnqueueAsync(GameEventTypes.ItemizationObserved,
        new ItemizationObservation(ItemizationObservation.StableId(kind, operation, equipment.State.Id), kind,
            characterId, DateTimeOffset.UtcNow, context, equipment.StatVersion, equipment.State.Id, equipment, replaced) { Choices = choices },
        characterId, null, ct);

    public static async Task RecordBattleAsync(this IGameEventOutbox outbox, CombatEncounterRuntime runtime,
        CombatResult result, CancellationToken ct)
    {
        var enemies = string.Join("+", runtime.AllHostileParticipants.Select(x => x.Combatant.SourceMonsterId)
            .Where(x => !string.IsNullOrEmpty(x)).Distinct().Order());
        foreach (var participant in runtime.FriendlyParticipants.Concat(runtime.AllHostileParticipants)
                     .Where(x => x.Combatant.IsPlayerCharacter).DistinctBy(x => x.Combatant.OriginalId))
        {
            var entity = participant.Combatant;
            var stats = result.EntityStats.SingleOrDefault(x => x.EntityId == entity.Id);
            if (stats is null || entity.OriginalId == Guid.Empty) continue;
            var friendly = runtime.FriendlyParticipants.Contains(participant);
            var outcome = result.EngineOutcome == BattleOutcome.Draw ? "Draw"
                : (result.EngineOutcome == BattleOutcome.Victory) == friendly ? "Win" : "Loss";
            var observation = new ItemizationObservation(
                ItemizationObservation.StableId("battle", runtime.Plan.EncounterId.ToString("N"), entity.OriginalId),
                "battle", entity.OriginalId, runtime.Plan.StartsAt, runtime.Plan.ContentType.ToString(), entity.AttributeRulesVersion,
                Build: ItemizationBuildSnapshot.Capture(entity),
                Battle: new(runtime.Plan.EncounterId, runtime.Plan.Mode.ToString(), enemies, outcome, result.Duration,
                    ItemizationBattleSnapshot.WithoutNames(stats)));
            await outbox.EnqueueAsync(GameEventTypes.ItemizationObserved, observation, entity.OriginalId, null, ct);
        }
    }
}
