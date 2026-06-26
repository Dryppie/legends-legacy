namespace Domain.Models.Colosseum;
public class ColosseumMatchResult
{
    public Guid Id { get; set; }

    public Guid CharacterAId { get; set; }
    public string CharacterAName { get; set; } = string.Empty;
    public int CharacterARatingBefore { get; set; }
    public int CharacterARatingAfter { get; set; }

    public Guid CharacterBId { get; set; }
    public string CharacterBName { get; set; } = string.Empty;
    public int CharacterBRatingBefore { get; set; }
    public int CharacterBRatingAfter { get; set; }

    public Guid? WinnerId { get; set; }
    public string WinnerName { get; set; } = string.Empty;
    public DateTimeOffset PlayedAt { get; set; }

    public string Outcome { get; set; } = string.Empty;
    public int CharacterARatingDelta { get; set; }
    public int CharacterBRatingDelta { get; set; }
    public int CharacterAGloryEarned { get; set; }
    public int CharacterBGloryEarned { get; set; }
    public int CharacterAStreakBefore { get; set; }
    public int CharacterAStreakAfter { get; set; }
}
