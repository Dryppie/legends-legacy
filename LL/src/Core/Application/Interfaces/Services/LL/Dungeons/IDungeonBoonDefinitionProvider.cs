using Domain.Models.Dungeons.Definitions.Boons;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonBoonDefinitionProvider
{
    IReadOnlyList<DungeonBoonDefinition> GetAll();
    DungeonBoonDefinition? GetById(string boonId);
}
