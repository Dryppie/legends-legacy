using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonRunService
{
    Task<ExecuteDungeonActionResult?> ExecuteActionAsync(Guid runId, string actionId, object? payload, CancellationToken cancellationToken);
    Task<DungeonRun?> GetDungeonRunAsync(Guid characterId, CancellationToken cancellationToken);
    Task<DungeonRun?> StartRunAsync(Guid characterId, string dungeonDefinitionId, CancellationToken cancellationToken);
    //Task<DungeonRun> WithdrawAsync(Guid runId, CancellationToken ct);
    //Task<DungeonRun> SelectTreasureOptionAsync(Guid runId, int optionIndex, CancellationToken ct);
    //Task<DungeonRun> SelectShrineBlessingAsync(Guid runId, Guid blessingId, CancellationToken ct);
}
