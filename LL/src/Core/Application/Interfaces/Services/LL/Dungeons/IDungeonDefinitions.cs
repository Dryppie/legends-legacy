using Domain.Models.Dungeons;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonDefinitions
{
    DungeonDefinition GetByKey(string key);
    IReadOnlyList<DungeonDefinition> GetAll();
}
