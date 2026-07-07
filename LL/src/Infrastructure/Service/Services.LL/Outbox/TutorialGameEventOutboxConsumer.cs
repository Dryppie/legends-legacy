using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Tutorials;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class TutorialGameEventOutboxConsumer(
    ITutorialProgressionService tutorialProgression,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.Tutorial;

    public bool CanHandle(string eventType) =>
        eventType is GameEventTypes.EquipmentChanged
            or GameEventTypes.EssenceAbsorbed
            or GameEventTypes.EssenceLoadoutChanged
            or GameEventTypes.EquipmentCrafted
            or GameEventTypes.IdleCombatEncounterCompleted
            or GameEventTypes.ClientTutorialStep;

    public Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var trigger = message.EventType switch
        {
            GameEventTypes.EquipmentChanged =>
                TutorialTrigger.EquipmentChanged(),

            GameEventTypes.EssenceAbsorbed =>
                TutorialTrigger.EssenceAbsorbed(
                    Read<EssenceAbsorbedPayload>(message).EssenceDefinitionId),

            GameEventTypes.EssenceLoadoutChanged =>
                TutorialTrigger.EssenceLoadoutChanged(
                    Read<EssenceLoadoutChangedPayload>(message).AttunedPlayerEssenceIds),

            GameEventTypes.EquipmentCrafted =>
                CreateCraftedEquipmentTrigger(Read<EquipmentCraftedPayload>(message)),

            GameEventTypes.IdleCombatEncounterCompleted =>
                CreateIdleCombatTrigger(Read<IdleCombatEncounterCompletedPayload>(message)),

            GameEventTypes.ClientTutorialStep =>
                CreateClientStepTrigger(Read<ClientTutorialStepPayload>(message)),

            _ => null
        };

        return trigger is null || message.CharacterId is null
            ? Task.CompletedTask
            : tutorialProgression.TryProgressAsync(message.CharacterId.Value, trigger, cancellationToken);
    }

    private TutorialTrigger? CreateCraftedEquipmentTrigger(EquipmentCraftedPayload payload) =>
        payload.CraftedItems.Count == 0
            ? null
            : TutorialTrigger.CraftedEquipment(
                payload.CraftedItems.Select(x => x.ItemBaseId).ToList(),
                payload.CraftedItems.Select(x => x.Tier).ToList());

    private static TutorialTrigger CreateIdleCombatTrigger(IdleCombatEncounterCompletedPayload payload) =>
        TutorialTrigger.IdleCombatCompleted(payload.AreaId, payload.WonEncounter);

    private static TutorialTrigger CreateClientStepTrigger(ClientTutorialStepPayload payload) =>
        TutorialTrigger.ClientStep(payload.StepKey, payload.TriggerType, payload.Route);

    private T Read<T>(GameEventOutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.PayloadJson, jsonOptions)
        ?? throw new InvalidOperationException(
            $"Outbox message '{message.Id}' could not be deserialized as {typeof(T).Name}.");
}
