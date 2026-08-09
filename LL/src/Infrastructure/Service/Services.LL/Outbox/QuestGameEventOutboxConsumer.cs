using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Quests;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

public sealed class QuestGameEventOutboxConsumer(
    IQuestProgressionService progression,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.Quests;

    public bool CanHandle(string eventType) =>
        eventType is GameEventTypes.EquipmentChanged
            or GameEventTypes.EssenceAbsorbed
            or GameEventTypes.EssenceLoadoutChanged
            or GameEventTypes.EquipmentCrafted
            or GameEventTypes.IdleCombatEncounterCompleted
            or GameEventTypes.CharacterCreated
            or GameEventTypes.CharacterLevelReached;

    public Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        if (!message.CharacterId.HasValue)
        {
            return Task.CompletedTask;
        }

        var trigger = message.EventType switch
        {
            GameEventTypes.EquipmentChanged => QuestTrigger.EquipmentChanged(),
            GameEventTypes.EssenceAbsorbed => QuestTrigger.EssenceAbsorbed(
                Read<EssenceAbsorbedPayload>(message).EssenceDefinitionId),
            GameEventTypes.EssenceLoadoutChanged => QuestTrigger.EssenceLoadoutChanged(),
            GameEventTypes.EquipmentCrafted => CreateEquipmentCraftedTrigger(
                Read<EquipmentCraftedPayload>(message)),
            GameEventTypes.IdleCombatEncounterCompleted => CreateCombatTrigger(
                Read<IdleCombatEncounterCompletedPayload>(message)),
            GameEventTypes.CharacterCreated => QuestTrigger.CharacterLevelReached(1),
            GameEventTypes.CharacterLevelReached => QuestTrigger.CharacterLevelReached(
                Read<CharacterLevelReachedPayload>(message).Level),
            _ => null
        };

        return trigger is null
            ? Task.CompletedTask
            : progression.ProcessAsync(
                message.CharacterId.Value,
                trigger,
                message.Id,
                message.EventType,
                cancellationToken);
    }

    private static QuestTrigger CreateEquipmentCraftedTrigger(EquipmentCraftedPayload payload) =>
        QuestTrigger.EquipmentCrafted(
            payload.CraftedItems.Select(x => x.ItemBaseId).ToList(),
            payload.CraftedItems.Select(x => x.Tier).ToList());

    private static QuestTrigger CreateCombatTrigger(IdleCombatEncounterCompletedPayload payload) =>
        QuestTrigger.CombatCompleted(payload.AreaId, payload.WonEncounter);

    private T Read<T>(GameEventOutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.PayloadJson, jsonOptions)
        ?? throw new InvalidOperationException(
            $"Outbox message '{message.Id}' could not be deserialized as {typeof(T).Name}.");
}
