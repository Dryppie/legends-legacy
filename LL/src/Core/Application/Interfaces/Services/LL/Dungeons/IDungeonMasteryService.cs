using Domain.Models.Dungeons.Runs;
using Domain.Models.Dungeons.Mastery;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonMasteryService
{
    int CalculateLevel(long experience);
    int? GetExperienceRequiredForNextLevel(int level);

    Task<DungeonMasteryAwardResult> AwardCompletionAsync(
        DungeonRun run,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, DungeonMasterySnapshot>> GetMasteryByDungeonAsync(
        Guid characterId,
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken);
}

public sealed record DungeonMasteryAwardResult(
    string DungeonDefinitionId,
    long ExperienceAwarded,
    long TotalExperience,
    int PreviousLevel,
    int Level,
    int CompletionCount,
    IReadOnlyList<DungeonMasteryAwardReason> Reasons,
    bool AlreadyAwarded)
{
    public int LevelsGained => Math.Max(0, Level - PreviousLevel);
}

public sealed record DungeonMasterySnapshot(
    string DungeonDefinitionId,
    long Experience,
    int Level,
    int? ExperienceRequiredForNextLevel,
    int CompletionCount);
