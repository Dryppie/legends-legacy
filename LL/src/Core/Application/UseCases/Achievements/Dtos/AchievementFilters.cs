using Domain.Models.Achievements;

namespace Application.UseCases.Achievements.Dtos;

public sealed class AchievementFilters
{
    public AchievementCategory? Category { get; init; }
    public AchievementVisibility? Visibility { get; init; }
    public bool? Completed { get; init; }
    public string? Search { get; init; }
}

public sealed class TitleFilters
{
    public AchievementCategory? Category { get; init; }
    public TitleRarity? Rarity { get; init; }
    public bool? Unlocked { get; init; }
    public string? Search { get; init; }
}
