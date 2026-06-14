namespace Domain.Models.Dungeons.Definitions;

public sealed class DungeonRewardTable
{
    public List<DungeonRewardGrant> CompletionRewards { get; set; } = [];
    public List<DungeonRewardGrant> BonusRewards { get; set; } = [];
    public List<DungeonRewardGrant> FirstClearRewards { get; set; } = [];
}
