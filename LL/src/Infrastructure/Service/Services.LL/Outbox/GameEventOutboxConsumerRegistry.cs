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
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.EssenceLoadoutChanged] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.EssenceFocusSet] = [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.FocusedCreatureEssenceReceived] = [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.EssenceAscended] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.EquipmentCrafted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.EquipmentTempered] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.BlueprintUnlocked] = [GameEventOutboxConsumerNames.Achievements],
            [GameEventTypes.IdleCombatEncounterCompleted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.CharacterCreated] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.CharacterLevelReached] =
            [
                GameEventOutboxConsumerNames.Quests,
                GameEventOutboxConsumerNames.Achievements,
                GameEventOutboxConsumerNames.EventQuests,
                GameEventOutboxConsumerNames.RealtimeCharacter
            ],
            [GameEventTypes.DungeonRunStarted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.DungeonRunCompleted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.ColosseumBattleCompleted] =
                [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.Achievements, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.TournamentBattleCompleted] = [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.TournamentChatAnnouncement] = [GameEventOutboxConsumerNames.TournamentChat],
            [GameEventTypes.ProphecyCompleted] = [GameEventOutboxConsumerNames.Quests, GameEventOutboxConsumerNames.EventQuests],
            [GameEventTypes.PlayerTransferChatMessage] = [GameEventOutboxConsumerNames.TransferChat],
            [GameEventTypes.GuildChatMessage] = [GameEventOutboxConsumerNames.GuildChat],
            [GameEventTypes.GuildVaultChatMessage] = [GameEventOutboxConsumerNames.GuildVaultChat],
            [GameEventTypes.GuildMissionSelected] = [GameEventOutboxConsumerNames.RealtimeGuildMission],
            [GameEventTypes.InventoryItemsGranted] = [GameEventOutboxConsumerNames.RealtimeInventory],
            [GameEventTypes.TournamentGroundsUpdated] = [GameEventOutboxConsumerNames.RealtimeTournamentGrounds],
            [GameEventTypes.WorldTowerRallyUpdated] = [GameEventOutboxConsumerNames.RealtimeWorldTower],
            [GameEventTypes.WorldTowerChatAnnouncement] = [GameEventOutboxConsumerNames.WorldTowerChat],
            [GameEventTypes.EventQuestChatAnnouncement] = [GameEventOutboxConsumerNames.EventQuestChat],
            [GameEventTypes.RealtimeDeliveryRequested] = [GameEventOutboxConsumerNames.RealtimeDelivery]
        };

    public IReadOnlyList<string> GetConsumers(string eventType) =>
        ConsumersByEvent.TryGetValue(eventType, out var consumers) ? consumers : [];
}
