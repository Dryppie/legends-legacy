namespace Application.WebSockets.Contracts;

public static class GameRealtimeEventNames
{
    public const string DungeonRewardsClaimed = nameof(DungeonRewardsClaimed);
    public const string LootReceived = nameof(LootReceived);
    public const string InventorySnapshot = nameof(InventorySnapshot);
    public const string CharacterSnapshot = nameof(CharacterSnapshot);
    public const string IdleCombatProcessed = nameof(IdleCombatProcessed);
}
