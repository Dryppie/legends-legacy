using Domain.Models.Dungeons;

namespace Services.LL.JsonDefinitions;

public interface IDungeonDefinitions
{
    DungeonDefinition GetByKey(string key);
    IReadOnlyList<DungeonDefinition> GetAll();
}

