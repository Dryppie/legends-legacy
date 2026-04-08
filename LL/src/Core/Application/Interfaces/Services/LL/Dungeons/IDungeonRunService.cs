using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonRunService
{
    Task<DungeonRun?> GetDungeonRunAsync(Guid characterId, CancellationToken cancellationToken);
    Task<DungeonRun> StartRunAsync(Guid characterId, string dungeonDefinitionId, CancellationToken ct);
    Task<DungeonRun?> TickRunAsync(Guid runId, CancellationToken ct); // progresses one “step” (encounter/event)
    //Task<DungeonRun> WithdrawAsync(Guid runId, CancellationToken ct);

    //Task<DungeonRun> SelectTreasureOptionAsync(Guid runId, int optionIndex, CancellationToken ct);
    //Task<DungeonRun> SelectShrineBlessingAsync(Guid runId, Guid blessingId, CancellationToken ct);
}
