using Domain.Models.Dungeons.Runs;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Dungeons.Definitions.Boons;
using Domain.Models.Essences.Definitions;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonBoonService
{
    IReadOnlyList<DungeonBoonDefinition> GetAllDefinitions();
    DungeonBoonDefinition? GetDefinition(string boonId);
    IReadOnlyList<DungeonBoonChoiceOption> GenerateBoonChoices(DungeonRun run, int count = 3);
    void ChooseBoon(DungeonRun run, string boonId);
    void SyncActiveBoonState(DungeonRun run);
    IReadOnlyList<AttributeModifierBase> GetActiveAttributeModifiers(DungeonRun run);
    IReadOnlyList<EssenceAbilityModifierDefinition> GetActiveAbilityModifiers(DungeonRun run);
}
