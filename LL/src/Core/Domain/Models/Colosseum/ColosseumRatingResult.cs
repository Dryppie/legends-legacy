namespace Domain.Models.Colosseum;
public class ColosseumRatingResult
{
    public int CharacterARatingBefore { get; init; }
    public int CharacterARatingAfter { get; init; }
    public int CharacterADelta => CharacterARatingAfter - CharacterARatingBefore;
    public int CharacterBRatingBefore { get; init; }
    public int CharacterBRatingAfter { get; init; }
    public int CharacterBDelta => CharacterBRatingAfter - CharacterBRatingBefore;
}
