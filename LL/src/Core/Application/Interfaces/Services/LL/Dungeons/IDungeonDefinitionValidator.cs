using Domain.Models.Dungeons;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonDefinitionValidator
{
    IReadOnlyList<string> Validate(IReadOnlyList<DungeonDefinition> definitions);
    void ThrowIfInvalid(IReadOnlyList<DungeonDefinition> definitions);
}
