using Domain.Models.Dungeons.Definitions.Events;
using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonEventChoiceService
{
    IReadOnlyList<DungeonEventChoiceOption> EnsureChoices(DungeonRun run, EventOutcomeType eventOutcome);
    IReadOnlyList<DungeonEventChoiceOption> EnsureChoices(
        DungeonRun run,
        string dungeonDefinitionId,
        EventOutcomeType eventOutcome);

    DungeonEventChoiceOption ApplyChoiceState(DungeonRun run, string choiceId);
}
