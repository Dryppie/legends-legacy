namespace Application.WebSockets.Contracts.V2;

public static class GameRealtimeEventNamesV2
{
    public const string DungeonRewardsClaimed = nameof(DungeonRewardsClaimedV2);
    public const string LootReceived = nameof(LootReceivedV2);
    public const string InventorySnapshot = nameof(InventorySnapshotV2);
    public const string CharacterSnapshot = nameof(CharacterSnapshotV2);
    public const string IdleCombatProcessed = nameof(IdleCombatProcessedV2);
}
