namespace Application.UseCases.Outbox;

public static class GameEventTypes
{
    public const string EquipmentChanged = "equipment.changed";
    public const string EquipmentSecured = "equipment.model_e_secured";
    public const string EssenceAbsorbed = "essence.absorbed";
    public const string EssenceLoadoutChanged = "essence.loadout_changed";
    public const string EssenceFocusSet = "essence.focus_set";
    public const string FocusedCreatureEssenceReceived = "essence.focused_creature_received";
    public const string EssenceAscended = "essence.ascended";
    public const string IdleCombatEncounterCompleted = "combat.idle_encounter_completed";
    public const string CharacterCreated = "character.created";
    public const string CharacterLevelReached = "character.level_reached";
    public const string DungeonRunStarted = "dungeon.run_started";
    public const string DungeonRunCompleted = "dungeon.run_completed";
    public const string ColosseumBattleCompleted = "colosseum.battle_completed";
    public const string TournamentBattleCompleted = "colosseum.tournament_battle_completed";
    public const string TournamentGroundsUpdated = "colosseum.tournament_grounds_updated";
    public const string TournamentChatAnnouncement = "colosseum.tournament_chat_announcement";
    public const string ProphecyCompleted = "prophecy.completed";
    public const string PlayerTransferChatMessage = "player_transfer.chat_message";
    public const string GuildChatMessage = "guild.chat_message";
    public const string GuildVaultChatMessage = "guild_vault.chat_message";
    public const string GuildMissionSelected = "guild.mission_selected";
    public const string GuildMissionProgressed = "guild.mission_progressed";
    public const string InventoryItemsGranted = "inventory.items_granted";
    public const string WorldTowerRallyUpdated = "world_tower.rally_updated";
    public const string WorldTowerChatAnnouncement = "world_tower.chat_announcement";
    public const string RaidUpdated = "raid.updated";
    public const string RaidChatAnnouncement = "raid.chat_announcement";
    public const string RaidChatChannelSnapshot = "raid.chat_channel_snapshot";
    public const string RegionBossChatAnnouncement = "region_boss.chat_announcement";
    public const string EventQuestChatAnnouncement = "event_quest.chat_announcement";
    public const string RealtimeDeliveryRequested = "realtime.delivery_requested";
    public const string AccountMultiplayerRestricted = "account.multiplayer_restricted";
}

public sealed record AccountMultiplayerRestrictedPayload(
    Guid RestrictionId,
    Guid AccountId,
    Guid CharacterId,
    DateTimeOffset AppliedAt);
