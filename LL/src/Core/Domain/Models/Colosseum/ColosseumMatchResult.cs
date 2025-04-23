namespace Domain.Models.Colosseum;
public class ColosseumMatchResult
{
    public Guid MatchId { get; init; }
    public Guid SeasonId { get; init; }
    public Guid PlayerAId { get; init; }
    public Guid PlayerBId { get; init; }
    public Guid? WinnerId { get; init; } // null = draw
    public DateTime PlayedUtc { get; init; }
    public int PlayerARatingBefore { get; init; }
    public int PlayerBRatingBefore { get; init; }
    public int PlayerARatingAfter { get; init; }
    public int PlayerBRatingAfter { get; init; }
    public int DurationSec { get; init; }
}