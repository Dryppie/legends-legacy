namespace Application.WebSockets.Contracts;

public static class GameRealtimeEventNames
{
    public const string LootReceived = nameof(LootReceived);
    public const string StateInvalidated = nameof(StateInvalidated);
    public const string AccountAccessChanged = nameof(AccountAccessChanged);
    public const string CharacterLevelUp = nameof(CharacterLevelUp);
    public const string MarketplaceChanged = nameof(MarketplaceChanged);
    public const string GuildApplication = nameof(GuildApplication);
    public const string GuildInviteReceived = nameof(GuildInviteReceived);
    public const string GuildInviteRejected = nameof(GuildInviteRejected);
    public const string GuildApplicationRejected = nameof(GuildApplicationRejected);
    public const string GuildBuildingsChanged = nameof(GuildBuildingsChanged);
    public const string GuildMissionsChanged = nameof(GuildMissionsChanged);
    public const string GuildStateChanged = nameof(GuildStateChanged);
    public const string GuildVaultChatMessage = nameof(GuildVaultChatMessage);
    public const string GuildMembershipChanged = nameof(GuildMembershipChanged);
    public const string GuildDisbanded = nameof(GuildDisbanded);
    public const string GuildDirectoryChanged = nameof(GuildDirectoryChanged);
    public const string QuestJournalChanged = nameof(QuestJournalChanged);
    public const string EventQuestChanged = nameof(EventQuestChanged);
    public const string ArenaBattleCompleted = nameof(ArenaBattleCompleted);
    public const string ProphecyProgressed = nameof(ProphecyProgressed);
    public const string AchievementUnlocked = nameof(AchievementUnlocked);
    public const string PlayerTransfer = nameof(PlayerTransfer);
    public const string TournamentGroundsUpdated = nameof(TournamentGroundsUpdated);
    public const string WorldTowerRallyUpdated = nameof(WorldTowerRallyUpdated);
    public const string WorldTowerCombatFrameUpdated = nameof(WorldTowerCombatFrameUpdated);
    public const string RaidUpdated = nameof(RaidUpdated);
    public const string RaidDirectoryUpdated = nameof(RaidDirectoryUpdated);
}
