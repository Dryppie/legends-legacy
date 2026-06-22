using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Definitions.Routes;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonRouteDefinitionProvider
{
    IReadOnlyList<DungeonRouteDefinition> GetAll();
    IReadOnlyList<DungeonRouteDefinition> GetDefinitions(string dungeonDefinitionId, RoomType roomType);
}
