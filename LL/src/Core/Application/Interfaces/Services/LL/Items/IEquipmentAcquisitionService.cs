using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Items;

public interface IEquipmentAcquisitionService
{
    Task CompleteAsync(DungeonRun run, bool firstCompletion, CancellationToken ct);
    Task CompleteMiniBossAsync(DungeonRun run, int roomIndex, CancellationToken ct);
    RunReward RollTreasuryReward(DungeonRun run, int roomIndex);
}
