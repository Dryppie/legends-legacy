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
            x.CreatedAt, x.CompletedAt, x.RewardsClaimedAt, x.EquipmentCommitment
        }).SingleOrDefaultAsync(ct);
        if (run is null) return null;

        var rewardQuery = runs.Where(x => x.Id == run.Id).SelectMany(x => x.PendingRewards);
        var rewardCount = await rewardQuery.CountAsync(ct);
        var rewards = await rewardQuery.OrderBy(x => x.Id).Take(limit).ToListAsync(ct);
        // Receipts survive run deletion and claims; inspect this run's receipt even after a claim.
        var receipt = await db.EquipmentProtectionReceipts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CharacterId == characterId && x.RunId == run.Id, ct);

        var commitment = run.EquipmentCommitment;
        return new(run.Id, run.DungeonDefinitionId, run.DungeonDefinitionName, run.Status.ToString(),
            run.CurrentRoomIndex, run.CreatedAt, run.CompletedAt, run.RewardsClaimedAt,
            commitment is null ? null : new(commitment.CharacterId, commitment.RunId,
                commitment.DungeonId, commitment.PoolId, commitment.Difficulty, commitment.MatchingChance,
                commitment.GuaranteeCompletions, commitment.CompletionScrap,
                commitment.Target is { } target
                    ? new(target.State.Id, target.ItemBaseId, target.DisplayName,
                        ["Frozen target; not an award"], DescribeEquipment(target)) : null),
            receipt is null ? null : new(receipt.RunId, receipt.Outcome.PoolId,
                receipt.Outcome.SecuredAtUtc, receipt.ClaimedAtUtc, receipt.Outcome.PreviousProgress,
                receipt.Outcome.Progress, receipt.Outcome.Scrap,
                receipt.Outcome.Equipment is { } award
                    ? new(award.State.Id, award.ItemBaseId, award.DisplayName,
                        ["Recorded dungeon award"], DescribeEquipment(award)) : null),
            rewardCount, rewards.Select(reward => new EquipmentSupportRunRewardDto(
                reward.Id, reward.ItemId, reward.Name, reward.ItemType.ToString(), reward.Quantity, reward.Source,
                reward.ProgressionData is { } equipment
                    ? new(equipment.State.Id, equipment.ItemBaseId, equipment.DisplayName,
                        ["Saved run reward row"], DescribeEquipment(equipment)) : null)).ToArray());
    }
}
