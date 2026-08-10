using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;

namespace Services.LL.Outbox;

public sealed class GameEventOutboxConsumerRegistry : IGameEventOutboxConsumerRegistry
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ConsumersByEvent =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [GameEventTypes.EquipmentChanged] = [GameEventOutboxConsumerNames.Quests],
            [GameEventTypes.EssenceAbsorbed] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.EssenceLoadoutChanged] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.EssenceFocusSet] = [GameEventOutboxConsumerNames.Quests],
            [GameEventTypes.FocusedCreatureEssenceReceived] = [GameEventOutboxConsumerNames.Quests],
            [GameEventTypes.EssenceAscended] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.EquipmentCrafted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.EquipmentTempered] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.BlueprintUnlocked] = [GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.IdleCombatEncounterCompleted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.CharacterCreated] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.CharacterLevelReached] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.DungeonRunStarted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.DungeonRunCompleted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.ColosseumBattleCompleted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.TournamentBattleCompleted] = [GameEventOutboxConsumerNames.Quests],
            [GameEventTypes.ProphecyCompleted] = [GameEventOutboxConsumerNames.Quests]
        };

    public IReadOnlyList<string> GetConsumers(string eventType) =>
        ConsumersByEvent.TryGetValue(eventType, out var consumers) ? consumers : [];
}
