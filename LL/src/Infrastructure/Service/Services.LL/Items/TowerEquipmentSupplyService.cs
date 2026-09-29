using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.WorldTower;
using Common.Randomness;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Options;
using Services.LL.WorldTower;

namespace Services.LL.Items;

public sealed class TowerEquipmentSupplyService(TowerEquipmentSupplyCatalog catalog,
    IWorldTowerProgressRepository progress, IWorldTowerDefinitionProvider floors,
    ICharacterSnapshotRepository snapshots, IDungeonRunRepository runs, IItemBaseRepository itemBases,
    IOptions<WorldTowerOptions> towerOptions, IOptions<EquipmentProgressionOptions> options)
    : ITowerEquipmentSupplyService
{
    public const string RewardSource = "tower-equipment-supply";

    public async Task CompleteAsync(DungeonRun run, int sourceRegion, CancellationToken ct)
    {
        if (!options.Value.ProtectedAcquisitionEnabled || !options.Value.TowerSupplyAcquisitionEnabled
            || run.Status != DungeonRunStatus.Completed || run.RewardsClaimedAt is not null
            || run.State.TowerEquipmentSupplyProcessed)
            return;

        var identity = new[] { RewardSource, run.CharacterId.ToString("N"), run.Id.ToString("N") };
        var rewardId = StableRandom.Guid(identity);
        if (run.PendingRewards.Any(x => x.Id == rewardId))
        {
            run.State.TowerEquipmentSupplyProcessed = true;
            return;
        }

        var snapshot = run.CharacterSnapshotId is { } snapshotId
            ? await snapshots.GetSnapshotByIdAsync(snapshotId, ct) : null;
        if (snapshot?.CharacterId != run.CharacterId)
        {
            run.State.TowerEquipmentSupplyProcessed = true;
            return;
        }

        foreach (var supply in catalog.Candidates(sourceRegion, snapshot.Level))
        {
            // The preceding floor unlocks its preparation gear; never require the target floor itself.
            if (floors.GetFloor(supply.TargetFloor) is null || (supply.RequiredClearedFloor > 0
                && !await progress.HasClearedFloorAsync(towerOptions.Value.ServerId, supply.RequiredClearedFloor, ct)))
                continue;
            var bases = await itemBases.GetItemBasesByIdsAsync([supply.ItemBaseId], ct);
            if (!bases.TryGetValue(supply.ItemBaseId, out var item) || !item.IsBound
                || !item.Stackable || item.ItemType != ItemType.Resource)
                throw new InvalidOperationException($"Tower supply item '{supply.ItemBaseId}' is missing or invalid.");
            await runs.AddPendingRewardAsync(run, new RunReward
            {
                Id = rewardId, ItemId = supply.ItemBaseId, Name = supply.Name,
                ItemType = ItemType.Resource, Quantity = 1, Source = RewardSource
            }, ct);
            break;
        }
        // Remember a no-award decision as well, so retries cannot acquire newly unlocked rewards.
        run.State.TowerEquipmentSupplyProcessed = true;
    }
}
