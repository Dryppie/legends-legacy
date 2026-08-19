namespace Application.WebSockets.Contracts;

public static class GameRealtimeEventNames
{
    public const string DungeonRewardsClaimed = nameof(DungeonRewardsClaimed);
    public const string LootReceived = nameof(LootReceived);
    public const string InventorySnapshot = nameof(InventorySnapshot);
    public const string CharacterSnapshot = nameof(CharacterSnapshot);
    public const string AccountAccessChanged = nameof(AccountAccessChanged);
    public const string TournamentGroundsUpdated = nameof(TournamentGroundsUpdated);
    public const string WorldTowerRallyUpdated = nameof(WorldTowerRallyUpdated);
    public const string WorldTowerCombatFrameUpdated = nameof(WorldTowerCombatFrameUpdated);
}
