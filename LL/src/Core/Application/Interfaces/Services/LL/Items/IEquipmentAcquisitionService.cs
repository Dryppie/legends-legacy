using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Items;

public interface IEquipmentAcquisitionService
{
    Task CompleteAsync(DungeonRun run, bool firstCompletion, CancellationToken ct);
}
