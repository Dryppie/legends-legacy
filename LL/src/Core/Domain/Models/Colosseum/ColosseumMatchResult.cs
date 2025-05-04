namespace Domain.Models.Colosseum;
public class ColosseumMatchResult
{
    public Guid Id { get; set; }
    //public Guid SeasonId { get; init; }
    public Guid CharacterAId { get; set; }
    public string CharacterAName { get; set; } = string.Empty;
    public Guid CharacterBId { get; set; }
    public string CharacterBName { get; set; } = string.Empty;
    public Guid? WinnerId { get; set; } // null = draw
    public string WinnerName { get; set; } = string.Empty;
    public DateTimeOffset PlayedAt { get; set; }
    //public int DurationSec { get; init; }
}