using Domain.Models.Dungeons.Mastery;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonMasteryBonusDefinitionProvider
{
    IReadOnlyList<DungeonMasteryBonusDefinition> GetAll();
}
