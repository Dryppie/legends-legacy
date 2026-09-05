using Microsoft.EntityFrameworkCore;
using Persistence.LL;

namespace API.LiveOps.Support;

public sealed partial class LiveOpsPlayerSupportSnapshotService
{
    private static async Task<EquipmentSupportDungeonRunDto?> LoadEquipmentDungeonRunAsync(
        LLDbContext db, Guid characterId, int limit, CancellationToken ct)
    {
        // One retained run per character. Avoid loading rooms, combat snapshots or unbounded rewards.
        var runs = db.DungeonRuns.AsNoTracking().Where(x => x.CharacterId == characterId);
        var run = await runs.Select(x => new
        {
            x.Id, x.DungeonDefinitionId, x.DungeonDefinitionName, x.Status, x.CurrentRoomIndex,
            x.CreatedAt, x.CompletedAt, x.RewardsClaimedAt
        }).SingleOrDefaultAsync(ct);
        if (run is null) return null;

        var rewardQuery = runs.Where(x => x.Id == run.Id).SelectMany(x => x.PendingRewards);
        var rewardCount = await rewardQuery.CountAsync(ct);
        var rewards = await rewardQuery.OrderBy(x => x.Id).Take(limit).ToListAsync(ct);
        return new(run.Id, run.DungeonDefinitionId, run.DungeonDefinitionName, run.Status.ToString(),
            run.CurrentRoomIndex, run.CreatedAt, run.CompletedAt, run.RewardsClaimedAt,
            rewardCount, rewards.Select(reward => new EquipmentSupportRunRewardDto(
                reward.Id, reward.ItemId, reward.Name, reward.ItemType.ToString(), reward.Quantity, reward.Source,
                reward.ProgressionData is { } equipment
                    ? new(equipment.State.Id, equipment.ItemBaseId, equipment.DisplayName,
                        ["Saved run reward row"], DescribeEquipment(equipment)) : null)).ToArray());
    }
}
