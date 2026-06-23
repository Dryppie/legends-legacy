using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonRouteService
{
    IReadOnlyList<DungeonRouteOption> GenerateRouteOptions(DungeonRun run);
    DungeonRouteOption ChooseRoute(DungeonRun run, string routeOptionId);
}
