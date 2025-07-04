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
}