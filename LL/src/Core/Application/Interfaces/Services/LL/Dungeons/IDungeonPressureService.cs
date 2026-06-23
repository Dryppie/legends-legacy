using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonPressureService
{
    DungeonPressureResult ApplyPressureDelta(DungeonRun run, int delta);
    int CalculateRewardMultiplierPercent(int pressure);
    IReadOnlyList<string> GetActivePressureThresholdIds(int pressure);
}
