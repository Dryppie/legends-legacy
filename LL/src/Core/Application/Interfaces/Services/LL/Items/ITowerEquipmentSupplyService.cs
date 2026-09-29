using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Items;

public interface ITowerEquipmentSupplyService
{
    Task CompleteAsync(DungeonRun run, int sourceRegion, CancellationToken cancellationToken);
}
