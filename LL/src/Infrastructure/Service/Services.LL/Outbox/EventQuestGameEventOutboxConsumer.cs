using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Quests;
using Application.Interfaces.Services.LL.Quests.Events;
using Application.Interfaces.Services.LL.Administration;
using Application.UseCases.Outbox;
using Domain.Models.Outbox;
using Application.Interfaces.Services.LL.Entities;

namespace Services.LL.Outbox;

public sealed class EventQuestGameEventOutboxConsumer(
    IEventQuestProgressionService progression,
    IAccountRestrictionIndex restrictions,
    JsonSerializerOptions jsonOptions,
    ICharacterService? characters = null) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.EventQuests;

    public bool CanHandle(string eventType) =>
        eventType is GameEventTypes.EssenceAbsorbed
            or GameEventTypes.EssenceLoadoutChanged
            or GameEventTypes.EssenceFocusSet
            or GameEventTypes.FocusedCreatureEssenceReceived
            or GameEventTypes.EssenceAscended
            or GameEventTypes.EquipmentCrafted
            or GameEventTypes.EquipmentTempered
            or GameEventTypes.IdleCombatEncounterCompleted
            or GameEventTypes.CharacterCreated
            or GameEventTypes.CharacterLevelReached
            or GameEventTypes.ColosseumBattleCompleted
            or GameEventTypes.TournamentBattleCompleted
            or GameEventTypes.DungeonRunStarted
            or GameEventTypes.DungeonRunCompleted
            or GameEventTypes.ProphecyCompleted;

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        if (!message.CharacterId.HasValue)
        {
            throw new InvalidOperationException(
                $"Event-quest outbox message '{message.Id}' has no character identity.");
        }
        var accountId = message.AccountId;
        if (!accountId.HasValue)
        {
            var character = characters is null
                ? null
                : await characters.GetBaseCharacterByIdAsync(
                    message.CharacterId.Value,
                    cancellationToken);
            accountId = character?.UserId ?? throw new InvalidOperationException(
                $"Event-quest outbox message '{message.Id}' has no resolvable account identity.");
        }
        if (!restrictions.Get(accountId.Value).CanParticipate)
        {
            return;
        }
        var trigger = message.EventType switch
        {
            GameEventTypes.EssenceAbsorbed => QuestTrigger.EssenceAbsorbed(
                Read<EssenceAbsorbedPayload>(message).EssenceDefinitionId),
            GameEventTypes.EssenceLoadoutChanged => QuestTrigger.EssenceLoadoutChanged(
                Read<EssenceLoadoutChangedPayload>(message).HasCompatibleEssenceTrio),
            GameEventTypes.EssenceFocusSet => QuestTrigger.EssenceFocusSet(),
            GameEventTypes.FocusedCreatureEssenceReceived => CreateFocusedEssenceTrigger(
                Read<FocusedCreatureEssenceReceivedPayload>(message)),
            GameEventTypes.EssenceAscended => QuestTrigger.EssenceAscended(),
            GameEventTypes.EquipmentCrafted => CreateEquipmentCraftedTrigger(Read<EquipmentCraftedPayload>(message)),
            GameEventTypes.EquipmentTempered => CreateEquipmentTemperedTrigger(Read<EquipmentTemperedPayload>(message)),
            GameEventTypes.IdleCombatEncounterCompleted => CreateCombatTrigger(Read<IdleCombatEncounterCompletedPayload>(message)),
            GameEventTypes.CharacterCreated => QuestTrigger.CharacterLevelReached(1),
            GameEventTypes.CharacterLevelReached => QuestTrigger.CharacterLevelReached(Read<CharacterLevelReachedPayload>(message).Level),
            GameEventTypes.ColosseumBattleCompleted => QuestTrigger.ColosseumBattleStarted(),
            GameEventTypes.TournamentBattleCompleted => QuestTrigger.TournamentBattleCompleted(),
            GameEventTypes.DungeonRunStarted => QuestTrigger.DungeonRunStarted(),
            GameEventTypes.DungeonRunCompleted => QuestTrigger.DungeonRunCompleted(
                Read<DungeonRunCompletedPayload>(message).DungeonDefinitionId),
            GameEventTypes.ProphecyCompleted when Read<ProphecyCompletedPayload>(message).Scope.Equals(
                "Daily", StringComparison.OrdinalIgnoreCase) => QuestTrigger.DailyProphecyCompleted(),
            _ => null
        };
        if (trigger is not null)
        {
            await progression.ProcessAsync(
                message.CharacterId.Value,
                trigger,
                message.Id,
                message.EventType,
                cancellationToken);
        }
    }

    private static QuestTrigger CreateEquipmentCraftedTrigger(EquipmentCraftedPayload payload) =>
        QuestTrigger.EquipmentCrafted(
            payload.CraftedItems.Select(x => x.ItemBaseId).ToList(),
            payload.CraftedItems.Select(x => x.Tier).ToList(),
            payload.CraftedItems.Select(x => x.BaseRecipeId).ToList(),
            payload.CraftedItems.Select(x => x.Quality).ToList(),
            payload.CraftedItems.Select(x => x.Potential).ToList());

    private static QuestTrigger CreateEquipmentTemperedTrigger(EquipmentTemperedPayload payload) =>
        QuestTrigger.EquipmentTempered(
            payload.CompletedItems.Select(x => x.ItemBaseId).ToList(),
            payload.CompletedItems.Select(x => x.Tier).ToList(),
            payload.CompletedItems.Select(x => x.BaseRecipeId).ToList(),
            payload.CompletedItems.Select(x => x.Quality).ToList(),
            payload.CompletedItems.Select(x => x.Potential).ToList(),
            payload.Summary.TotalActions);

    private static QuestTrigger CreateFocusedEssenceTrigger(FocusedCreatureEssenceReceivedPayload payload) =>
        QuestTrigger.FocusedCreatureEssenceReceived(payload.CreatureDefinitionId, payload.EssenceDefinitionId);

    private static QuestTrigger CreateCombatTrigger(IdleCombatEncounterCompletedPayload payload) =>
        QuestTrigger.CombatCompleted(
            payload.AreaId,
            payload.WonEncounter,
            Math.Max(1, payload.ActionCount),
            payload.EquippedGatheringType,
            payload.WinningEncounterCount,
            payload.GatheredResourceCount);

    private T Read<T>(GameEventOutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.PayloadJson, jsonOptions)
        ?? throw new InvalidOperationException(
            $"Outbox message '{message.Id}' could not be deserialized as {typeof(T).Name}.");
}
