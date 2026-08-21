using Domain.Models.Entities.Characters;

namespace Application.Interfaces.Services.LL.PowerRatings;

public static class PowerRatingAlgorithm
{
    public const int Version = 25;
    public const int CombatRulesVersion = 16;
}

public static class CombatRatingDisplay
{
    public const int Divisor = 10;

    public static int FromRaw(int rating) => Math.Max(0, rating) / Divisor;
}

public enum PowerRatingConfidence
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum PowerAnalysisState
{
    Available = 0,
    Unsupported = 1,
    InsufficientCombatData = 2,
    LowConfidence = 3,
    CalculationFailed = 4
}

public sealed record PowerRatingSnapshot(
    int AlgorithmVersion,
    string BuildFingerprint,
    int Overall,
    int SingleTargetOffense,
    int MultiTargetOffense,
    int PhysicalDurability,
    int MagicalDurability,
    int Sustain,
    int ControlUtility,
    DateTimeOffset ComputedAtUtc,
    PowerRatingConfidence Confidence,
    PowerAnalysisState State,
    string? StatusMessage = null);

public sealed record OverallPowerRating(
    int Overall,
    PowerAnalysisState State,
    string? StatusMessage = null);

public sealed record DungeonPartySelection(IReadOnlyList<Guid> CompanionIds)
{
    public static DungeonPartySelection Solo { get; } = new([]);
}

public interface IPowerRatingService
{
    Task<OverallPowerRating> GetCharacterOverallRatingAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task<OverallPowerRating> GetCharacterOverallRatingAsync(
        Character character,
        CancellationToken cancellationToken);

    Task<PowerRatingSnapshot> GetCharacterRatingAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task<PowerRatingSnapshot> GetPartyRatingAsync(
        Guid characterId,
        DungeonPartySelection partySelection,
        CancellationToken cancellationToken);
}
