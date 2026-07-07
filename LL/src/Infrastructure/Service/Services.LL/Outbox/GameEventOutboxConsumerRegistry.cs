using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;

namespace Services.LL.Outbox;

public sealed class GameEventOutboxConsumerRegistry : IGameEventOutboxConsumerRegistry
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ConsumersByEvent =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [GameEventTypes.EquipmentChanged] =
            [
                GameEventOutboxConsumerNames.Tutorial
            ],
            [GameEventTypes.EssenceAbsorbed] =
            [
                GameEventOutboxConsumerNames.Tutorial,
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.EssenceLoadoutChanged] =
            [
                GameEventOutboxConsumerNames.Tutorial,
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.EssenceAscended] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.EquipmentCrafted] =
            [
                GameEventOutboxConsumerNames.Tutorial,
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.EquipmentTempered] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.BlueprintUnlocked] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.IdleCombatEncounterCompleted] =
            [
                GameEventOutboxConsumerNames.Tutorial,
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.CharacterCreated] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.CharacterLevelReached] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.DungeonRunStarted] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.DungeonRunCompleted] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.ColosseumBattleCompleted] =
            [
                GameEventOutboxConsumerNames.Achievements
            ],
            [GameEventTypes.ClientTutorialStep] =
            [
                GameEventOutboxConsumerNames.Tutorial
            ]
        };

    public IReadOnlyList<string> GetConsumers(string eventType) =>
        ConsumersByEvent.TryGetValue(eventType, out var consumers)
            ? consumers
            : [];
}
