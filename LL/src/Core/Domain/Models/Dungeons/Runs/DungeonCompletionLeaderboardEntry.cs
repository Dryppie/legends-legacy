namespace Domain.Models.Dungeons.Runs;

public sealed record DungeonCompletionLeaderboardEntry(
    Guid CharacterId,
    string CharacterName,
    string DungeonDefinitionId,
    DateTimeOffset FirstCompletedAt,
    DateTimeOffset LastCompletedAt,
    int CompletionCount);
